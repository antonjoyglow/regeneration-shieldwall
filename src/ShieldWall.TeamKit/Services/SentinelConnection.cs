using Microsoft.AspNetCore.SignalR.Client;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;

namespace ShieldWall.TeamKit.Services;

public sealed class SentinelConnection(
    IConfiguration configuration,
    ILogger<SentinelConnection> logger) : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly string _hubUrl = configuration["GameMaster:HubUrl"]
        ?? throw new InvalidOperationException("GameMaster:HubUrl is not configured.");
    private readonly string _teamName = configuration["Team:Name"]
        ?? throw new InvalidOperationException("Team:Name is not configured.");

    public event Func<SentinelAlert, Task>? AlertReceived;
    public event Func<ScoreUpdate, Task>? ScoreUpdated;
    public event Func<PhaseInfo, Task>? PhaseChanged;
    public event Func<string, Task>? AnnouncementReceived;
    public event Func<HubConnectionState, Task>? ConnectionStateChanged;
    public event Func<GamePhase, Task>? GameStateChanged;

    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;
    public string TeamName => _teamName;

    public async Task ConnectAsync(CancellationToken ct)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect([
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            ])
            .Build();

        _hubConnection.On<SentinelAlert>(nameof(ISentinelHubClient.ReceiveAlert),
            async alert =>
            {
                logger.LogDebug("ReceiveAlert: {AlertId} Type={Type} Sector={Sector} Sev={Sev}",
                    alert.AlertId, alert.Type, alert.Sector, alert.RawSeverity);
                await InvokeAllAsync(AlertReceived, alert);
            });

        _hubConnection.On<ScoreUpdate>(nameof(ISentinelHubClient.ReceiveScoreUpdate),
            async update =>
            {
                logger.LogDebug("ReceiveScoreUpdate: {Effectiveness}% ({Processed} alerts, {Latency}ms)",
                    update.MissionEffectiveness, update.AlertsProcessed, update.AverageLatencyMs);
                await InvokeAllAsync(ScoreUpdated, update);
            });

        _hubConnection.On<PhaseInfo>(nameof(ISentinelHubClient.ReceivePhaseChange),
            async phase =>
            {
                logger.LogDebug("ReceivePhaseChange: Phase {Number} — {Name}",
                    phase.PhaseNumber, phase.Name);
                await InvokeAllAsync(PhaseChanged, phase);
            });

        _hubConnection.On<string>(nameof(ISentinelHubClient.ReceiveAnnouncement),
            async msg =>
            {
                logger.LogDebug("ReceiveAnnouncement: {Message}", msg);
                await InvokeAllAsync(AnnouncementReceived, msg);
            });

        _hubConnection.On<int>(nameof(ISentinelHubClient.ReceiveGameStateChange),
            async phase =>
            {
                var gamePhase = (GamePhase)phase;
                logger.LogDebug("ReceiveGameStateChange: {Phase} (raw={Raw})", gamePhase, phase);
                await InvokeAllAsync(GameStateChanged, gamePhase);
            });

        _hubConnection.Reconnecting += error =>
        {
            logger.LogWarning(error, "Reconnecting to Game Master...");
            return NotifyConnectionStateAsync(HubConnectionState.Reconnecting);
        };

        _hubConnection.Reconnected += async _ =>
        {
            logger.LogInformation("Reconnected to Game Master — re-registering team");
            await RegisterTeamAsync(CancellationToken.None);
            await NotifyConnectionStateAsync(HubConnectionState.Connected);
        };

        _hubConnection.Closed += error =>
        {
            logger.LogWarning(error, "Connection to Game Master closed");
            return NotifyConnectionStateAsync(HubConnectionState.Disconnected);
        };

        await _hubConnection.StartAsync(ct);
        logger.LogInformation("SignalR connection started to {HubUrl}", _hubUrl);
        await RegisterTeamAsync(ct);
        await NotifyConnectionStateAsync(HubConnectionState.Connected);
    }

    /// <summary>
    /// Invokes every subscriber in a multicast <c>Func&lt;T, Task&gt;</c> delegate,
    /// awaiting each one individually. The default C# multicast invocation only awaits
    /// the last delegate's Task, silently dropping earlier subscribers' async continuations.
    /// </summary>
    private async Task InvokeAllAsync<T>(Func<T, Task>? handler, T arg)
    {
        if (handler is null) return;

        foreach (var subscriber in handler.GetInvocationList().Cast<Func<T, Task>>())
        {
            try
            {
                await subscriber(arg);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Event subscriber {Method} failed for {EventType}",
                    subscriber.Method.Name, typeof(T).Name);
            }
        }
    }

    private async Task RegisterTeamAsync(CancellationToken ct)
    {
        await _hubConnection!.InvokeAsync(
            nameof(ISentinelHubServer.RegisterTeam),
            new TeamRegistration(_teamName),
            ct);
        logger.LogInformation("Registered as team {TeamName}", _teamName);
    }

    private Task NotifyConnectionStateAsync(HubConnectionState state)
    {
        logger.LogDebug("Connection state: {State}", state);
        return InvokeAllAsync(ConnectionStateChanged, state);
    }

    public async Task SubmitDecisionAsync(TeamDecision decision, CancellationToken ct)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to Game Master.");
        await _hubConnection.InvokeAsync(nameof(ISentinelHubServer.SubmitDecision), decision, ct);
    }

    public async Task SubmitPatternsAsync(TeamPatternReport report, CancellationToken ct)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to Game Master.");
        await _hubConnection.InvokeAsync(nameof(ISentinelHubServer.SubmitPatterns), report, ct);
    }

    public async Task SendHeartbeatAsync(CancellationToken ct)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.InvokeAsync(nameof(ISentinelHubServer.Heartbeat), _teamName, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}
