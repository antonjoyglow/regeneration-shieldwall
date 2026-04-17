namespace ShieldWall.GameMaster.Models;

/// <summary>
/// Snapshot of alert broadcast progress for the Game Master dashboard.
/// </summary>
/// <param name="Sent">Total alerts broadcast so far.</param>
/// <param name="Total">Total alerts in the scenario.</param>
/// <param name="Phases">Per-phase breakdown of sent vs. total.</param>
public record AlertProgress(int Sent, int Total, IReadOnlyList<PhaseProgress> Phases);

/// <param name="Name">Phase display name.</param>
/// <param name="Sent">Alerts already broadcast in this phase.</param>
/// <param name="Total">Total alerts scheduled for this phase.</param>
public record PhaseProgress(string Name, int Sent, int Total);
