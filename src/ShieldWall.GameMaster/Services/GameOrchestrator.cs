using Microsoft.AspNetCore.SignalR;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.GameMaster.Models;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Hubs;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Enforces the workshop lifecycle state machine and coordinates all sub-services.
/// State transitions: Lobby → Briefing → Live ↔ Paused → Completed.
/// Reset returns to Lobby from any state.
/// </summary>
public sealed class GameOrchestrator(
    IAlertStreamEngine alertStream,
    IScoringEngine scoringEngine,
    ITeamTracker teamTracker,
    IPhaseManager phaseManager,
    IHubContext<SentinelHub, ISentinelHubClient> hubContext,
    TimeProvider timeProvider,
    ILogger<GameOrchestrator> logger) : IGameOrchestrator
{
    private readonly Lock _lock = new();
    private GamePhase _currentPhase = GamePhase.Lobby;
    private DateTimeOffset? _briefingStartedAt;
    private DateTimeOffset? _streamStartedAt;

    public GamePhase CurrentGamePhase => _currentPhase;
    public DateTimeOffset? BriefingStartedAt => _briefingStartedAt;
    public DateTimeOffset? StreamStartedAt => _streamStartedAt;

    public async Task StartBriefingAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_currentPhase != GamePhase.Lobby)
                throw new InvalidOperationException($"Cannot start briefing from {_currentPhase} state.");
            _currentPhase = GamePhase.Briefing;
            _briefingStartedAt = timeProvider.GetUtcNow();
        }

        logger.LogInformation("Exercise transitioned to Briefing");
        await hubContext.Clients.Group("broadcast").ReceiveGameStateChange(GamePhase.Briefing);
    }

    public async Task StartStreamAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_currentPhase != GamePhase.Briefing)
                throw new InvalidOperationException($"Cannot start stream from {_currentPhase} state.");
            _currentPhase = GamePhase.Live;
            _streamStartedAt = timeProvider.GetUtcNow();
        }

        await alertStream.StartAsync(ct);
        logger.LogInformation("Exercise transitioned to Live — alert stream started");
        await hubContext.Clients.Group("broadcast").ReceiveGameStateChange(GamePhase.Live);
    }

    public async Task TogglePauseAsync(CancellationToken ct)
    {
        GamePhase newPhase;

        lock (_lock)
        {
            newPhase = _currentPhase switch
            {
                GamePhase.Live => GamePhase.Paused,
                GamePhase.Paused => GamePhase.Live,
                _ => throw new InvalidOperationException($"Cannot toggle pause from {_currentPhase} state.")
            };
            _currentPhase = newPhase;
        }

        if (newPhase == GamePhase.Paused)
            await alertStream.PauseAsync(ct);
        else
            await alertStream.ResumeAsync(ct);

        logger.LogInformation("Exercise transitioned to {Phase}", newPhase);
        await hubContext.Clients.Group("broadcast").ReceiveGameStateChange(newPhase);
    }

    public async Task<IReadOnlyList<ScenarioAlert>> DispatchWaveAsync(int count, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_currentPhase is not (GamePhase.Live or GamePhase.Paused))
                throw new InvalidOperationException($"Cannot dispatch waves in {_currentPhase} state.");
        }

        var dispatched = await alertStream.DispatchWaveAsync(count, ct);
        logger.LogInformation("Game Master dispatched wave of {Count} alerts", dispatched.Count);
        return dispatched;
    }

    public async Task<IReadOnlyList<ScenarioAlert>> DispatchPhaseAsync(int phaseNumber, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_currentPhase is not (GamePhase.Live or GamePhase.Paused))
                throw new InvalidOperationException($"Cannot dispatch phase in {_currentPhase} state.");
        }

        var dispatched = await alertStream.DispatchPhaseAsync(phaseNumber, ct);
        await phaseManager.ForcePhaseAsync(phaseNumber, ct);
        logger.LogInformation("Game Master dispatched phase {Phase} ({Count} alerts)", phaseNumber, dispatched.Count);
        return dispatched;
    }

    public async Task<IReadOnlyList<ScenarioAlert>> ReplayPhaseAsync(int phaseNumber, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_currentPhase is not (GamePhase.Live or GamePhase.Paused))
                throw new InvalidOperationException($"Cannot replay phase in {_currentPhase} state.");
        }

        scoringEngine.SoftReset();

        var dispatched = await alertStream.ReplayPhaseAsync(phaseNumber, ct);
        await phaseManager.ForcePhaseAsync(phaseNumber, ct);
        logger.LogInformation("Game Master replayed phase {Phase} ({Count} alerts) — scores reset", phaseNumber, dispatched.Count);
        return dispatched;
    }

    public async Task EndGameAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_currentPhase is not (GamePhase.Live or GamePhase.Paused))
                throw new InvalidOperationException($"Cannot end game from {_currentPhase} state.");
            _currentPhase = GamePhase.Completed;
        }

        if (alertStream.IsRunning)
            await alertStream.PauseAsync(ct);

        logger.LogInformation("Exercise transitioned to Completed");
        await hubContext.Clients.Group("broadcast").ReceiveGameStateChange(GamePhase.Completed);
    }

    public async Task ResetAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            _currentPhase = GamePhase.Lobby;
            _briefingStartedAt = null;
            _streamStartedAt = null;
        }

        await alertStream.ResetAsync(ct);
        scoringEngine.Reset();
        teamTracker.ResetScores();
        logger.LogInformation("Exercise reset to Lobby");
        await hubContext.Clients.Group("broadcast").ReceiveGameStateChange(GamePhase.Lobby);
    }
}
