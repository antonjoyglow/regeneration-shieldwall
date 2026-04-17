using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.GameMaster.Models;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Thread-safe singleton that scores team decisions against scenario ground truth,
/// evaluates compound-threat pattern detection, and pushes live score updates via SignalR.
/// </summary>
public sealed class ScoringEngine(
    IHubContext<SentinelHub, ISentinelHubClient> hubContext,
    ITeamTracker teamTracker,
    ScenarioLoader scenarioLoader,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<ScoringEngine> logger) : IScoringEngine, IDisposable
{
    // ── Ground truth ──────────────────────────────────────────────────────────

    private readonly ConcurrentDictionary<string, AlertGroundTruth> _groundTruth =
        new(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<CompoundThreat> _compoundThreats = [];

    // ── Idempotency guard: (TeamId, AlertId) → processed ─────────────────────

    private readonly ConcurrentDictionary<(string TeamId, string AlertId), bool> _processedDecisions = new();

    // ── Compound threat detection: TeamId → set of credited GroupIds ──────────

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> _detectedPatterns =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Broadcast times: AlertId → UTC broadcast offset ───────────────────────

    private readonly ConcurrentDictionary<string, DateTimeOffset> _alertBroadcastTimes =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Per-team accuracy counters (for ExportResults) ────────────────────────

    private readonly ConcurrentDictionary<string, int> _correctClassifications =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _correctActions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _noiseAlertsCorrect =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _noiseAlertsTotal =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _compoundThreatsDetectedByTeam =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Per-team mutation locks (ConnectedTeam fields are not atomic) ─────────

    private readonly ConcurrentDictionary<string, object> _teamLocks =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Initialisation ────────────────────────────────────────────────────────

    private volatile bool _initialized;
    private readonly object _initLock = new();
    private ITimer? _scoreUpdateTimer;

    /// <inheritdoc />
    public void Initialize(ScenarioFile scenario)
    {
        lock (_initLock)
        {
            ClearState();
            InitializeCore(scenario);
        }
    }

    private void InitializeCore(ScenarioFile scenario)
    {
        foreach (var alert in scenario.Alerts)
            _groundTruth[alert.AlertId] = alert.GroundTruth;

        _compoundThreats = scenario.CompoundThreats;

        _scoreUpdateTimer?.Dispose();
        _scoreUpdateTimer = timeProvider.CreateTimer(
            OnScoreTimerTick,
            state: null,
            dueTime: TimeSpan.FromSeconds(5),
            period: TimeSpan.FromSeconds(5));

        _initialized = true;

        logger.LogInformation(
            "ScoringEngine initialised with {AlertCount} alerts and {ThreatCount} compound threats",
            _groundTruth.Count, _compoundThreats.Count);
    }

    private void ClearState()
    {
        _groundTruth.Clear();
        _processedDecisions.Clear();
        _detectedPatterns.Clear();
        _alertBroadcastTimes.Clear();
        _correctClassifications.Clear();
        _correctActions.Clear();
        _noiseAlertsCorrect.Clear();
        _noiseAlertsTotal.Clear();
        _compoundThreatsDetectedByTeam.Clear();
        _teamLocks.Clear();
        _compoundThreats = [];
        _initialized = false;
    }

    /// <summary>
    /// Lazily initialises from the scenario path config when the engine is used
    /// before an explicit <see cref="Initialize"/> call (safety fallback).
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            var scenarioPath = configuration["Scenario:Path"] ?? "Data/alert-scenario.json";
            var scenario = scenarioLoader.Load(scenarioPath);
            InitializeCore(scenario);
        }
    }

    // ── IScoringEngine ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void RecordAlertBroadcast(string alertId, DateTimeOffset broadcastTime) =>
        _alertBroadcastTimes[alertId] = broadcastTime;

    /// <inheritdoc />
    public async Task EvaluateDecisionAsync(TeamDecision decision, string connectionId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(decision);

        EnsureInitialized();

        var teamName = teamTracker.GetTeamNameByConnectionId(connectionId);
        if (teamName is null)
        {
            logger.LogWarning(
                "Decision received from unknown connection {ConnectionId} — ignoring", connectionId);
            return;
        }

        // Idempotency: drop duplicate submissions for the same alert by the same team.
        if (!_processedDecisions.TryAdd((teamName, decision.AlertId), true))
        {
            logger.LogDebug(
                "Duplicate decision from {TeamName} for alert {AlertId} — skipping",
                teamName, decision.AlertId);
            return;
        }

        if (!_groundTruth.TryGetValue(decision.AlertId, out var groundTruth))
        {
            logger.LogWarning(
                "No ground truth found for alert {AlertId} submitted by {TeamName}",
                decision.AlertId, teamName);
            return;
        }

        var teamClassification = decision.Classification.ThreatLevel;
        var correctClassification = groundTruth.CorrectClassification;
        var teamAction = decision.Response.Action;
        var correctAction = groundTruth.CorrectAction;

        // ── Classification score ──────────────────────────────────────────────
        bool bothNoise = teamClassification == ThreatLevel.Noise
                      && correctClassification == ThreatLevel.Noise;

        double classificationScore = bothNoise
            ? 1.2
            : Math.Abs((int)teamClassification - (int)correctClassification) switch
            {
                0 => 1.0,
                1 => 0.5,
                _ => 0.0
            };

        // ── Action score ──────────────────────────────────────────────────────
        double actionScore;

        if (teamAction == ActionType.Dismiss && correctAction == ActionType.Dismiss)
            actionScore = 1.1; // bonus for correct restraint
        else if (teamAction == correctAction)
            actionScore = 1.0;
        else if (teamAction == ActionType.Escalate && correctAction != ActionType.Escalate)
            actionScore = 0.7; // over-escalation
        else if (teamAction != ActionType.Escalate && correctAction == ActionType.Escalate)
            actionScore = 0.0; // under-escalation — most dangerous
        else
            actionScore = 0.5;

        double alertTotal = (classificationScore * 0.6) + (actionScore * 0.4);

        // ── Latency multiplier ────────────────────────────────────────────────
        double latencyMs = 0;
        double latencyMultiplier = 1.0;

        if (_alertBroadcastTimes.TryGetValue(decision.AlertId, out var broadcastTime))
        {
            latencyMs = (decision.ProcessedAt - broadcastTime).TotalMilliseconds;
            latencyMultiplier = latencyMs switch
            {
                < 500 => 1.0,
                < 2000 => 0.9,
                < 5000 => 0.7,
                _ => 0.5
            };
        }

        double finalScore = alertTotal * latencyMultiplier;

        // Max possible score for this alert (best-case classification + action, no latency penalty).
        double maxClassScore = correctClassification == ThreatLevel.Noise ? 1.2 : 1.0;
        double maxActionScore = correctAction == ActionType.Dismiss ? 1.1 : 1.0;
        double maxAlertScore = (maxClassScore * 0.6) + (maxActionScore * 0.4);

        // ── Update ConnectedTeam state ────────────────────────────────────────
        var team = teamTracker.GetTeam(teamName);
        if (team is null)
        {
            logger.LogWarning(
                "Team {TeamName} resolved from connection ID but not found in tracker", teamName);
            return;
        }

        var teamLock = _teamLocks.GetOrAdd(teamName, static _ => new object());
        lock (teamLock)
        {
            team.AlertsProcessed++;
            team.TotalScore += finalScore;
            team.MaxPossibleScore += maxAlertScore;
            team.MissionEffectiveness = team.MaxPossibleScore > 0
                ? team.TotalScore / team.MaxPossibleScore * 100.0
                : 0.0;

            // Rolling average latency (Welford-style online update).
            int latencyMsInt = (int)Math.Max(0, latencyMs);
            team.AverageLatencyMs = team.AlertsProcessed == 1
                ? latencyMsInt
                : (int)(((team.AverageLatencyMs * (team.AlertsProcessed - 1.0)) + latencyMsInt)
                  / team.AlertsProcessed);
        }

        // ── Accuracy counters ─────────────────────────────────────────────────
        if (teamClassification == correctClassification)
            _correctClassifications.AddOrUpdate(teamName, 1, static (_, v) => v + 1);

        if (teamAction == correctAction)
            _correctActions.AddOrUpdate(teamName, 1, static (_, v) => v + 1);

        if (correctClassification == ThreatLevel.Noise)
        {
            _noiseAlertsTotal.AddOrUpdate(teamName, 1, static (_, v) => v + 1);
            if (bothNoise)
                _noiseAlertsCorrect.AddOrUpdate(teamName, 1, static (_, v) => v + 1);
        }

        logger.LogInformation(
            "Scored decision for {TeamName} on {AlertId}: " +
            "Classification={ClassScore:F2}, Action={ActionScore:F2}, " +
            "Latency={LatencyMs:F0}ms, Total={FinalScore:F3}",
            teamName, decision.AlertId,
            classificationScore, actionScore, latencyMs, finalScore);
    }

    /// <inheritdoc />
    public Task EvaluatePatternsAsync(TeamPatternReport report, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(report);

        EnsureInitialized();

        var team = teamTracker.GetTeam(report.TeamId);
        if (team is null)
        {
            logger.LogWarning(
                "Pattern report received from unknown team {TeamId}", report.TeamId);
            return Task.CompletedTask;
        }

        var teamLock = _teamLocks.GetOrAdd(report.TeamId, static _ => new object());
        var creditedGroups = _detectedPatterns.GetOrAdd(
            report.TeamId,
            static _ => new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase));

        int credited = 0;
        int falsePositives = 0;

        foreach (var pattern in report.Patterns ?? [])
        {
            var sortedSubmitted = pattern.AlertIds
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var match = _compoundThreats.FirstOrDefault(compound =>
                compound.MemberAlertIds
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .SequenceEqual(sortedSubmitted, StringComparer.OrdinalIgnoreCase));

            if (match is not null)
            {
                if (creditedGroups.TryAdd(match.GroupId, true))
                {
                    lock (teamLock)
                    {
                        team.TotalScore += 5.0;
                        if (team.MaxPossibleScore > 0)
                            team.MissionEffectiveness = team.TotalScore / team.MaxPossibleScore * 100.0;
                    }

                    _compoundThreatsDetectedByTeam.AddOrUpdate(
                        report.TeamId, 1, static (_, v) => v + 1);

                    credited++;
                    logger.LogInformation(
                        "Team {TeamId} correctly identified compound threat {GroupId} (+5.0)",
                        report.TeamId, match.GroupId);
                }
            }
            else
            {
                lock (teamLock)
                {
                    team.TotalScore -= 1.0;
                    if (team.MaxPossibleScore > 0)
                        team.MissionEffectiveness = team.TotalScore / team.MaxPossibleScore * 100.0;
                }

                falsePositives++;
                logger.LogInformation(
                    "Team {TeamId} submitted false-positive pattern {PatternId} (-1.0)",
                    report.TeamId, pattern.PatternId);
            }
        }

        logger.LogInformation(
            "Pattern evaluation for {TeamId}: +{Credited} correct, -{FalsePositives} false positives",
            report.TeamId, credited, falsePositives);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EvaluatePredictionAsync(TeamPrediction prediction, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        // Hero mission bonus — logged for facilitator review; full scoring is post-exercise.
        logger.LogInformation(
            "Prediction logged for {TeamId}: sector={Sector}, confidence={Confidence:P0}",
            prediction.TeamId, prediction.PredictedSector, prediction.Confidence);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_initLock)
        {
            ClearState();
            _scoreUpdateTimer?.Dispose();
            _scoreUpdateTimer = null;
        }
    }

    /// <inheritdoc />
    public void SoftReset()
    {
        _processedDecisions.Clear();
        _detectedPatterns.Clear();
        _alertBroadcastTimes.Clear();
        _correctClassifications.Clear();
        _correctActions.Clear();
        _noiseAlertsCorrect.Clear();
        _noiseAlertsTotal.Clear();
        _compoundThreatsDetectedByTeam.Clear();

        teamTracker.ResetScores();

        logger.LogInformation("ScoringEngine soft-reset: team scores cleared, ground truth kept");
    }

    public IReadOnlyList<TeamResults> ExportResults()
    {
        var teams = teamTracker.GetAllTeams();
        var results = new List<TeamResults>(teams.Count);

        foreach (var team in teams)
        {
            int processed = team.AlertsProcessed;
            int missed = Math.Max(0, _groundTruth.Count - processed);

            double classAccuracy = processed > 0
                ? (double)_correctClassifications.GetValueOrDefault(team.TeamName, 0) / processed
                : 0.0;

            double actionAccuracy = processed > 0
                ? (double)_correctActions.GetValueOrDefault(team.TeamName, 0) / processed
                : 0.0;

            int noiseTotalForTeam = _noiseAlertsTotal.GetValueOrDefault(team.TeamName, 0);
            double noiseCorrectlyDismissed = noiseTotalForTeam > 0
                ? (double)_noiseAlertsCorrect.GetValueOrDefault(team.TeamName, 0) / noiseTotalForTeam
                : 0.0;

            results.Add(new TeamResults
            {
                Team = team.TeamName,
                FinalEffectiveness = Math.Round(team.MissionEffectiveness, 2),
                AlertsProcessed = processed,
                AlertsMissed = missed,
                AverageLatencyMs = team.AverageLatencyMs,
                ClassificationAccuracy = Math.Round(classAccuracy, 4),
                ActionAccuracy = Math.Round(actionAccuracy, 4),
                CompoundThreatsDetected = _compoundThreatsDetectedByTeam.GetValueOrDefault(team.TeamName, 0),
                CompoundThreatsTotal = _compoundThreats.Count,
                NoiseCorrectlyDismissed = Math.Round(noiseCorrectlyDismissed, 4)
            });
        }

        return results.AsReadOnly();
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void OnScoreTimerTick(object? state) =>
        _ = Task.Run(PushScoreUpdatesAsync);

    private async Task PushScoreUpdatesAsync()
    {
        var teams = teamTracker.GetAllTeams();

        foreach (var team in teams.Where(static t => t.IsConnected))
        {
            var update = new ScoreUpdate(
                TeamId: team.TeamName,
                MissionEffectiveness: Math.Round(team.MissionEffectiveness, 2),
                TotalScore: Math.Round(team.TotalScore, 3),
                MaxPossibleScore: Math.Round(team.MaxPossibleScore, 3),
                AlertsProcessed: team.AlertsProcessed,
                AverageLatencyMs: team.AverageLatencyMs,
                Timestamp: DateTime.UtcNow);

            try
            {
                // Send to the individual team and to the broadcast group (War Room leaderboard).
                await hubContext.Clients
                    .Group($"team.{team.TeamName.ToLowerInvariant()}")
                    .ReceiveScoreUpdate(update);

                await hubContext.Clients
                    .Group("broadcast")
                    .ReceiveScoreUpdate(update);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to push score update to team {TeamName}", team.TeamName);
            }
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose() => _scoreUpdateTimer?.Dispose();
}
