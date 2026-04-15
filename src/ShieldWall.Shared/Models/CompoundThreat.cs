using ShieldWall.Shared.Enums;

namespace ShieldWall.Shared.Models;

/// <summary>
/// Ground truth definition of a compound threat used in the exercise scenario.
/// Defines the set of correlated alerts that together constitute a multi-vector threat.
/// </summary>
public record CompoundThreat
{
    /// <summary>Unique group identifier for this compound threat (e.g., "CG-001").</summary>
    public required string GroupId { get; init; }

    /// <summary>Human-readable description of what this compound threat represents.</summary>
    public required string Description { get; init; }

    /// <summary>Alert IDs of the individual alerts that form this compound threat.</summary>
    public IReadOnlyList<string> MemberAlertIds { get; init; } = [];

    /// <summary>The correct escalated threat level for this compound group.</summary>
    public required ThreatLevel CorrectEscalatedLevel { get; init; }

    /// <summary>The correct response action that should be taken for this compound threat.</summary>
    public required ActionType CorrectAction { get; init; }

    /// <summary>Time window in seconds within which the member alerts must be correlated.</summary>
    public required int WindowSeconds { get; init; }
}
