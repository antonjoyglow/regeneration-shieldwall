namespace ShieldWall.GameMaster.Models;

/// <summary>
/// Represents the mutable runtime state for a team connected to the sentinel hub.
/// </summary>
public sealed class ConnectedTeam
{
    /// <summary>The team's display name (lower-cased canonical key in TeamTracker).</summary>
    public required string TeamName { get; init; }

    /// <summary>The current SignalR connection ID for this team. Changes on reconnect.</summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>Whether the team is currently connected to the hub.</summary>
    public bool IsConnected { get; set; }

    /// <summary>UTC timestamp when the team first registered.</summary>
    public DateTime RegisteredAt { get; init; }

    /// <summary>UTC timestamp of the last received heartbeat.</summary>
    public DateTime LastHeartbeat { get; set; }

    /// <summary>Total number of alerts the team has processed.</summary>
    public int AlertsProcessed { get; set; }

    /// <summary>Cumulative score earned by the team.</summary>
    public double TotalScore { get; set; }

    /// <summary>Maximum possible score based on alerts seen.</summary>
    public double MaxPossibleScore { get; set; }

    /// <summary>Current mission effectiveness (0–100).</summary>
    public double MissionEffectiveness { get; set; }

    /// <summary>Rolling average decision latency in milliseconds.</summary>
    public int AverageLatencyMs { get; set; }
}
