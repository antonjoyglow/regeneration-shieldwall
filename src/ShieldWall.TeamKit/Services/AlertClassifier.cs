using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Interfaces;
using ShieldWall.Shared.Models;

namespace ShieldWall.TeamKit.Services;

public sealed class AlertClassifier : IAlertClassifier
{
    /// <summary>
    /// WORKSHOP EXERCISE: Improve this classifier!
    /// Current implementation uses only raw severity and ignores the confidence score entirely,
    /// treating every alert source as equally reliable.
    /// Consider: a sev=9 reading from a known-faulty sensor is NOT the same as sev=9 from a
    /// verified source. How should source reliability (ConfidenceScore) affect classification?
    /// Also consider: alert type, sector context, and correlation with recent alerts.
    /// </summary>
    public ClassifiedAlert Classify(SentinelAlert alert)
    {
        // Naive: uses only raw severity — ignores ConfidenceScore completely.
        // High-confidence low-severity alerts and low-confidence high-severity alerts
        // are treated identically. This causes both over- and under-classification.
        var threatLevel = alert.RawSeverity switch
        {
            >= 8 => ThreatLevel.Critical,
            >= 6 => ThreatLevel.High,
            >= 4 => ThreatLevel.Medium,
            >= 2 => ThreatLevel.Low,
            _ => ThreatLevel.Noise
        };

        return new ClassifiedAlert(
            ThreatLevel: threatLevel,
            ComputedScore: (double)alert.RawSeverity,
            Reasoning: $"RawSeverity={alert.RawSeverity} (confidence={alert.ConfidenceScore:F2} ignored)",
            OriginalAlert: alert);
    }
}
