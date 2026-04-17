using ShieldWall.Shared.Models;

namespace ShieldWall.GameMaster.Models;

public sealed record ScenarioMetadata
{
    public required int TotalAlerts { get; init; }
    public required int DurationMinutes { get; init; }
    public required IReadOnlyList<PhaseDefinition> Phases { get; init; }
}

public sealed record PhaseDefinition
{
    public required string Name { get; init; }
    public required int StartMinute { get; init; }
    public required int EndMinute { get; init; }
}

public sealed record ScenarioFile
{
    public required ScenarioMetadata Metadata { get; init; }
    public required IReadOnlyList<ScenarioAlert> Alerts { get; init; }
    public required IReadOnlyList<CompoundThreat> CompoundThreats { get; init; }
}
