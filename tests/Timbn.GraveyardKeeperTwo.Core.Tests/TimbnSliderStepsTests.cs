using Timbn.GraveyardKeeperTwo.Core.Framework;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSliderStepsTests
{
    [Theory]
    [InlineData(true, 1f, 18f, 7f, 0)]
    [InlineData(false, 3.2f, 5f, 5f, 1)]
    [InlineData(false, 0f, 1f, 0.25f, 2)]
    [InlineData(false, 0f, 1f, 0f, 1)]
    [InlineData(false, 5f, 5f, 5f, 0)]
    public void DecimalsFollowTheNumbersTheAuthorWrote(bool wholeNumbers, float min, float max, float defaultValue, int expected) =>
        Assert.Equal(expected, TimbnSliderSteps.Decimals(wholeNumbers, min, max, defaultValue));

    [Fact]
    public void ALongRangeSteps2Or5OrTenTimesLarger() => Assert.Equal(500f, TimbnSliderSteps.StepSize(0, 0f, 100000f));

    [Fact]
    public void ANumberTooBigForADecimalDoesNotThrow() => Assert.Equal(0, TimbnSliderSteps.Decimals(false, 0f, float.MaxValue, 7f));
}
