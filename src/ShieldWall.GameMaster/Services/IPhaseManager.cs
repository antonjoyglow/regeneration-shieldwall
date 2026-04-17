using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Manages exercise phases and phase transitions.
/// Implemented in Feature 4 (Scenario Engine).
/// </summary>
public interface IPhaseManager
{
    /// <summary>Gets the current exercise phase.</summary>
    PhaseInfo CurrentPhase { get; }

    /// <summary>Forces an immediate transition to the specified phase number.</summary>
    /// <param name="phaseNumber">The 1-based phase number to advance to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ForcePhaseAsync(int phaseNumber, CancellationToken ct);
}