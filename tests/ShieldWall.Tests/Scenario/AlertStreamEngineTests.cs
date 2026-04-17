using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.GameMaster.Models;
using ShieldWall.GameMaster.Services;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;
using Xunit;

namespace ShieldWall.Tests.Scenario;

public sealed class AlertStreamEngineTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _scenarioPath;

    private readonly ISentinelHubClient _broadcastClient;
    private readonly IHubContext<SentinelHub, ISentinelHubClient> _hubContext;
    private readonly IScoringEngine _scoringEngine;
    private readonly PhaseManager _phaseManager;
    private readonly ScenarioLoader _scenarioLoader;
    private readonly IConfiguration _configuration;

    public AlertStreamEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _scenarioPath = Path.Combine(_tempDir, "test-scenario.json");

        WriteTestScenario(_scenarioPath);

        _broadcastClient = Substitute.For<ISentinelHubClient>();
        _broadcastClient.ReceiveAlert(Arg.Any<SentinelAlert>()).Returns(Task.CompletedTask);

        var hubClients = Substitute.For<IHubClients<ISentinelHubClient>>();
        hubClients.Group(Arg.Any<string>()).Returns(_broadcastClient);

        _hubContext = Substitute.For<IHubContext<SentinelHub, ISentinelHubClient>>();
        _hubContext.Clients.Returns(hubClients);

        _scoringEngine = Substitute.For<IScoringEngine>();
        _phaseManager = new PhaseManager(_hubContext, NullLogger<PhaseManager>.Instance);
        _scenarioLoader = new ScenarioLoader(NullLogger<ScenarioLoader>.Instance);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Scenario:Path"] = _scenarioPath })
            .Build();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static void WriteTestScenario(string path)
    {
        var scenario = new
        {
            metadata = new
            {
                totalAlerts = 8,
                durationMinutes = 60,
                phases = new[]
                {
                    new { name = "Phase 1", startMinute = 0,  endMinute = 15 },
                    new { name = "Phase 2", startMinute = 15, endMinute = 30 },
                    new { name = "Phase 3", startMinute = 30, endMinute = 45 },
                    new { name = "Phase 4", startMinute = 45, endMinute = 60 }
                }
            },
            alerts = new[]
            {
                MakeAlert("SA-0001", 60),
                MakeAlert("SA-0002", 300),
                MakeAlert("SA-0003", 950),
                MakeAlert("SA-0004", 1200),
                MakeAlert("SA-0005", 1500),
                MakeAlert("SA-0006", 2000),
                MakeAlert("SA-0007", 2800),
                MakeAlert("SA-0008", 3000),
            },
            compoundThreats = Array.Empty<object>()
        };

        File.WriteAllText(path, JsonSerializer.Serialize(scenario));
    }

    private static object MakeAlert(string alertId, int offsetSeconds) => new
    {
        alertId,
        broadcastOffsetSeconds = offsetSeconds,
        sector = "A-1",
        type = "cyber",
        rawSeverity = 5,
        confidenceScore = 0.8,
        source = "Sensor",
        groundTruth = new
        {
            correctClassification = "high",
            correctAction = "escalate",
            isCompoundMember = false,
            compoundGroupId = (string?)null
        }
    };

    private AlertStreamEngine CreateEngine() =>
        new(_hubContext, _scoringEngine, _phaseManager, _scenarioLoader, _configuration,
            TimeProvider.System, NullLogger<AlertStreamEngine>.Instance);

    // ── GetPhaseAlertRange ───────────────────────────────────────────────────

    [Fact]
    public void GetPhaseAlertRange_Phase1_ReturnsCorrectRange()
    {
        var engine = CreateEngine();

        var (startIndex, count) = engine.GetPhaseAlertRange(1);

        Assert.Equal(0, startIndex);
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetPhaseAlertRange_Phase2_ReturnsCorrectRange()
    {
        var engine = CreateEngine();

        var (startIndex, count) = engine.GetPhaseAlertRange(2);

        Assert.Equal(2, startIndex);
        Assert.Equal(3, count);
    }

    [Fact]
    public void GetPhaseAlertRange_Phase4_ReturnsCorrectRange()
    {
        var engine = CreateEngine();

        var (startIndex, count) = engine.GetPhaseAlertRange(4);

        Assert.Equal(6, startIndex);
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetPhaseAlertRange_InvalidPhase0_ThrowsArgumentOutOfRange()
    {
        var engine = CreateEngine();

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.GetPhaseAlertRange(0));
    }

    [Fact]
    public void GetPhaseAlertRange_InvalidPhase5_ThrowsArgumentOutOfRange()
    {
        var engine = CreateEngine();

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.GetPhaseAlertRange(5));
    }

    // ── DispatchPhaseAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DispatchPhaseAsync_Phase1_DispatchesTwoAlerts()
    {
        var ct = TestContext.Current.CancellationToken;
        var engine = CreateEngine();

        var result = await engine.DispatchPhaseAsync(1, ct);

        Assert.Equal(2, result.Count);
        await _broadcastClient.Received(2).ReceiveAlert(Arg.Any<SentinelAlert>());
        _scoringEngine.Received(2).RecordAlertBroadcast(Arg.Any<string>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task DispatchPhaseAsync_Phase2_DispatchesThreeAlerts()
    {
        var ct = TestContext.Current.CancellationToken;
        var engine = CreateEngine();

        var result = await engine.DispatchPhaseAsync(2, ct);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task DispatchPhaseAsync_CalledTwice_SecondCallStillDispatches()
    {
        var ct = TestContext.Current.CancellationToken;
        var engine = CreateEngine();

        var first = await engine.DispatchPhaseAsync(1, ct);
        var second = await engine.DispatchPhaseAsync(1, ct);

        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        await _broadcastClient.Received(4).ReceiveAlert(Arg.Any<SentinelAlert>());
    }

    [Fact]
    public async Task ReplayPhaseAsync_RewindsAndDispatches()
    {
        var ct = TestContext.Current.CancellationToken;
        var engine = CreateEngine();

        await engine.StartAsync(ct);
        await engine.DispatchPhaseAsync(2, ct); // advances index past phase 2 (index = 5)

        var replayed = await engine.ReplayPhaseAsync(1, ct); // rewinds to index 0 and dispatches phase 1

        Assert.Equal(2, replayed.Count);
        Assert.Equal("SA-0001", replayed[0].AlertId);
        Assert.Equal("SA-0002", replayed[1].AlertId);
    }

    [Fact]
    public async Task DispatchWaveAsync_AfterPhaseDispatch_ContinuesFromPhaseEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var engine = CreateEngine();

        await engine.DispatchPhaseAsync(1, ct); // nextAlertIndex = 2 after dispatch

        var wave = await engine.DispatchWaveAsync(1, ct);

        Assert.Single(wave);
        Assert.Equal("SA-0003", wave[0].AlertId);
    }
}
