using Microsoft.AspNetCore.SignalR.Client;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Models;
using ShieldWall.TeamKit.Services;

namespace ShieldWall.TeamKit.State;

public sealed class GameState(
    SentinelConnection connection,
    ILogger<GameState> logger) : IDisposable
{
    private readonly List<SentinelAlert> _recentAlerts = [];
    private readonly List<string> _announcements = [];
    private readonly Lock _lock = new();
    private bool _subscribed;

    public HubConnectionState ConnectionState => connection.State;
    public string TeamName => connection.TeamName;
    public ScoreUpdate? LatestScore { get; private set; }
    public PhaseInfo? CurrentPhase { get; private set; }
    public string? LatestAnnouncement { get; private set; }
    public GamePhase CurrentGamePhase { get; private set; } = GamePhase.Lobby;

    public IReadOnlyList<SentinelAlert> RecentAlerts
    {
        get { lock (_lock) { return [.. _recentAlerts]; } }
    }

    public IReadOnlyList<string> Announcements
    {
        get { lock (_lock) { return [.. _announcements]; } }
    }

    public event Action? StateChanged;

    /// <summary>Subscribe to connection events. Safe to call multiple times.</summary>
    public void EnsureSubscribed()
    {
        if (_subscribed) return;
        _subscribed = true;

        connection.AlertReceived += OnAlertReceivedAsync;
        connection.ScoreUpdated += OnScoreUpdatedAsync;
        connection.PhaseChanged += OnPhaseChangedAsync;
        connection.AnnouncementReceived += OnAnnouncementReceivedAsync;
        connection.ConnectionStateChanged += OnConnectionStateChangedAsync;
        connection.GameStateChanged += OnGameStateChangedAsync;

        logger.LogDebug("GameState subscribed to SentinelConnection events");
    }

    private Task OnAlertReceivedAsync(SentinelAlert alert)
    {
        int count;
        lock (_lock)
        {
            if (_recentAlerts.Count >= 50)
                _recentAlerts.RemoveAt(0);
            _recentAlerts.Add(alert);
            count = _recentAlerts.Count;
        }
        logger.LogDebug("GameState received alert {AlertId} (total: {Count})", alert.AlertId, count);
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnScoreUpdatedAsync(ScoreUpdate update)
    {
        LatestScore = update;
        logger.LogDebug("GameState score update: {Effectiveness}% ({Processed} alerts)",
            update.MissionEffectiveness, update.AlertsProcessed);
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnPhaseChangedAsync(PhaseInfo phase)
    {
        CurrentPhase = phase;
        logger.LogDebug("GameState phase changed: Phase {Number} — {Name}", phase.PhaseNumber, phase.Name);
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnAnnouncementReceivedAsync(string message)
    {
        lock (_lock)
        {
            _announcements.Add(message);
        }
        LatestAnnouncement = message;
        logger.LogDebug("GameState announcement: {Message}", message);
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnConnectionStateChangedAsync(HubConnectionState state)
    {
        logger.LogDebug("GameState connection state: {State}", state);
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnGameStateChangedAsync(GamePhase phase)
    {
        var previous = CurrentGamePhase;
        CurrentGamePhase = phase;

        if (phase is GamePhase.Lobby or GamePhase.Live)
        {
            lock (_lock)
            {
                _recentAlerts.Clear();
                _announcements.Clear();
            }

            LatestScore = null;
            CurrentPhase = null;
            LatestAnnouncement = null;
            logger.LogDebug("GameState cleared stale data for {Phase} transition", phase);
        }

        logger.LogDebug("GameState game phase: {Previous} → {Current}", previous, phase);
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        connection.AlertReceived -= OnAlertReceivedAsync;
        connection.ScoreUpdated -= OnScoreUpdatedAsync;
        connection.PhaseChanged -= OnPhaseChangedAsync;
        connection.AnnouncementReceived -= OnAnnouncementReceivedAsync;
        connection.ConnectionStateChanged -= OnConnectionStateChangedAsync;
        connection.GameStateChanged -= OnGameStateChangedAsync;
    }
}
