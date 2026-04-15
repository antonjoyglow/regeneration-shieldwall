namespace ShieldWall.Shared.Enums;

/// <summary>
/// The response action a team selects after classifying an alert.
/// </summary>
public enum ActionType
{
    /// <summary>Treat the alert as benign and close it without further action.</summary>
    Dismiss,

    /// <summary>Keep the alert under observation without immediate escalation.</summary>
    Monitor,

    /// <summary>Immediately escalate the alert to the highest response level.</summary>
    Escalate
}
