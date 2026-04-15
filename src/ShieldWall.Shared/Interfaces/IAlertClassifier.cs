using ShieldWall.Shared.Models;

namespace ShieldWall.Shared.Interfaces;

/// <summary>
/// Classifies a raw <see cref="SentinelAlert"/> into a <see cref="ClassifiedAlert"/>.
/// Workshop participants implement this interface as their primary exercise deliverable.
/// </summary>
public interface IAlertClassifier
{
    /// <summary>
    /// Evaluates the incoming alert and returns a classification with a threat level and reasoning.
    /// </summary>
    /// <param name="alert">The raw alert from the sentinel grid.</param>
    /// <returns>A classified alert containing the assigned threat level and score.</returns>
    ClassifiedAlert Classify(SentinelAlert alert);
}
