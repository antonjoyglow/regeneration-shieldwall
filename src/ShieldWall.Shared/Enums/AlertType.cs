namespace ShieldWall.Shared.Enums;

/// <summary>
/// Category of an incoming alert from the sentinel grid.
/// </summary>
public enum AlertType
{
    /// <summary>Alert originating from physical perimeter sensors.</summary>
    Perimeter,

    /// <summary>Alert originating from cyber intrusion detection systems.</summary>
    Cyber,

    /// <summary>Alert from generic environmental or hardware sensors.</summary>
    Sensor,

    /// <summary>Alert related to communications anomalies or jamming.</summary>
    Comms,

    /// <summary>Alert triggered by environmental conditions (weather, seismic, etc.).</summary>
    Environmental
}
