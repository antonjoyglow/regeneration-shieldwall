using ShieldWall.GameMaster.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Controls the alert stream that drives the workshop scenario.
/// Implemented in Feature 4 (Scenario Engine).
/// </summary>
public interface IAlertStreamEngine
{
    /// <summary>Starts streaming alerts to connected teams.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task StartAsync(CancellationToken ct);

    /// <summary>Pauses the alert stream without resetting state.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task PauseAsync(CancellationToken ct);

    /// <summary>Resumes a paused alert stream.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task ResumeAsync(CancellationToken ct);

    /// <summary>Resets the scenario to its initial state.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task ResetAsync(CancellationToken ct);

    /// <summary>Immediately dispatches the next <paramref name="count"/> alerts regardless of their scheduled offset.</summary>
    /// <param name="count">Number of alerts to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The scenario alerts that were dispatched (with ground truth for GM reference).</returns>
    Task<IReadOnlyList<ScenarioAlert>> DispatchWaveAsync(int count, CancellationToken ct);

    /// <summary>Returns the next <paramref name="count"/> alerts that have not yet been broadcast, including ground truth.</summary>
    IReadOnlyList<ScenarioAlert> GetUpcomingAlerts(int count);

    /// <summary>Returns current alert broadcast progress.</summary>
    AlertProgress GetProgress();

    /// <summary>
    /// Dispatches all alerts belonging to the given phase number (1-based).
    /// Loads the scenario if not yet loaded and advances <c>_nextAlertIndex</c> past the phase.
    /// </summary>
    Task<IReadOnlyList<ScenarioAlert>> DispatchPhaseAsync(int phaseNumber, CancellationToken ct);

    /// <summary>
    /// Rewinds <c>_nextAlertIndex</c> to the beginning of the given phase, then dispatches all its alerts.
    /// Useful for re-running a phase after teams have submitted decisions.
    /// </summary>
    Task<IReadOnlyList<ScenarioAlert>> ReplayPhaseAsync(int phaseNumber, CancellationToken ct);

    /// <summary>
    /// Returns the zero-based start index and count of alerts belonging to the given phase number (1-based).
    /// </summary>
    (int StartIndex, int Count) GetPhaseAlertRange(int phaseNumber);

    /// <summary>Returns true if the alert stream is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Returns true if the alert stream is paused.</summary>
    bool IsPaused { get; }
}