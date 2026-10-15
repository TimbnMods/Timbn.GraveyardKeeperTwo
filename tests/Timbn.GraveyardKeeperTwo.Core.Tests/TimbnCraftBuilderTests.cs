namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnCraftBuilderTests
{
    private static TimbnCraftBuilder Craft() => new("timbn_cheese_pie", "", "");

    [Fact]
    public void ACraftStartsAsACopyOfTheWoodenPlankWithNothingOverridden()
    {
        var craft = Craft();

        Assert.Equal(TimbnCraftRules.DefaultTemplate, craft.TemplateId);
        Assert.Null(craft.StationIds);
        Assert.Null(craft.NeedItems);
        Assert.Null(craft.Outputs);
        Assert.Null(craft.MasteryLevel);
        Assert.Null(craft.TechId);
        Assert.False(craft.MustBeUnlocked);
    }

    [Fact]
    public void ATechThatGrantsTheCraftAlsoKeepsItHiddenUntilThen()
    {
        var craft = Craft().BasedOn("berry_pie").UnlockedBy("cooking_fire");

        Assert.Equal("berry_pie", craft.TemplateId);
        Assert.Equal("cooking_fire", craft.TechId);
        Assert.True(craft.MustBeUnlocked);
    }

    [Fact]
    public void NeedsAndMakesKeepTheirCountExpressionsAsWritten()
    {
        var craft = Craft()
            .Needs(("dough", 1), ("cheese", 1))
            .Needs("fire", "10-5*#perk_heat_saver")
            .Makes("timbn_cheese_pie", "2+1*#perk_cook_2");

        Assert.Equal([("dough", "1"), ("cheese", "1"), ("fire", "10-5*#perk_heat_saver")], craft.NeedItems);
        Assert.Equal([("timbn_cheese_pie", "2+1*#perk_cook_2")], craft.Outputs);
    }

    [Fact]
    public void MasteryIsOnlySetWhenAsked()
    {
        Assert.Equal(3, Craft().Mastery(3).MasteryLevel);
    }

    [Fact]
    public void DurationTicksIsOnlySetWhenAsked()
    {
        Assert.Null(Craft().WorkTicks);
        Assert.Equal(6, Craft().DurationTicks(6).WorkTicks);
    }
}
