namespace ShieldWall.Shared.Models;

/// <summary>
/// Describes a phase transition in the exercise timeline.
/// Sent by the Game Master when the scenario advances to a new phase.
/// </summary>
/// <param name="Name">Descriptive phase name (e.g., "Calm Waters", "Fog of War").</param>
/// <param name="PhaseNumber">Sequential phase number, starting at 1.</param>
/// <param name="StartMinute">Elapsed exercise minute at which this phase begins.</param>
/// <param name="EndMinute">Elapsed exercise minute at which this phase ends.</param>
public record PhaseInfo(
    string Name,
    int PhaseNumber,
    int StartMinute,
    int EndMinute);
