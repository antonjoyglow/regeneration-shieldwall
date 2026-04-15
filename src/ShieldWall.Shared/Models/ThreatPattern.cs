using ShieldWall.Shared.Enums;

namespace ShieldWall.Shared.Models;

/// <summary>
/// A compound threat pattern detected by correlating multiple classified alerts.
/// Produced by <see cref="ShieldWall.Shared.Interfaces.IPatternDetector"/>.
/// </summary>
/// <param name="PatternId">Unique identifier for this pattern instance.</param>
/// <param name="EscalatedLevel">Threat level after pattern-based escalation.</param>
/// <param name="AlertIds">Ordered set of alert IDs that compose this pattern.</param>
/// <param name="PatternDescription">Human-readable description of the detected pattern.</param>
public record ThreatPattern(
    string PatternId,
    ThreatLevel EscalatedLevel,
    IReadOnlyList<string> AlertIds,
    string PatternDescription);
