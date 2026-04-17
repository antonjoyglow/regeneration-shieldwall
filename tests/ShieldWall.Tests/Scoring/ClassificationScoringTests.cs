using Xunit;
using ShieldWall.Shared.Enums;

namespace ShieldWall.Tests.Scoring;

public sealed class ClassificationScoringTests
{
    [Theory]
    [InlineData(ThreatLevel.Critical, ThreatLevel.Critical, 1.0)]
    [InlineData(ThreatLevel.High,     ThreatLevel.High,     1.0)]
    [InlineData(ThreatLevel.Noise,    ThreatLevel.Noise,    1.2)]
    [InlineData(ThreatLevel.Critical, ThreatLevel.High,     0.5)]
    [InlineData(ThreatLevel.High,     ThreatLevel.Medium,   0.5)]
    [InlineData(ThreatLevel.Critical, ThreatLevel.Medium,   0.0)]
    [InlineData(ThreatLevel.Critical, ThreatLevel.Noise,    0.0)]
    [InlineData(ThreatLevel.Low,      ThreatLevel.Critical, 0.0)]
    public void ClassificationScore_GivenLevels_ReturnsExpected(
        ThreatLevel team, ThreatLevel correct, double expected)
    {
        bool bothNoise = team == ThreatLevel.Noise && correct == ThreatLevel.Noise;
        double actual = bothNoise ? 1.2 :
            Math.Abs((int)team - (int)correct) switch
            {
                0 => 1.0,
                1 => 0.5,
                _ => 0.0
            };

        Assert.Equal(expected, actual);
    }
}
