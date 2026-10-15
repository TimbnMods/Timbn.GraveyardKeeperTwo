namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnMarkupTests
{
    [Fact]
    public void IconWritesTheGamesIconMarkup() =>
        Assert.Equal("(*slv*)", TimbnMarkup.Icon("slv"));

    [Theory]
    [InlineData(TimbnWeekday.Gluttony, "(*day_gluttony*)")]
    [InlineData(TimbnWeekday.Sloth, "(*day_sloth*)")]
    [InlineData(TimbnWeekday.Lust, "(*day_lust*)")]
    [InlineData(TimbnWeekday.Envy, "(*day_envy*)")]
    [InlineData(TimbnWeekday.Pride, "(*day_pride*)")]
    [InlineData(TimbnWeekday.Wrath, "(*day_wrath*)")]
    public void DayNamesTheGamesDayIcon(TimbnWeekday day, string expected) =>
        Assert.Equal(expected, TimbnMarkup.Weekday(day));

    [Fact]
    public void EveryWeekdayHasALowercaseIcon()
    {
        foreach (var day in Enum.GetValues<TimbnWeekday>())
        {
            var markup = TimbnMarkup.Weekday(day);
            Assert.Equal(markup.ToLowerInvariant(), markup);
            Assert.StartsWith("(*day_", markup);
            Assert.EndsWith("*)", markup);
        }
    }

    [Fact]
    public void TextWritesANestedKey() =>
        Assert.Equal("#(npc_herm)", TimbnMarkup.Localized("npc_herm"));

    [Fact]
    public void MarkupKeepsTheNameAsGiven()
    {
        Assert.Equal("(*icon-quality-gold*)", TimbnMarkup.Icon("icon-quality-gold"));
        Assert.Equal("#(grape_wine:2)", TimbnMarkup.Localized("grape_wine:2"));
    }
}
