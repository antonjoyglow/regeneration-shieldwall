using Xunit;

namespace ShieldWall.Tests.Scoring;

public sealed class LatencyScoringTests
{
    [Theory]
    [InlineData(100,   1.0)]
    [InlineData(499,   1.0)]
    [InlineData(500,   0.9)]
    [InlineData(1999,  0.9)]
    [InlineData(2000,  0.7)]
    [InlineData(4999,  0.7)]
    [InlineData(5000,  0.5)]
    [InlineData(10000, 0.5)]
    public void LatencyMultiplier_GivenMs_ReturnsExpected(double latencyMs, double expected)
    {
        double actual = latencyMs switch
        {
            < 500  => 1.0,
            < 2000 => 0.9,
            < 5000 => 0.7,
            _      => 0.5
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AlertTotal_PerfectScore_ReturnsOne()
    {
        var total = (1.0 * 0.6) + (1.0 * 0.4);

        Assert.Equal(1.0, total);
    }

    [Fact]
    public void AlertTotal_NoiseBonusWithDismissBonus_ReturnsAboveOne()
    {
        var total = (1.2 * 0.6) + (1.1 * 0.4);

        Assert.Equal(1.16, total, 2);
    }
}
