using ShieldWall.Shared.Interfaces;
using ShieldWall.Shared.Models;

namespace ShieldWall.TeamKit.Services;

public sealed class PatternDetector : IPatternDetector
{
    /// <summary>
    /// WORKSHOP EXERCISE: Implement pattern detection!
    /// Look for: shared CorrelationGroup values, 3+ alerts in same Sector within 2 minutes,
    /// escalating severity trends, repeated alert types from same source.
    /// </summary>
    public List<ThreatPattern> Detect(List<ClassifiedAlert> recent)
    {
        // Starter: no pattern detection. Teams must implement this to score compound threat bonuses.
        return [];
    }
}
