using Microsoft.AspNetCore.SignalR;
using ShieldWall.GameMaster.Services;
using ShieldWall.Shared.Hubs;
using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Hubs;

/// <summary>
/// Strongly-typed SignalR hub that acts as the real-time communication backbone
/// between the Game Master and all participating teams.
/// </summary>
public sealed class SentinelHub(
    ITeamTracker teamTracker,
    IScoringEngine scoringEngine,
    ILogger<SentinelHub> logger)
    : Hub<ISentinelHubClient>, ISentinelHubServer
{
    private const string BroadcastGroup = "broadcast";

    /// <inheritdoc />
    public async Task RegisterTeam(TeamRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var teamName = registration.TeamName;
        var connectionId = Context.ConnectionId;

        await Groups.AddToGroupAsync(connectionId, BroadcastGroup, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(connectionId, $"team.{teamName.ToLowerInvariant()}", Context.ConnectionAborted);

        teamTracker.RegisterTeam(teamName, connectionId);

        logger.LogInformation("Team {TeamName} registered with connection {ConnectionId}", teamName, connectionId);
    }

    /// <inheritdoc />
    public Task SubmitDecision(TeamDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        logger.LogInformation(
            "Decision received from connection {ConnectionId} for alert {AlertId}",
            Context.ConnectionId,
            decision.AlertId);

        return scoringEngine.EvaluateDecisionAsync(decision, Context.ConnectionId, Context.ConnectionAborted);
    }

    /// <inheritdoc />
    public Task SubmitPatterns(TeamPatternReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        logger.LogInformation(
            "Pattern report received from connection {ConnectionId} with {PatternCount} patterns",
            Context.ConnectionId,
            report.Patterns?.Count ?? 0);

        return scoringEngine.EvaluatePatternsAsync(report, Context.ConnectionAborted);
    }

    /// <inheritdoc />
    public Task SubmitPrediction(TeamPrediction prediction)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        logger.LogInformation(
            "Prediction received from connection {ConnectionId} for sector {Sector}",
            Context.ConnectionId,
            prediction.PredictedSector);

        return scoringEngine.EvaluatePredictionAsync(prediction, Context.ConnectionAborted);
    }

    /// <inheritdoc />
    public Task Heartbeat(string teamId)
    {
        teamTracker.UpdateHeartbeat(teamId);
        logger.LogDebug("Heartbeat received from team {TeamId}", teamId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task JoinWarRoom()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BroadcastGroup, Context.ConnectionAborted);
        logger.LogInformation("Spectator joined broadcast group: {ConnectionId}", Context.ConnectionId);
    }

    /// <summary>
    /// Handles cleanup when a team disconnects. Marks the team as offline in the tracker.
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var teamName = teamTracker.GetTeamNameByConnectionId(connectionId);

        teamTracker.MarkDisconnected(connectionId);

        if (teamName is not null)
            logger.LogWarning("Team {TeamName} disconnected", teamName);
        else
            logger.LogDebug("Unknown connection {ConnectionId} disconnected", connectionId);

        return base.OnDisconnectedAsync(exception);
    }
}