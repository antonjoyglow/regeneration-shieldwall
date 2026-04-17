using System.Text.Json;
using System.Text.Json.Serialization;
using ShieldWall.GameMaster.Models;

namespace ShieldWall.GameMaster.Services;

public sealed class ScenarioLoader(ILogger<ScenarioLoader> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ScenarioFile Load(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"Scenario file not found: '{path}'.");

        var json = File.ReadAllText(path);
        var scenario = JsonSerializer.Deserialize<ScenarioFile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Scenario file deserialized to null.");

        Validate(scenario);

        logger.LogInformation(
            "Loaded {AlertCount} alerts, {CompoundCount} compound threats from '{Path}'",
            scenario.Alerts.Count,
            scenario.CompoundThreats.Count,
            path);

        return scenario;
    }

    private static void Validate(ScenarioFile scenario)
    {
        for (var i = 1; i < scenario.Alerts.Count; i++)
        {
            var prev = scenario.Alerts[i - 1];
            var curr = scenario.Alerts[i];
            if (curr.BroadcastOffsetSeconds < prev.BroadcastOffsetSeconds)
                throw new InvalidOperationException(
                    $"Alerts are not sorted by BroadcastOffsetSeconds: '{curr.AlertId}' at index {i} " +
                    $"has offset {curr.BroadcastOffsetSeconds}s which is less than previous offset {prev.BroadcastOffsetSeconds}s.");
        }

        var alertIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alert in scenario.Alerts)
        {
            if (!alertIdSet.Add(alert.AlertId))
                throw new InvalidOperationException($"Duplicate AlertId found: '{alert.AlertId}'.");
        }

        var compoundMemberIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var compound in scenario.CompoundThreats)
        {
            foreach (var memberId in compound.MemberAlertIds)
            {
                if (!alertIdSet.Contains(memberId))
                    throw new InvalidOperationException(
                        $"Compound threat '{compound.GroupId}' references unknown alert ID: '{memberId}'.");
                compoundMemberIds.Add(memberId);
            }
        }

        foreach (var alert in scenario.Alerts.Where(a => compoundMemberIds.Contains(a.AlertId)))
        {
            if (!alert.GroundTruth.IsCompoundMember)
                throw new InvalidOperationException(
                    $"Alert '{alert.AlertId}' is referenced by a compound threat but has IsCompoundMember=false in ground truth.");
        }
    }
}
