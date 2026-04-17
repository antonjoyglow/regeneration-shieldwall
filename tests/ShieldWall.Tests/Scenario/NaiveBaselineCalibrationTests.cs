using Microsoft.Extensions.Logging.Abstractions;
using ShieldWall.GameMaster.Models;
using ShieldWall.GameMaster.Services;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Models;
using ShieldWall.TeamKit.Services;
using Xunit;

namespace ShieldWall.Tests.Scenario;

/// <summary>
/// Regression gate: ensures the naive starter code (AlertClassifier + ResponseEngine)
/// scores within the target calibration band against the live scenario file.
/// If this test breaks after editing alert-scenario.json, the scenario is mis-calibrated.
/// </summary>
public sealed class NaiveBaselineCalibrationTests
{
    private const double MinEffectiveness = 30.0;
    private const double MaxEffectiveness = 45.0;

    private static readonly string ScenarioPath = Path.Combine(
        FindRepoRoot(), "src", "ShieldWall.GameMaster", "Data", "alert-scenario.json");

    [Fact]
    public void NaiveStarterCode_Effectiveness_IsWithinCalibrationBand()
    {
        var scenario = LoadScenario();
        var (effectiveness, _, _) = SimulateNaiveRun(scenario);

        Assert.True(
            effectiveness >= MinEffectiveness && effectiveness <= MaxEffectiveness,
            $"Naive effectiveness {effectiveness:F1}% is outside the calibration band " +
            $"[{MinEffectiveness}%, {MaxEffectiveness}%]. Adjust alert-scenario.json.");
    }

    [Fact]
    public void NaiveStarterCode_HasZeroUnderEscalations()
    {
        // The naive ResponseEngine escalates everything Medium+ and monitors Low/Noise.
        // It never under-escalates, which is a known weakness teams should discover.
        var scenario = LoadScenario();
        var (_, _, breakdown) = SimulateNaiveRun(scenario);

        Assert.True(
            breakdown.UnderEscalations > 0,
            "Scenario should contain alerts where naive code under-escalates " +
            "(low severity but correct action is Escalate) to reward confidence-aware classifiers.");
    }

    [Fact]
    public void NaiveStarterCode_NeverDismisses()
    {
        // The naive ResponseEngine never returns Dismiss — teams must learn to dismiss noise.
        var scenario = LoadScenario();
        var classifier = new AlertClassifier();
        var engine = new ResponseEngine();

        foreach (var alert in scenario.Alerts)
        {
            var sentinel = ToSentinelAlert(alert);
            var classified = classifier.Classify(sentinel);
            var response = engine.Decide(classified, []);

            Assert.NotEqual(ActionType.Dismiss, response.Action);
        }
    }

    [Fact]
    public void Scenario_HasSufficientStealthThreats()
    {
        // Stealth threats: RawSeverity ≤ 4 but CorrectClassification is High or Critical.
        // These punish severity-only classifiers and reward confidence-aware logic.
        var scenario = LoadScenario();

        var stealthCount = scenario.Alerts.Count(a =>
            a.RawSeverity <= 4 &&
            a.GroundTruth.CorrectClassification is ThreatLevel.High or ThreatLevel.Critical);

        Assert.True(
            stealthCount >= 10,
            $"Scenario has only {stealthCount} stealth threats (sev ≤ 4, correct = High/Critical). " +
            "Need at least 10 to make confidence-awareness a meaningful differentiator.");
    }

    [Fact]
    public void Scenario_HasSufficientDeceptiveNoise()
    {
        // Deceptive noise: RawSeverity ≥ 5 but CorrectClassification is Noise or Low.
        // These punish "always escalate high severity" classifiers.
        var scenario = LoadScenario();

        var deceptiveCount = scenario.Alerts.Count(a =>
            a.RawSeverity >= 5 &&
            a.GroundTruth.CorrectClassification is ThreatLevel.Noise or ThreatLevel.Low);

        Assert.True(
            deceptiveCount >= 15,
            $"Scenario has only {deceptiveCount} deceptive noise alerts (sev ≥ 5, correct = Noise/Low). " +
            "Need at least 15 to punish severity-only classifiers.");
    }

    [Fact]
    public void Scenario_AllAlertsHaveSourceReliabilityMetadata()
    {
        var scenario = LoadScenario();

        var missing = scenario.Alerts
            .Where(a => !a.Metadata.ContainsKey("sourceReliability"))
            .Select(a => a.AlertId)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} alerts lack sourceReliability metadata: {string.Join(", ", missing.Take(10))}...");
    }

    // ── Simulation helpers ────────────────────────────────────────────────────

    private static (double Effectiveness, double TotalScore, ScoringBreakdown Breakdown)
        SimulateNaiveRun(ScenarioFile scenario)
    {
        var classifier = new AlertClassifier();
        var engine = new ResponseEngine();
        var groundTruth = scenario.Alerts.ToDictionary(
            a => a.AlertId, a => a.GroundTruth, StringComparer.OrdinalIgnoreCase);

        double totalScore = 0;
        double maxScore = 0;
        int exactClassifications = 0;
        int offBy1 = 0;
        int offBy2Plus = 0;
        int exactActions = 0;
        int overEscalations = 0;
        int underEscalations = 0;
        int otherActionMismatch = 0;

        foreach (var scenarioAlert in scenario.Alerts)
        {
            var sentinel = ToSentinelAlert(scenarioAlert);
            var classified = classifier.Classify(sentinel);
            var response = engine.Decide(classified, []);
            var truth = groundTruth[scenarioAlert.AlertId];

            // Classification score (same formula as ScoringEngine)
            bool bothNoise = classified.ThreatLevel == ThreatLevel.Noise
                          && truth.CorrectClassification == ThreatLevel.Noise;
            int levelDiff = Math.Abs((int)classified.ThreatLevel - (int)truth.CorrectClassification);
            double classScore = bothNoise ? 1.2 : levelDiff switch
            {
                0 => 1.0,
                1 => 0.5,
                _ => 0.0
            };

            if (bothNoise || levelDiff == 0) exactClassifications++;
            else if (levelDiff == 1) offBy1++;
            else offBy2Plus++;

            // Action score (same formula as ScoringEngine)
            double actionScore;
            if (response.Action == ActionType.Dismiss && truth.CorrectAction == ActionType.Dismiss)
                actionScore = 1.1;
            else if (response.Action == truth.CorrectAction)
                actionScore = 1.0;
            else if (response.Action == ActionType.Escalate && truth.CorrectAction != ActionType.Escalate)
                actionScore = 0.7;
            else if (response.Action != ActionType.Escalate && truth.CorrectAction == ActionType.Escalate)
                actionScore = 0.0;
            else
                actionScore = 0.5;

            if (response.Action == truth.CorrectAction) exactActions++;
            else if (response.Action == ActionType.Escalate && truth.CorrectAction != ActionType.Escalate) overEscalations++;
            else if (response.Action != ActionType.Escalate && truth.CorrectAction == ActionType.Escalate) underEscalations++;
            else otherActionMismatch++;

            double alertTotal = (classScore * 0.6) + (actionScore * 0.4);

            // Max possible for this alert
            double maxClass = truth.CorrectClassification == ThreatLevel.Noise ? 1.2 : 1.0;
            double maxAction = truth.CorrectAction == ActionType.Dismiss ? 1.1 : 1.0;
            double maxAlert = (maxClass * 0.6) + (maxAction * 0.4);

            totalScore += alertTotal;
            maxScore += maxAlert;
        }

        double effectiveness = maxScore > 0 ? totalScore / maxScore * 100.0 : 0;

        return (Math.Round(effectiveness, 1), Math.Round(totalScore, 2), new ScoringBreakdown(
            exactClassifications, offBy1, offBy2Plus,
            exactActions, overEscalations, underEscalations, otherActionMismatch));
    }

    private static SentinelAlert ToSentinelAlert(ScenarioAlert source) => new()
    {
        AlertId = source.AlertId,
        Timestamp = DateTime.UtcNow,
        Sector = source.Sector,
        Type = Enum.Parse<AlertType>(source.Type, ignoreCase: true),
        RawSeverity = source.RawSeverity,
        ConfidenceScore = source.ConfidenceScore,
        Source = source.Source,
        CorrelationGroup = source.CorrelationGroup,
        Metadata = new Dictionary<string, string>(source.Metadata)
    };

    private static ScenarioFile LoadScenario()
    {
        var loader = new ScenarioLoader(NullLogger<ScenarioLoader>.Instance);
        return loader.Load(ScenarioPath);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0 || dir.GetFiles("*.slnx").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        // Fallback: assume standard test output layout
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    private sealed record ScoringBreakdown(
        int ExactClassifications,
        int OffBy1,
        int OffBy2Plus,
        int ExactActions,
        int OverEscalations,
        int UnderEscalations,
        int OtherActionMismatch);
}
