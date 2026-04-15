namespace ShieldWall.Shared.Models;

/// <summary>
/// Registration payload sent by a team client when connecting to the Game Master hub.
/// </summary>
/// <param name="TeamName">The team's self-reported name (e.g., "Alpha", "Bravo").</param>
public record TeamRegistration(string TeamName);
