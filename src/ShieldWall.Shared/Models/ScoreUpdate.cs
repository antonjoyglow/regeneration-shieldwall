namespace ShieldWall.Shared.Models;

/// <summary>
/// A score broadcast sent from the Game Master to all connected clients.
/// Reflects the current performance of a specific team.
/// </summary>
/// <param name="TeamId">The team this update applies to.</param>
/// <param name="MissionEffectiveness">Current mission effectiveness as a percentage (0–100).</param>
/// <param name="TotalScore">Running total score accumulated so far.</param>
/// <param name="MaxPossibleScore">Maximum score achievable across all evaluated alerts.</param>
/// <param name="AlertsProcessed">Number of alerts the team has submitted decisions for.</param>
/// <param name="AverageLatencyMs">Rolling average response time in milliseconds.</param>
/// <param name="Timestamp">UTC timestamp when this score snapshot was computed.</param>
public record ScoreUpdate(
    string TeamId,
    double MissionEffectiveness,
    double TotalScore,
    double MaxPossibleScore,
    int AlertsProcessed,
    int AverageLatencyMs,
    DateTime Timestamp);
