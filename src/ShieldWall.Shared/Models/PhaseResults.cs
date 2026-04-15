namespace ShieldWall.Shared.Models;

/// <summary>
/// Performance breakdown for a single exercise phase, included in <see cref="TeamResults"/>.
/// </summary>
/// <param name="Phase">Name of the phase (e.g., "Calm Waters").</param>
/// <param name="Effectiveness">Team effectiveness percentage for this phase (0–100).</param>
public record PhaseResults(string Phase, double Effectiveness);
