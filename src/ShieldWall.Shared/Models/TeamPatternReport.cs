namespace ShieldWall.Shared.Models;

/// <summary>
/// A team's submission of detected threat patterns at a point in time.
/// Used to evaluate compound threat detection accuracy.
/// </summary>
/// <param name="TeamId">The team submitting the report.</param>
/// <param name="Patterns">The list of threat patterns detected by the team.</param>
/// <param name="Timestamp">UTC timestamp when the report was generated.</param>
public record TeamPatternReport(
    string TeamId,
    IReadOnlyList<ThreatPattern> Patterns,
    DateTime Timestamp);
