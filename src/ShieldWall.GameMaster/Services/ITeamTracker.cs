using ShieldWall.GameMaster.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Tracks connected teams and their real-time state for the workshop session.
/// </summary>
public interface ITeamTracker
{
    /// <summary>Registers a new team or updates an existing team on reconnect.</summary>
    /// <param name="teamName">The team's display name.</param>
    /// <param name="connectionId">The SignalR connection ID for this session.</param>
    void RegisterTeam(string teamName, string connectionId);

    /// <summary>Marks a team as disconnected based on its connection ID.</summary>
    /// <param name="connectionId">The SignalR connection ID that disconnected.</param>
    void MarkDisconnected(string connectionId);

    /// <summary>Updates the last-seen heartbeat timestamp for the given team.</summary>
    /// <param name="teamId">The team identifier (name).</param>
    void UpdateHeartbeat(string teamId);

    /// <summary>Returns the team name associated with a SignalR connection ID, or null if not found.</summary>
    /// <param name="connectionId">The SignalR connection ID to look up.</param>
    string? GetTeamNameByConnectionId(string connectionId);

    /// <summary>Returns the <see cref="ConnectedTeam"/> for the given team name, or null if not registered.</summary>
    /// <param name="teamName">The team's display name.</param>
    ConnectedTeam? GetTeam(string teamName);

    /// <summary>Returns a snapshot of all registered teams.</summary>
    IReadOnlyList<ConnectedTeam> GetAllTeams();

    /// <summary>Returns true if a team with the given name is registered.</summary>
    /// <param name="teamName">The team's display name.</param>
    bool IsTeamRegistered(string teamName);

    /// <summary>
    /// Resets all per-session scoring fields on every registered team while keeping
    /// connection state intact. Called by <see cref="IGameOrchestrator.ResetAsync"/> so that
    /// teams that remain connected pick up a clean score on the next run.
    /// </summary>
    void ResetScores();
}
