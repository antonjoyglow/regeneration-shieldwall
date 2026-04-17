using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Models;

public sealed record ScenarioAlert
{
    public required string AlertId { get; init; }
    public required int BroadcastOffsetSeconds { get; init; }
    public required string Sector { get; init; }

    /// <summary>Alert type as a string — parsed to <see cref="ShieldWall.Shared.Enums.AlertType"/> at broadcast time.</summary>
    public required string Type { get; init; }

    public required int RawSeverity { get; init; }
    public required double ConfidenceScore { get; init; }
    public required string Source { get; init; }
    public string? CorrelationGroup { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public required AlertGroundTruth GroundTruth { get; init; }
}
