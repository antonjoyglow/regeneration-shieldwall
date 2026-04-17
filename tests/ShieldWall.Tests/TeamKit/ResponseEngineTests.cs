using Xunit;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Models;
using ShieldWall.TeamKit.Services;

namespace ShieldWall.Tests.TeamKit;

public sealed class ResponseEngineTests
{
    private readonly ResponseEngine _engine = new();

    [Theory]
    [InlineData(ThreatLevel.Critical, ActionType.Escalate)]
    [InlineData(ThreatLevel.High,     ActionType.Escalate)]
    [InlineData(ThreatLevel.Medium,   ActionType.Escalate)]  // naive: over-escalates Medium
    [InlineData(ThreatLevel.Low,      ActionType.Monitor)]
    [InlineData(ThreatLevel.Noise,    ActionType.Monitor)]   // naive: never dismisses
    public void Decide_GivenThreatLevel_ReturnsExpectedAction(
        ThreatLevel level, ActionType expectedAction)
    {
        var classified = CreateClassifiedAlert(level);

        var result = _engine.Decide(classified, []);

        Assert.Equal(expectedAction, result.Action);
    }

    private static ClassifiedAlert CreateClassifiedAlert(ThreatLevel level) => new(
        ThreatLevel: level,
        ComputedScore: 5.0,
        Reasoning: "Test",
        OriginalAlert: new SentinelAlert
        {
            AlertId = "SA-TEST",
            Timestamp = DateTime.UtcNow,
            Sector = "Test-1",
            Type = AlertType.Perimeter,
            RawSeverity = 5,
            ConfidenceScore = 0.5,
            Source = "Test-Source"
        });
}
