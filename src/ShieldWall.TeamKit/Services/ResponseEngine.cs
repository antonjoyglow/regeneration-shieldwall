using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Interfaces;
using ShieldWall.Shared.Models;

namespace ShieldWall.TeamKit.Services;

public sealed class ResponseEngine : IResponseEngine
{
    /// <summary>
    /// WORKSHOP EXERCISE: Improve the response engine!
    /// Current implementation always escalates anything Medium or above and never dismisses
    /// noise — causing alert fatigue and over-escalation penalties.
    /// Consider: pattern context (compound threats should escalate even if individually Medium),
    /// correct dismissal of confirmed noise to avoid flooding the response team,
    /// and avoiding over-escalation of low-confidence Medium alerts.
    /// </summary>
    public ResponseAction Decide(ClassifiedAlert classified, List<ThreatPattern> patterns)
    {
        // Naive: "when in doubt, escalate" — escalates everything Medium and above.
        // Never dismisses noise (afraid to miss a real threat).
        // Consequence: response team is flooded with low-priority escalations
        // and alert fatigue sets in, causing real threats to be missed.
        var (action, priority) = classified.ThreatLevel switch
        {
            ThreatLevel.Critical => (ActionType.Escalate, 1),
            ThreatLevel.High     => (ActionType.Escalate, 2),
            ThreatLevel.Medium   => (ActionType.Escalate, 3),  // over-escalation!
            ThreatLevel.Low      => (ActionType.Monitor,  4),
            ThreatLevel.Noise    => (ActionType.Monitor,  5),  // should Dismiss!
            _                    => (ActionType.Monitor,  3)
        };

        return new ResponseAction(
            Action: action,
            Justification: $"Threat level {classified.ThreatLevel} -> {action} (naive escalation policy)",
            Priority: priority);
    }
}
