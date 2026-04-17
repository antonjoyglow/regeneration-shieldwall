using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.GameMaster.Models;
using ShieldWall.GameMaster.Services;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;
using Xunit;

namespace ShieldWall.Tests.Orchestrator;

public sealed class GameOrchestratorTests
{
    private readonly IAlertStreamEngine _alertStream;
    private readonly IScoringEngine _scoringEngine;
    private readonly ITeamTracker _teamTracker;
    private readonly IPhaseManager _phaseManager;
    private readonly IHubContext<SentinelHub, ISentinelHubClient> _hubContext;
    private readonly ISentinelHubClient _broadcastClient;

    public GameOrchestratorTests()
    {
        _alertStream = Substitute.For<IAlertStreamEngine>();
        _scoringEngine = Substitute.For<IScoringEngine>();
        _teamTracker = Substitute.For<ITeamTracker>();
        _phaseManager = Substitute.For<IPhaseManager>();

        _broadcastClient = Substitute.For<ISentinelHubClient>();
        _broadcastClient.ReceiveGameStateChange(Arg.Any<GamePhase>()).Returns(Task.CompletedTask);

        var hubClients = Substitute.For<IHubClients<ISentinelHubClient>>();
        hubClients.Group(Arg.Any<string>()).Returns(_broadcastClient);

        _hubContext = Substitute.For<IHubContext<SentinelHub, ISentinelHubClient>>();
        _hubContext.Clients.Returns(hubClients);

        _alertStream.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _alertStream.PauseAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _alertStream.ResumeAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _alertStream.ResetAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _alertStream.DispatchWaveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ScenarioAlert>>([]));
        _alertStream.DispatchPhaseAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ScenarioAlert>>([]));
        _alertStream.ReplayPhaseAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ScenarioAlert>>([]));
        _phaseManager.ForcePhaseAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private GameOrchestrator CreateOrchestrator() =>
        new(_alertStream, _scoringEngine, _teamTracker, _phaseManager, _hubContext,
            TimeProvider.System, NullLogger<GameOrchestrator>.Instance);

    private async Task AdvanceToLiveAsync(GameOrchestrator orchestrator)
    {
        var ct = TestContext.Current.CancellationToken;
        await orchestrator.StartBriefingAsync(ct);
        await orchestrator.StartStreamAsync(ct);
    }

    // ── DispatchPhaseAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DispatchPhaseAsync_WhenLive_DelegatesToAlertStream()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();
        await AdvanceToLiveAsync(orchestrator);

        await orchestrator.DispatchPhaseAsync(1, ct);

        await _alertStream.Received(1).DispatchPhaseAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPhaseAsync_WhenLobby_ThrowsInvalidOperationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.DispatchPhaseAsync(1, ct));
    }

    [Fact]
    public async Task DispatchPhaseAsync_WhenBriefing_ThrowsInvalidOperationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();
        await orchestrator.StartBriefingAsync(ct);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.DispatchPhaseAsync(1, ct));
    }

    [Fact]
    public async Task DispatchPhaseAsync_WhenPaused_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();
        await AdvanceToLiveAsync(orchestrator);
        await orchestrator.TogglePauseAsync(ct); // Live → Paused

        var exception = await Record.ExceptionAsync(() => orchestrator.DispatchPhaseAsync(1, ct));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DispatchPhaseAsync_ForcesPhaseTransition()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();
        await AdvanceToLiveAsync(orchestrator);

        await orchestrator.DispatchPhaseAsync(3, ct);

        await _phaseManager.Received(1).ForcePhaseAsync(3, Arg.Any<CancellationToken>());
    }

    // ── ReplayPhaseAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ReplayPhaseAsync_WhenLive_CallsSoftResetThenDispatches()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();
        await AdvanceToLiveAsync(orchestrator);

        await orchestrator.ReplayPhaseAsync(2, ct);

        Received.InOrder(() =>
        {
            _scoringEngine.SoftReset();
            _alertStream.ReplayPhaseAsync(2, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ReplayPhaseAsync_WhenLobby_ThrowsInvalidOperationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.ReplayPhaseAsync(1, ct));
    }

    [Fact]
    public async Task ReplayPhaseAsync_ForcesPhaseTransition()
    {
        var ct = TestContext.Current.CancellationToken;
        var orchestrator = CreateOrchestrator();
        await AdvanceToLiveAsync(orchestrator);

        await orchestrator.ReplayPhaseAsync(2, ct);

        await _phaseManager.Received(1).ForcePhaseAsync(2, Arg.Any<CancellationToken>());
    }
}
