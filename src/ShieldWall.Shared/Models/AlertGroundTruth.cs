using ShieldWall.Shared.Enums;

namespace ShieldWall.Shared.Models;

/// <summary>
/// Per-alert ground truth embedded in the scenario definition.
/// Used by the scoring engine to evaluate a team's classification and response.
/// </summary>
/// <param name="CorrectClassification">The correct threat level for this alert.</param>
/// <param name="CorrectAction">The correct response action for this alert.</param>
/// <param name="IsCompoundMember">Whether this alert is part of a compound threat group.</param>
/// <param name="CompoundGroupId">
/// The compound group ID this alert belongs to, or <see langword="null"/> if not a compound member.
/// </param>
public record AlertGroundTruth(
    ThreatLevel CorrectClassification,
    ActionType CorrectAction,
    bool IsCompoundMember,
    string? CompoundGroupId);
