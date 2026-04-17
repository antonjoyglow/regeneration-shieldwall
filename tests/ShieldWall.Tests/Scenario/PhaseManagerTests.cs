using Xunit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.GameMaster.Services;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;

namespace ShieldWall.Tests.Scenario;

public sealed class PhaseManagerTests
{
    private static PhaseManager CreatePhaseManager()
    {
        var broadcastClient = Substitute.For<ISentinelHubClient>();
        var hubClients = Substitute.For<IHubClients<ISentinelHubClient>>();
        hubClients.Group(Arg.Any<string>()).Returns(broadcastClient);
        var hubContext = Substitute.For<IHubContext<SentinelHub, ISentinelHubClient>>();
        hubContext.Clients.Returns(hubClients);
        return new PhaseManager(hubContext, NullLogger<PhaseManager>.Instance);
    }

    [Fact]
    public async Task CheckAndTransitionAsync_AtMinute0_StaysInPhase1()
    {
        var phaseManager = CreatePhaseManager();

        await phaseManager.CheckAndTransitionAsync(0, TestContext.Current.CancellationToken);

        Assert.Equal(1, phaseManager.CurrentPhase.PhaseNumber);
    }

    [Fact]
    public async Task CheckAndTransitionAsync_AtMinute15_TransitionsToPhase2()
    {
        var phaseManager = CreatePhaseManager();

        await phaseManager.CheckAndTransitionAsync(15 * 60, TestContext.Current.CancellationToken);

        Assert.Equal(2, phaseManager.CurrentPhase.PhaseNumber);
    }

    [Fact]
    public async Task CheckAndTransitionAsync_AtMinute30_TransitionsToPhase3()
    {
        var phaseManager = CreatePhaseManager();

        await phaseManager.CheckAndTransitionAsync(30 * 60, TestContext.Current.CancellationToken);

        Assert.Equal(3, phaseManager.CurrentPhase.PhaseNumber);
    }

    [Fact]
    public async Task CheckAndTransitionAsync_AtMinute45_TransitionsToPhase4()
    {
        var phaseManager = CreatePhaseManager();

        await phaseManager.CheckAndTransitionAsync(45 * 60, TestContext.Current.CancellationToken);

        Assert.Equal(4, phaseManager.CurrentPhase.PhaseNumber);
    }

    [Fact]
    public async Task ForcePhaseAsync_ToPhase3_SetsCurrentPhase()
    {
        var phaseManager = CreatePhaseManager();

        await phaseManager.ForcePhaseAsync(3, TestContext.Current.CancellationToken);

        Assert.Equal(3, phaseManager.CurrentPhase.PhaseNumber);
    }

    [Fact]
    public async Task ForcePhaseAsync_InvalidPhase_ThrowsArgumentOutOfRangeException()
    {
        var phaseManager = CreatePhaseManager();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => phaseManager.ForcePhaseAsync(5, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Reset_AfterPhase3_ReturnsToPhase1()
    {
        var phaseManager = CreatePhaseManager();
        await phaseManager.ForcePhaseAsync(3, TestContext.Current.CancellationToken);

        phaseManager.Reset();

        Assert.Equal(1, phaseManager.CurrentPhase.PhaseNumber);
    }
}
