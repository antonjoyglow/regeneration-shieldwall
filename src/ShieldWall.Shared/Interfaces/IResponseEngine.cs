using ShieldWall.Shared.Models;

namespace ShieldWall.Shared.Interfaces;

/// <summary>
/// Determines the appropriate response action for a classified alert given the current threat context.
/// Workshop participants implement this interface as the final decision-making stage.
/// </summary>
public interface IResponseEngine
{
    /// <summary>
    /// Selects a response action based on the alert classification and any active threat patterns.
    /// </summary>
    /// <param name="classified">The classification result for the alert being responded to.</param>
    /// <param name="patterns">
    /// Active threat patterns at the time of the decision. May be empty if no patterns are detected.
    /// </param>
    /// <returns>A <see cref="ResponseAction"/> specifying the chosen action and its justification.</returns>
    ResponseAction Decide(ClassifiedAlert classified, List<ThreatPattern> patterns);
}
