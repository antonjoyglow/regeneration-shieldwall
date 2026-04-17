using Xunit;
using ShieldWall.Shared.Enums;
using ShieldWall.Shared.Models;

namespace ShieldWall.Tests.Models;

public sealed class ModelTests
{
    [Fact]
    public void SentinelAlert_WithRequiredProperties_CreatesSuccessfully()
    {
        var alert = new SentinelAlert
        {
            AlertId = "SA-0001",
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Sector = "Alpha-1",
            Type = AlertType.Perimeter,
            RawSeverity = 7,
            ConfidenceScore = 0.85,
            Source = "Radar-North"
        };

        Assert.Equal("SA-0001", alert.AlertId);
        Assert.Equal(AlertType.Perimeter, alert.Type);
        Assert.Equal(7, alert.RawSeverity);
        Assert.Null(alert.CorrelationGroup);
        Assert.Empty(alert.Metadata);
    }

    [Fact]
    public void ClassifiedAlert_RecordEquality_WorksCorrectly()
    {
        var originalAlert = new SentinelAlert
        {
            AlertId = "SA-0001",
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Sector = "Alpha-1",
            Type = AlertType.Perimeter,
            RawSeverity = 7,
            ConfidenceScore = 0.85,
            Source = "Radar-North"
        };
        var a = new ClassifiedAlert(ThreatLevel.High, 5.95, "Test reason", originalAlert);
        var b = new ClassifiedAlert(ThreatLevel.High, 5.95, "Test reason", originalAlert);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ThreatLevel_EnumValues_MatchExpectedNumericOrder()
    {
        Assert.Equal(0, (int)ThreatLevel.Critical);
        Assert.Equal(1, (int)ThreatLevel.High);
        Assert.Equal(2, (int)ThreatLevel.Medium);
        Assert.Equal(3, (int)ThreatLevel.Low);
        Assert.Equal(4, (int)ThreatLevel.Noise);
    }
}
