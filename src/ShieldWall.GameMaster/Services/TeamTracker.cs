using System.Collections.Concurrent;
using ShieldWall.GameMaster.Models;

namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Thread-safe singleton that tracks all registered teams and their runtime state.
/// Uses ConcurrentDictionary for lock-free reads under concurrent hub invocations.
/// </summary>
public sealed class TeamTracker(TimeProvider timeProvider) : ITeamTracker
{
    private readonly ConcurrentDictionary<string, ConnectedTeam> _teamsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _nameByConnectionId = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void RegisterTeam(string teamName, string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        _teamsByName.AddOrUpdate(
            key: teamName.ToLowerInvariant(),
            addValueFactory: _ => new ConnectedTeam
            {
                TeamName = teamName,
                ConnectionId = connectionId,
                IsConnected = true,
                RegisteredAt = timeProvider.GetUtcNow().UtcDateTime,
                LastHeartbeat = timeProvider.GetUtcNow().UtcDateTime
            },
            updateValueFactory: (_, existing) =>
            {
                lock (existing)
                {
                    // Reconnect: remove stale connection ID mapping before adding new one
                    _nameByConnectionId.TryRemove(existing.ConnectionId, out string? _);
                    existing.ConnectionId = connectionId;
                    existing.IsConnected = true;
                    existing.LastHeartbeat = timeProvider.GetUtcNow().UtcDateTime;
                }
                return existing;
            });

        _nameByConnectionId[connectionId] = teamName.ToLowerInvariant();
    }

    /// <inheritdoc />
    public void MarkDisconnected(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        if (!_nameByConnectionId.TryRemove(connectionId, out var key))
            return;

        if (_teamsByName.TryGetValue(key, out var team))
            team.IsConnected = false;
    }

    /// <inheritdoc />
    public void UpdateHeartbeat(string teamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);

        if (_teamsByName.TryGetValue(teamId.ToLowerInvariant(), out var team))
            team.LastHeartbeat = timeProvider.GetUtcNow().UtcDateTime;
    }

    /// <inheritdoc />
    public string? GetTeamNameByConnectionId(string connectionId)
    {
        _nameByConnectionId.TryGetValue(connectionId, out var name);
        return name;
    }

    /// <inheritdoc />
    public ConnectedTeam? GetTeam(string teamName)
    {
        _teamsByName.TryGetValue(teamName.ToLowerInvariant(), out var team);
        return team;
    }

    /// <inheritdoc />
    public IReadOnlyList<ConnectedTeam> GetAllTeams() =>
        [.. _teamsByName.Values];

    /// <inheritdoc />
    public bool IsTeamRegistered(string teamName) =>
        _teamsByName.ContainsKey(teamName.ToLowerInvariant());

    /// <inheritdoc />
    public void ResetScores()
    {
        foreach (var team in _teamsByName.Values)
        {
            lock (team)
            {
                team.AlertsProcessed = 0;
                team.TotalScore = 0;
                team.MaxPossibleScore = 0;
                team.MissionEffectiveness = 0;
                team.AverageLatencyMs = 0;
            }
        }
    }
}