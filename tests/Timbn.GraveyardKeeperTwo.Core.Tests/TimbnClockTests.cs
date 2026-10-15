namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnClockTests
{
    [Theory]
    [InlineData(0.25f, 0.5f, 0.25f)]
    [InlineData(0.9f, 0.1f, 0.2f)]
    [InlineData(0.5f, 0.5f, 0f)]
    [InlineData(0f, 0.999f, 0.999f)]
    public void DayFractionCountsForwardAcrossMidnight(float from, float to, float expected) =>
        Assert.Equal(expected, TimbnClock.DayFractionBetween(from, to), 0.0001f);

    [Theory]
    [InlineData(0.3f, 0.3f)]
    [InlineData(0.3f, 1.3f)]
    [InlineData(0f, 1f)]
    [InlineData(0.5f, 2.5f)]
    public void DayFractionNeverReachesAWholeDay(float from, float to)
    {
        var fraction = TimbnClock.DayFractionBetween(from, to);

        Assert.True(fraction >= 0f, $"{fraction} is below 0");
        Assert.True(fraction < 1f, $"{fraction} reached 1");
    }

    [Fact]
    public void EveryWeekdayHasItsOwnId()
    {
        var ids = Enum.GetValues<TimbnWeekday>().Select(TimbnClock.WeekdayId).ToList();

        Assert.Equal(6, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.False(string.IsNullOrEmpty(id)));
    }

    [Fact]
    public void WeekdayIdsAreTheGamesDayConstants()
    {
        Assert.Equal(LazyConsts.ConstDefs.DAY_GLUTTONY, TimbnClock.WeekdayId(TimbnWeekday.Gluttony));
        Assert.Equal(LazyConsts.ConstDefs.DAY_WRATH, TimbnClock.WeekdayId(TimbnWeekday.Wrath));
    }

    [Fact]
    public void AtTheMainMenuEverythingReadsAsZero()
    {
        Assert.Equal(0, TimbnClock.Day);
        Assert.Equal(0, TimbnClock.WeekdayNumber);
        Assert.Null(TimbnClock.Weekday);
        Assert.Equal(0f, TimbnClock.TimeOfDay);
        Assert.Equal(0d, TimbnClock.Minutes);
        Assert.Equal("00:00", TimbnClock.TimeText);
        Assert.False(TimbnClock.IsPaused);
    }

    [Fact]
    public void SettingTheTimeAtTheMainMenuDoesNothing() =>
        Assert.False(TimbnClock.SetTime(0.5f));
}
