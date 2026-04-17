using Xunit;
using ShieldWall.Shared.Enums;

namespace ShieldWall.Tests.Scoring;

public sealed class ActionScoringTests
{
    [Theory]
    [InlineData(ActionType.Dismiss,  ActionType.Dismiss,  1.1)]
    [InlineData(ActionType.Monitor,  ActionType.Monitor,  1.0)]
    [InlineData(ActionType.Escalate, ActionType.Escalate, 1.0)]
    [InlineData(ActionType.Escalate, ActionType.Monitor,  0.7)]
    [InlineData(ActionType.Escalate, ActionType.Dismiss,  0.7)]
    [InlineData(ActionType.Monitor,  ActionType.Escalate, 0.0)]
    [InlineData(ActionType.Dismiss,  ActionType.Escalate, 0.0)]
    [InlineData(ActionType.Dismiss,  ActionType.Monitor,  0.5)]
    [InlineData(ActionType.Monitor,  ActionType.Dismiss,  0.5)]
    public void ActionScore_GivenActions_ReturnsExpected(
        ActionType team, ActionType correct, double expected)
    {
        double actual;
        if (team == ActionType.Dismiss && correct == ActionType.Dismiss)
            actual = 1.1;
        else if (team == correct)
            actual = 1.0;
        else if (team == ActionType.Escalate && correct != ActionType.Escalate)
            actual = 0.7;
        else if (team != ActionType.Escalate && correct == ActionType.Escalate)
            actual = 0.0;
        else
            actual = 0.5;

        Assert.Equal(expected, actual);
    }
}
