using Microsoft.AspNetCore.SignalR;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.GameMaster.Models;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Timer-based deterministic alert broadcast engine.
/// Reads scenario offsets from <see cref="ScenarioFile"/> and fires alerts at the correct wall-clock time,
/// respecting pause/resume by accumulating pause duration.
/// </summary>
public sealed class AlertStreamEngine(
    IHubContext<SentinelHub, ISentinelHubClient> hubContext,
    IScoringEngine scoringEngine,
    PhaseManager phaseManager,
    ScenarioLoader scenarioLoader,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<AlertStreamEngine> logger) : IAlertStreamEngine, IDisposable
{
    private const string BroadcastGroup = "broadcast";

    // State — modified only in Start/Pause/Resume/Reset or DispatchPhase.
    private ScenarioFile? _scenario;
    private DateTimeOffset _streamStartTime;
    private DateTimeOffset _pauseStartTime;
    private TimeSpan _totalPauseDuration;
    private int _nextAlertIndex;

    // volatile: cross-thread flag reads don't need full barriers but must see latest value.
    private volatile bool _isRunning;
    private volatile bool _isPaused;

    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Lazily loads the scenario for read-only operations (upcoming alerts, progress)
    /// so the GM Panel can show queue data before the stream is started.
    /// </summary>
    private ScenarioFile EnsureScenarioLoaded()
    {
        if (_scenario is not null)
            return _scenario;

        var scenarioPath = configuration["Scenario:Path"] ?? "Data/alert-scenario.json";
        _scenario = scenarioLoader.Load(scenarioPath);
        return _scenario;
    }

    public Task StartAsync(CancellationToken ct)
    {
        if (_isRunning)
            throw new InvalidOperationException("Alert stream is already running.");

        var scenarioPath = configuration["Scenario:Path"] ?? "Data/alert-scenario.json";
        _scenario = scenarioLoader.Load(scenarioPath);

        scoringEngine.Initialize(_scenario);

        _streamStartTime = timeProvider.GetUtcNow();
        _totalPauseDuration = TimeSpan.Zero;
        _nextAlertIndex = 0;
        _isPaused = false;
        _isRunning = true;

        logger.LogInformation(
            "Alert stream ready for phase dispatch — {AlertCount} alerts across {PhaseCount} phases",
            _scenario.Alerts.Count, _scenario.Metadata.Phases.Count);
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken ct)
    {
        if (!_isRunning || _isPaused)
            return Task.CompletedTask;

        // Record pause timestamp for elapsed-time logging in ResumeAsync.
        _pauseStartTime = timeProvider.GetUtcNow();
        _isPaused = true;

        var elapsed = _pauseStartTime - _streamStartTime - _totalPauseDuration;
        logger.LogInformation("Alert stream paused at {ElapsedSeconds:F1}s", elapsed.TotalSeconds);
        return Task.CompletedTask;
    }

    public Task ResumeAsync(CancellationToken ct)
    {
        if (!_isRunning || !_isPaused)
            return Task.CompletedTask;

        _totalPauseDuration += timeProvider.GetUtcNow() - _pauseStartTime;
        _isPaused = false;

        logger.LogInformation("Alert stream resumed");
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken ct)
    {
        _isRunning = false;
        _isPaused = false;
        _nextAlertIndex = 0;
        _totalPauseDuration = TimeSpan.Zero;
        phaseManager.Reset();

        logger.LogInformation("Alert stream reset");
        return Task.CompletedTask;
    }

    public void Dispose() { }

    // ── Wave dispatch ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ScenarioAlert>> DispatchWaveAsync(int count, CancellationToken ct)
    {
        if (_scenario is null)
        {
            var scenarioPath = configuration["Scenario:Path"] ?? "Data/alert-scenario.json";
            _scenario = scenarioLoader.Load(scenarioPath);
            scoringEngine.Initialize(_scenario);
        }

        var dispatched = new List<ScenarioAlert>();
        var alerts = _scenario.Alerts;

        for (var i = 0; i < count && _nextAlertIndex < alerts.Count; i++)
        {
            if (i > 0)
                await Task.Delay(500, ct);

            var now = timeProvider.GetUtcNow();
            var scenarioAlert = alerts[_nextAlertIndex];
            var sentinelAlert = ToSentinelAlert(scenarioAlert, now.UtcDateTime);

            await hubContext.Clients.Group(BroadcastGroup).ReceiveAlert(sentinelAlert);
            scoringEngine.RecordAlertBroadcast(sentinelAlert.AlertId, now);
            dispatched.Add(scenarioAlert);

            logger.LogInformation(
                "Wave-dispatched alert {AlertId} ({Type}, Sector {Sector})",
                sentinelAlert.AlertId, sentinelAlert.Type, sentinelAlert.Sector);

            _nextAlertIndex++;
        }

        return dispatched;
    }

    // ── Phase dispatch ────────────────────────────────────────────────────────

    public (int StartIndex, int Count) GetPhaseAlertRange(int phaseNumber)
    {
        var scenario = EnsureScenarioLoaded();
        var phases = scenario.Metadata.Phases;

        if (phaseNumber < 1 || phaseNumber > phases.Count)
            throw new ArgumentOutOfRangeException(
                nameof(phaseNumber),
                $"Phase number must be between 1 and {phases.Count}. Got: {phaseNumber}.");

        var phase = phases[phaseNumber - 1];
        var startSec = phase.StartMinute * 60;
        var endSec = phase.EndMinute * 60;

        var alerts = scenario.Alerts;
        var startIndex = alerts.Count; // default: past-the-end when no alerts belong to phase
        var count = 0;

        for (var i = 0; i < alerts.Count; i++)
        {
            var offset = alerts[i].BroadcastOffsetSeconds;
            if (offset >= startSec && offset < endSec)
            {
                if (count == 0) startIndex = i;
                count++;
            }
        }

        return (startIndex, count);
    }

    public async Task<IReadOnlyList<ScenarioAlert>> DispatchPhaseAsync(int phaseNumber, CancellationToken ct)
    {
        if (_scenario is null)
        {
            var scenarioPath = configuration["Scenario:Path"] ?? "Data/alert-scenario.json";
            _scenario = scenarioLoader.Load(scenarioPath);
            scoringEngine.Initialize(_scenario);
        }

        var (startIndex, count) = GetPhaseAlertRange(phaseNumber);

        if (count == 0)
        {
            logger.LogWarning("Phase {PhaseNumber} contains no alerts — nothing dispatched", phaseNumber);
            return [];
        }

        var phaseName = _scenario.Metadata.Phases[phaseNumber - 1].Name;
        var alerts = _scenario.Alerts;
        var dispatched = new List<ScenarioAlert>(count);

        for (var i = startIndex; i < startIndex + count; i++)
        {
            ct.ThrowIfCancellationRequested();

            if (i > startIndex)
                await Task.Delay(500, ct);

            var now = timeProvider.GetUtcNow();
            var scenarioAlert = alerts[i];
            var sentinelAlert = ToSentinelAlert(scenarioAlert, now.UtcDateTime);

            await hubContext.Clients.Group(BroadcastGroup).ReceiveAlert(sentinelAlert);
            scoringEngine.RecordAlertBroadcast(sentinelAlert.AlertId, now);
            dispatched.Add(scenarioAlert);

            logger.LogInformation(
                "Phase {PhaseNumber} dispatched alert {AlertId} ({Type}, Sector {Sector})",
                phaseNumber, sentinelAlert.AlertId, sentinelAlert.Type, sentinelAlert.Sector);
        }

        // Advance _nextAlertIndex to the end of this phase (never move it backwards here).
        if (_nextAlertIndex < startIndex + count)
            _nextAlertIndex = startIndex + count;

        logger.LogInformation(
            "Phase {PhaseNumber} ({PhaseName}) dispatch complete — {Count} alert(s) sent",
            phaseNumber, phaseName, dispatched.Count);

        return dispatched;
    }

    public async Task<IReadOnlyList<ScenarioAlert>> ReplayPhaseAsync(int phaseNumber, CancellationToken ct)
    {
        var (startIndex, _) = GetPhaseAlertRange(phaseNumber);

        // Rewind index to the beginning of this phase before dispatching.
        _nextAlertIndex = startIndex;

        logger.LogInformation(
            "Replaying phase {PhaseNumber} — rewound index to {StartIndex}",
            phaseNumber, startIndex);

        return await DispatchPhaseAsync(phaseNumber, ct);
    }

    public IReadOnlyList<ScenarioAlert> GetUpcomingAlerts(int count)
    {
        var scenario = EnsureScenarioLoaded();
        return scenario.Alerts
            .Skip(_nextAlertIndex)
            .Take(count)
            .ToList();
    }

    public AlertProgress GetProgress()
    {
        var scenario = EnsureScenarioLoaded();
        var sent = _nextAlertIndex;
        var total = scenario.Alerts.Count;
        var allAlerts = scenario.Alerts;
        var phases = scenario.Metadata.Phases.Select(phase =>
        {
            var startSec = phase.StartMinute * 60;
            var endSec = phase.EndMinute * 60;
            var phaseTotal = 0;
            var phaseSent = 0;
            for (var i = 0; i < allAlerts.Count; i++)
            {
                if (allAlerts[i].BroadcastOffsetSeconds >= startSec &&
                    allAlerts[i].BroadcastOffsetSeconds < endSec)
                {
                    phaseTotal++;
                    if (i < _nextAlertIndex) phaseSent++;
                }
            }
            return new PhaseProgress(phase.Name, phaseSent, phaseTotal);
        }).ToList();

        return new AlertProgress(sent, total, phases);
    }

    /// <summary>
    /// Converts a <see cref="ScenarioAlert"/> to a <see cref="SentinelAlert"/>,
    /// intentionally stripping the ground truth so teams cannot see the answers.
    /// </summary>
    private static SentinelAlert ToSentinelAlert(ScenarioAlert source, DateTime timestamp) =>
        new()
        {
            AlertId = source.AlertId,
            Timestamp = timestamp,
            Sector = source.Sector,
            Type = Enum.Parse<AlertType>(source.Type, ignoreCase: true),
            RawSeverity = source.RawSeverity,
            ConfidenceScore = source.ConfidenceScore,
            Source = source.Source,
            CorrelationGroup = source.CorrelationGroup,
            Metadata = new Dictionary<string, string>(source.Metadata)
        };
}
