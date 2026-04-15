namespace ShieldWall.Shared.Enums;

/// <summary>
/// Represents the high-level lifecycle state of the workshop exercise,
/// controlled exclusively by the Game Master.
/// </summary>
public enum GamePhase
{
    /// <summary>Default state. Teams connect. No alerts flow.</summary>
    Lobby = 0,

    /// <summary>GM has opened the exercise. Teams read code and strategise. No alerts flow.</summary>
    Briefing = 1,

    /// <summary>Alert stream is running. Scoring is active.</summary>
    Live = 2,

    /// <summary>Stream paused by GM. Manual wave dispatch still available.</summary>
    Paused = 3,

    /// <summary>Exercise complete. Final scores frozen.</summary>
    Completed = 4
}
