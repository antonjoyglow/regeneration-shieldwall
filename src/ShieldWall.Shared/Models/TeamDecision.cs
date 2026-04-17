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

    /// <summary>UTC timestamp when the team completed processing this alert (diagnostic only — not used for scoring).</summary>
    public required DateTime ProcessedAt { get; init; }

    /// <summary>
    /// Client-measured processing duration in milliseconds (monotonic clock).
    /// Covers only the classify → detect → decide pipeline, excluding network transit.
    /// The server uses this for the latency multiplier instead of cross-clock subtraction.
    /// </summary>
    public double ProcessingDurationMs { get; init; }
}
