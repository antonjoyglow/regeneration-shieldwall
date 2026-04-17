using Microsoft.AspNetCore.SignalR;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Services;

public sealed class PhaseManager(
    IHubContext<SentinelHub, ISentinelHubClient> hubContext,
    ILogger<PhaseManager> logger) : IPhaseManager
{
    private const string BroadcastGroup = "broadcast";

    private static readonly PhaseInfo[] Phases =
    [
        new("Calm Waters",        1, 0,  15),
        new("Fog of War",         2, 15, 30),
        new("Pattern Recognition", 3, 30, 45),
        new("The Storm",          4, 45, 60)
    ];

    // Accessed via Interlocked and Volatile — do NOT mark volatile (CS0420).
    private int _currentPhaseIndex;

    public PhaseInfo CurrentPhase => Phases[Volatile.Read(ref _currentPhaseIndex)];

    /// <summary>
    /// Called by <see cref="AlertStreamEngine"/> on each tick.
    /// Transitions through any phase boundaries that <paramref name="elapsedSeconds"/> has crossed.
    /// Thread-safe via compare-and-swap — safe to call from a single timer thread.
    /// </summary>
    public async Task CheckAndTransitionAsync(int elapsedSeconds, CancellationToken ct)
    {
        var elapsedMinutes = elapsedSeconds / 60;

        while (true)
        {
            var currentIndex = Volatile.Read(ref _currentPhaseIndex);
            var nextIndex = currentIndex + 1;

            if (nextIndex >= Phases.Length)
                break;

            if (elapsedMinutes < Phases[nextIndex].StartMinute)
                break;

            // CAS ensures only one caller completes the transition even under concurrent access.
            if (Interlocked.CompareExchange(ref _currentPhaseIndex, nextIndex, currentIndex) != currentIndex)
                continue; // Another caller raced ahead — re-evaluate from new index.

            var newPhase = Phases[nextIndex];
            logger.LogInformation(
                "Phase transition: entering '{PhaseName}' (Phase {PhaseNumber}) at {ElapsedSeconds}s",
                newPhase.Name, newPhase.PhaseNumber, elapsedSeconds);

            await hubContext.Clients.Group(BroadcastGroup).ReceivePhaseChange(newPhase).WaitAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task ForcePhaseAsync(int phaseNumber, CancellationToken ct)
    {
        if (phaseNumber < 1 || phaseNumber > Phases.Length)
            throw new ArgumentOutOfRangeException(
                nameof(phaseNumber),
                $"Phase number must be between 1 and {Phases.Length}, got {phaseNumber}.");

        var index = phaseNumber - 1;
        Interlocked.Exchange(ref _currentPhaseIndex, index);

        var phase = Phases[index];
        logger.LogInformation(
            "Phase forced to '{PhaseName}' (Phase {PhaseNumber})",
            phase.Name, phase.PhaseNumber);

        await hubContext.Clients.Group(BroadcastGroup).ReceivePhaseChange(phase).WaitAsync(ct);
    }

    /// <summary>Resets to Phase 1. Called by <see cref="AlertStreamEngine.ResetAsync"/>.</summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _currentPhaseIndex, 0);
        logger.LogInformation("Phase manager reset to Phase 1 (Calm Waters)");
    }
}
