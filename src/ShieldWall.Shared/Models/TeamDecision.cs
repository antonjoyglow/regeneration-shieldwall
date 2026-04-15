namespace ShieldWall.Shared.Models;

/// <summary>
/// The complete decision package that a team submits to the Game Master for a single alert.
/// Wraps the classification, any detected patterns, and the response action.
/// </summary>
public record TeamDecision
{
    /// <summary>The alert ID this decision applies to (must match a live <see cref="SentinelAlert.AlertId"/>).</summary>
    public required string AlertId { get; init; }

    /// <summary>The team's classification of the alert.</summary>
    public required ClassifiedAlert Classification { get; init; }

    /// <summary>Threat patterns the team had detected at the time of this decision.</summary>
    public IReadOnlyList<ThreatPattern> Patterns { get; init; } = [];

    /// <summary>The team's chosen response action for the classified alert.</summary>
    public required ResponseAction Response { get; init; }

    /// <summary>UTC timestamp when the team completed processing this alert.</summary>
    public required DateTime ProcessedAt { get; init; }
}
