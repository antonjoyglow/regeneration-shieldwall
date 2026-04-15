using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Models;

namespace ShieldWall.Shared.Hubs;

/// <summary>
/// Defines methods the Game Master hub can invoke on connected team clients.
/// Implement this interface on the client-side SignalR proxy.
/// </summary>
public interface ISentinelHubClient
{
    /// <summary>Called when the Game Master broadcasts a new alert to all teams.</summary>
    /// <param name="alert">The sentinel alert that teams must classify and respond to.</param>
    Task ReceiveAlert(SentinelAlert alert);

    /// <summary>Called when the Game Master sends an updated score snapshot for a team.</summary>
    /// <param name="update">The score update containing current effectiveness and totals.</param>
    Task ReceiveScoreUpdate(ScoreUpdate update);

    /// <summary>Called when the exercise advances to a new phase.</summary>
    /// <param name="phaseInfo">Details of the new phase, including its name and time window.</param>
    Task ReceivePhaseChange(PhaseInfo phaseInfo);

    /// <summary>Called when the Game Master broadcasts a general announcement to all participants.</summary>
    /// <param name="message">The announcement text (e.g., scenario narration, phase summary).</param>
    Task ReceiveAnnouncement(string message);

    /// <summary>Called when the exercise lifecycle state changes (Lobby → Briefing → Live → etc.).</summary>
    /// <param name="gamePhase">The new game phase.</param>
    Task ReceiveGameStateChange(GamePhase gamePhase);
}

/// <summary>
/// Defines methods that team clients can invoke on the Game Master hub.
/// Use these as the method names when calling <c>HubConnection.InvokeAsync</c>.
/// </summary>
public interface ISentinelHubServer
{
    /// <summary>Registers this connection as a named team. Must be called after connecting.</summary>
    /// <param name="registration">The team registration payload containing the team name.</param>
    Task RegisterTeam(TeamRegistration registration);

    /// <summary>Submits a complete decision (classification + patterns + response) for an alert.</summary>
    /// <param name="decision">The team's decision package for the specified alert.</param>
    Task SubmitDecision(TeamDecision decision);

    /// <summary>Submits the team's current detected threat patterns for scoring.</summary>
    /// <param name="report">The pattern report containing all currently detected patterns.</param>
    Task SubmitPatterns(TeamPatternReport report);

    /// <summary>Submits a predictive sector guess for the hero mission phase.</summary>
    /// <param name="prediction">The team's sector prediction with confidence level.</param>
    Task SubmitPrediction(TeamPrediction prediction);

    /// <summary>
    /// Sends a heartbeat to keep the connection alive and signal that the team is still active.
    /// </summary>
    /// <param name="teamId">The registered team ID.</param>
    Task Heartbeat(string teamId);

    /// <summary>
    /// Joins the broadcast group as a spectator (War Room / GM panel).
    /// Does not register as a team.
    /// </summary>
    Task JoinWarRoom();
}
