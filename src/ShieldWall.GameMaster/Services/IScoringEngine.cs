using ShieldWall.GameMaster.Models;
using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Evaluates team decisions, patterns, and predictions against ground truth.
/// Implemented in Feature 5 (Scoring Engine).
/// </summary>
public interface IScoringEngine
{
    /// <summary>
    /// Initialises the engine with scenario ground truth. Called by <see cref="AlertStreamEngine"/> at stream start.
    /// Resets all per-session state when called on a restart.
    /// </summary>
    /// <param name="scenario">The loaded scenario file containing alerts and compound threats.</param>
    void Initialize(ScenarioFile scenario);

    /// <summary>Records the wall-clock broadcast time for an alert ID, used to calculate team latency.</summary>
    /// <param name="alertId">The alert that was just broadcast.</param>
    /// <param name="broadcastTime">UTC timestamp when the alert reached connected teams.</param>
    void RecordAlertBroadcast(string alertId, DateTimeOffset broadcastTime);

    /// <summary>Evaluates a team's alert decision against ground truth and updates their score.</summary>
    /// <param name="decision">The submitted team decision.</param>
    /// <param name="connectionId">The submitting team's SignalR connection ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task EvaluateDecisionAsync(TeamDecision decision, string connectionId, CancellationToken ct);

    /// <summary>Evaluates detected threat patterns reported by a team.</summary>
    /// <param name="report">The team's pattern report.</param>
    /// <param name="ct">Cancellation token.</param>
    Task EvaluatePatternsAsync(TeamPatternReport report, CancellationToken ct);

    /// <summary>Evaluates a team's predictive sector guess for the hero mission phase.</summary>
    /// <param name="prediction">The team's sector prediction.</param>
    /// <param name="ct">Cancellation token.</param>
    Task EvaluatePredictionAsync(TeamPrediction prediction, CancellationToken ct);

    /// <summary>Exports a final result snapshot for all registered teams.</summary>
    IReadOnlyList<TeamResults> ExportResults();

    /// <summary>
    /// Clears all per-session scoring state. Called by <see cref="IGameOrchestrator.ResetAsync"/>
    /// so that a second run on the same server process starts with a clean slate.
    /// </summary>
    void Reset();

    /// <summary>
    /// Clears all team scoring state (processed decisions, accuracy counters) while keeping
    /// ground truth and compound threats loaded. Used when replaying a phase so teams start
    /// with a clean scoring slate without re-initialising the scenario.
    /// </summary>
    void SoftReset();
}