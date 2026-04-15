namespace ShieldWall.Shared.Enums;

/// <summary>
/// Threat severity level assigned during alert classification.
/// Numeric order matters — adjacent levels are one-level-off for partial scoring purposes.
/// </summary>
public enum ThreatLevel
{
    /// <summary>Immediate, highest-priority threat requiring instant escalation.</summary>
    Critical = 0,

    /// <summary>Significant threat requiring urgent attention.</summary>
    High = 1,

    /// <summary>Notable threat that should be monitored closely.</summary>
    Medium = 2,

    /// <summary>Minor threat with limited immediate impact.</summary>
    Low = 3,

    /// <summary>Benign signal; should be dismissed.</summary>
    Noise = 4
}
