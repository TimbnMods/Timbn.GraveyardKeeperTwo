using Timbn.GraveyardKeeperTwo.Core.Framework;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSliderStepsTests
{
    [Theory]
    [InlineData(true, 1f, 18f, 7f, 0)]
    [InlineData(false, 3.2f, 5f, 5f, 1)]
    [InlineData(false, 0f, 100f, 2.5f, 1)]
    [InlineData(false, 0f, 1f, 0.25f, 2)]
    [InlineData(false, 0f, 1f, 0f, 0)]
    public void DecimalsFollowTheNumbersTheAuthorWrote(bool wholeNumbers, float min, float max, float defaultValue, int expected) =>
        Assert.Equal(expected, TimbnSliderSteps.Decimals(wholeNumbers, min, max, defaultValue));

    [Theory]
    [InlineData(0f, 1f, 0.01f)]
    [InlineData(0f, 5f, 0.05f)]
    [InlineData(0f, 10f, 0.1f)]
    [InlineData(0f, 50f, 0.5f)]
    [InlineData(0f, 100f, 1f)]
    [InlineData(0f, 200f, 1f)]
    [InlineData(0f, 500f, 5f)]
    [InlineData(0f, 5000f, 50f)]
    [InlineData(-50f, 50f, 1f)]
    public void WholeEndsStepByTheLargestRoundNumberThatGivesAHundredSteps(float min, float max, float expected) =>
        Assert.Equal(expected, TimbnSliderSteps.StepSize(false, 0, min, max));

    [Theory]
    [InlineData(true, 0, 0f, 1f, 1f)]
    [InlineData(false, 1, 0f, 1000f, 0.1f)]
    [InlineData(false, 2, 0f, 1f, 0.01f)]
    [InlineData(false, 0, 5f, 5f, 1f)]
    [InlineData(false, 0, 0f, float.PositiveInfinity, 1f)]
    public void WholeNumbersAndDecimalsSetTheStepDirectly(bool wholeNumbers, int decimals, float min, float max, float expected) =>
        Assert.Equal(expected, TimbnSliderSteps.StepSize(wholeNumbers, decimals, min, max));

    [Fact]
    public void ANumberTooBigForADecimalDoesNotThrow() => Assert.Equal(0, TimbnSliderSteps.Decimals(false, 0f, float.MaxValue, 7f));
}
