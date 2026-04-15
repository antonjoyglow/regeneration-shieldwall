using ShieldWall.Shared.Models;

namespace ShieldWall.Shared.Interfaces;

/// <summary>
/// Detects compound threat patterns across a window of recently classified alerts.
/// Workshop participants implement this interface as part of the pattern-detection stage.
/// </summary>
public interface IPatternDetector
{
    /// <summary>
    /// Analyses a collection of recently classified alerts and returns any detected patterns.
    /// </summary>
    /// <param name="recent">
    /// The list of classified alerts to correlate. Typically a sliding window of recent decisions.
    /// </param>
    /// <returns>
    /// A list of <see cref="ThreatPattern"/> instances. Returns an empty list when no patterns are found.
    /// </returns>
    List<ThreatPattern> Detect(List<ClassifiedAlert> recent);
}
