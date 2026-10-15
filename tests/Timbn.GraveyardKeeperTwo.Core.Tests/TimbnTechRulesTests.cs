namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnTechRulesTests
{
    [Fact]
    public void TheSameSpotOverlaps() =>
        Assert.True(TimbnTechRules.Overlaps(8f, 2f, 8f, 2f));

    [Theory]
    [InlineData(9f, 2f)]
    [InlineData(7f, 2f)]
    [InlineData(8f, 3f)]
    [InlineData(8f, 1f)]
    public void TheNextColumnOrRowIsFree(float column, float row) =>
        Assert.False(TimbnTechRules.Overlaps(8f, 2f, column, row));

    [Theory]
    [InlineData(8f, 2.5f)]
    [InlineData(8f, 1.5f)]
    [InlineData(8.5f, 2f)]
    public void AHalfStepIntoTheNodeOverlaps(float column, float row) =>
        Assert.True(TimbnTechRules.Overlaps(8f, 2f, column, row));

    [Fact]
    public void AHalfStepRowGapStaysFree() =>
        Assert.False(TimbnTechRules.Overlaps(7f, 0.5f, 7f, 2f));
}
