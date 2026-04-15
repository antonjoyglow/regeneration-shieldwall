using ShieldWall.Shared.Enums;

namespace ShieldWall.Shared.Models;

/// <summary>
/// A team's classification output for a single <see cref="SentinelAlert"/>.
/// Produced by <see cref="ShieldWall.Shared.Interfaces.IAlertClassifier"/>.
/// </summary>
/// <param name="ThreatLevel">The threat level assigned by the classifier.</param>
/// <param name="ComputedScore">Numeric score that drove the classification decision.</param>
/// <param name="Reasoning">Human-readable explanation of the classification logic.</param>
/// <param name="OriginalAlert">The source alert that was classified.</param>
public record ClassifiedAlert(
    ThreatLevel ThreatLevel,
    double ComputedScore,
    string Reasoning,
    SentinelAlert OriginalAlert);
