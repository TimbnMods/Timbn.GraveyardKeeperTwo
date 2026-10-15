namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnDayPlanTests
{
    private static readonly float[] _starts = [0.25f, 0.70f, 0.85f];

    [Theory]
    [InlineData(0.25f, 0)]
    [InlineData(0.5f, 0)]
    [InlineData(0.70f, 1)]
    [InlineData(0.84f, 1)]
    [InlineData(0.9f, 2)]
    [InlineData(0.1f, 2)]
    [InlineData(0f, 2)]
    public void BlockAtPicksTheLatestStartAndWrapsBeforeDawn(float timeOfDay, int expected) =>
        Assert.Equal(expected, TimbnDayPlan.BlockAt(_starts, timeOfDay));

    [Fact]
    public void BlockAtIgnoresTheOrderOfTheList()
    {
        float[] starts = [0.85f, 0.25f, 0.70f];

        Assert.Equal(1, TimbnDayPlan.BlockAt(starts, 0.3f));
        Assert.Equal(0, TimbnDayPlan.BlockAt(starts, 0.2f));
    }

    [Fact]
    public void BlockAtWithNoBlocksIsNone() => Assert.Equal(-1, TimbnDayPlan.BlockAt([], 0.5f));

    [Fact]
    public void LastStartIsTodaysBlockStart() =>
        Assert.Equal(TimbnDayPlan.Minutes(4, 0.70f), TimbnDayPlan.LastStart(_starts, 4, 0.8f));

    [Fact]
    public void LastStartBeforeDawnIsYesterdaysLastBlock() =>
        Assert.Equal(TimbnDayPlan.Minutes(3, 0.85f), TimbnDayPlan.LastStart(_starts, 4, 0.1f));

    [Fact]
    public void LastStartOfAOneBlockDayMovesOnEachDay()
    {
        float[] starts = [0.25f];

        Assert.Equal(TimbnDayPlan.Minutes(4, 0.25f), TimbnDayPlan.LastStart(starts, 4, 0.9f));
        Assert.Equal(TimbnDayPlan.Minutes(5, 0.25f), TimbnDayPlan.LastStart(starts, 5, 0.3f));
        Assert.True(TimbnDayPlan.LastStart(starts, 5, 0.3f) > TimbnDayPlan.Minutes(4, 0.9f));
    }

    [Fact]
    public void TakeBackIsDueOnceAtTheNextBlockStart()
    {
        var leftAt = TimbnDayPlan.Minutes(4, 0.3f);

        Assert.False(TimbnDayPlan.IsTakeBackDue(_starts, 4, 0.5f, ref leftAt));
        Assert.True(TimbnDayPlan.IsTakeBackDue(_starts, 4, 0.70f, ref leftAt));
        Assert.Equal(TimbnDayPlan.Minutes(4, 0.70f), leftAt);
        Assert.False(TimbnDayPlan.IsTakeBackDue(_starts, 4, 0.75f, ref leftAt));
    }

    [Fact]
    public void TakeBackAfterTheClockIsSetBackCountsTheNextStartFromTheNewTime()
    {
        var leftAt = TimbnDayPlan.Minutes(4, 0.9f);

        Assert.False(TimbnDayPlan.IsTakeBackDue(_starts, 4, 0.3f, ref leftAt));
        Assert.True(TimbnDayPlan.IsTakeBackDue(_starts, 4, 0.71f, ref leftAt));
    }

    [Fact]
    public void TakeBackPastMidnightCountsTheLastBlockOfTheDayBefore()
    {
        var leftAt = TimbnDayPlan.Minutes(4, 0.8f);

        Assert.True(TimbnDayPlan.IsTakeBackDue(_starts, 5, 0.1f, ref leftAt));
        Assert.Equal(TimbnDayPlan.Minutes(4, 0.85f), leftAt);
    }

    [Theory]
    [InlineData(0.70f, 0.60f, 144)]
    [InlineData(0.25f, 0.85f, 576)]
    [InlineData(0.25f, 0.25f, 1440)]
    public void MinutesUntilWrapsPastMidnightAndCountsAWholeDayForTheSameMoment(float start, float timeOfDay, double expected) =>
        Assert.Equal(expected, TimbnDayPlan.MinutesUntil(start, timeOfDay), 1);

    [Fact]
    public void MinutesCountWholeDaysAndThePartOfToday() =>
        Assert.Equal(3 * 1440.0 + 720.0, TimbnDayPlan.Minutes(3, 0.5f), 3);
}
