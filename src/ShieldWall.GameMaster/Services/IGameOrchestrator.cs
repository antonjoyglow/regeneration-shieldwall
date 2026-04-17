using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Orchestrates the workshop exercise lifecycle: Lobby → Briefing → Live ↔ Paused → Completed.
/// All facilitator actions route through here to enforce valid state transitions.
/// </summary>
public interface IGameOrchestrator
{
    /// <summary>Current exercise lifecycle phase.</summary>
    GamePhase CurrentGamePhase { get; }

    /// <summary>UTC timestamp when briefing started, or null if not yet started.</summary>
    DateTimeOffset? BriefingStartedAt { get; }

    /// <summary>UTC timestamp when the alert stream started, or null if not yet started.</summary>
    DateTimeOffset? StreamStartedAt { get; }

    /// <summary>Transition from Lobby to Briefing. Teams see instructions to read their code.</summary>
    Task StartBriefingAsync(CancellationToken ct);

    /// <summary>Transition from Briefing to Live. Starts the alert stream and scoring engine.</summary>
    Task StartStreamAsync(CancellationToken ct);

    /// <summary>Toggle between Live and Paused states.</summary>
    Task TogglePauseAsync(CancellationToken ct);

    /// <summary>Immediately dispatch the next <paramref name="count"/> alerts (manual wave push).</summary>
    Task<IReadOnlyList<Models.ScenarioAlert>> DispatchWaveAsync(int count, CancellationToken ct);

    /// <summary>Dispatch all alerts for <paramref name="phaseNumber"/> (1–4) and force the phase transition.</summary>
    Task<IReadOnlyList<Models.ScenarioAlert>> DispatchPhaseAsync(int phaseNumber, CancellationToken ct);

    /// <summary>Re-broadcast all alerts for <paramref name="phaseNumber"/> (1–4) and force the phase transition.</summary>
    Task<IReadOnlyList<Models.ScenarioAlert>> ReplayPhaseAsync(int phaseNumber, CancellationToken ct);

    /// <summary>End the exercise. Freezes scores and stops the stream.</summary>
    Task EndGameAsync(CancellationToken ct);

    /// <summary>Reset everything back to Lobby state.</summary>
    Task ResetAsync(CancellationToken ct);
}
