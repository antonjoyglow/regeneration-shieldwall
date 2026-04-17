using Xunit;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Models;
using ShieldWall.TeamKit.Services;

namespace ShieldWall.Tests.TeamKit;

public sealed class AlertClassifierTests
{
    private readonly AlertClassifier _classifier = new();

    [Theory]
    [InlineData(9, 0.95, ThreatLevel.Critical)]
    [InlineData(7, 0.80, ThreatLevel.High)]
    [InlineData(5, 0.70, ThreatLevel.Medium)]
    [InlineData(3, 0.60, ThreatLevel.Low)]
    [InlineData(2, 0.10, ThreatLevel.Low)]   // naive: ignores confidence, sev=2 → Low
    public void Classify_GivenSeverityAndConfidence_ReturnsExpectedLevel(
        int severity, double confidence, ThreatLevel expected)
    {
        var alert = CreateAlert(severity, confidence);

        var result = _classifier.Classify(alert);

        Assert.Equal(expected, result.ThreatLevel);
    }

    [Fact]
    public void Classify_ReturnsOriginalAlert()
    {
        var alert = CreateAlert();

        var result = _classifier.Classify(alert);

        Assert.Equal(alert, result.OriginalAlert);
    }

    [Fact]
    public void Classify_ReturnsRawSeverityAsComputedScore()
    {
        var alert = CreateAlert(severity: 3, confidence: 0.333);

        var result = _classifier.Classify(alert);

        Assert.Equal(3.0, result.ComputedScore);  // naive: ComputedScore = RawSeverity (confidence ignored)
    }

    private static SentinelAlert CreateAlert(int severity = 5, double confidence = 0.5,
        string? correlationGroup = null) => new()
    {
        AlertId = "SA-TEST",
        Timestamp = DateTime.UtcNow,
        Sector = "Test-1",
        Type = AlertType.Perimeter,
        RawSeverity = severity,
        ConfidenceScore = confidence,
        Source = "Test-Source",
        CorrelationGroup = correlationGroup
    };
}
