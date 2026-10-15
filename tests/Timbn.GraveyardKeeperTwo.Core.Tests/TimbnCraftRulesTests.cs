namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnCraftRulesTests
{
    [Fact]
    public void TheStationDecidesWhetherACraftRunsItself()
    {
        Assert.True(TimbnCraftRules.IsAuto(stationRunsItself: true, templateIsAuto: false));
        Assert.False(TimbnCraftRules.IsAuto(stationRunsItself: false, templateIsAuto: true));
    }

    [Fact]
    public void TheTemplateDecidesWhenTheStationIsUnknown()
    {
        Assert.True(TimbnCraftRules.IsAuto(null, templateIsAuto: true));
        Assert.False(TimbnCraftRules.IsAuto(null, templateIsAuto: false));
    }

    [Fact]
    public void ACraftIsNamedByItsIdWithTheDescriptionUnderIdD()
    {
        var texts = TimbnCraftRules.Texts("timbn_stick_box_sticks", "Sticks from a Board", "Split one board into 4 sticks.");

        Assert.Equal("Sticks from a Board", texts["timbn_stick_box_sticks"]);
        Assert.Equal("Split one board into 4 sticks.", texts["timbn_stick_box_sticks_d"]);
        Assert.Equal(2, texts.Count);
    }

    [Fact]
    public void EmptyTextIsLeftToTheLangFiles()
    {
        Assert.Empty(TimbnCraftRules.Texts("timbn_cheese_pie", "", ""));
    }

    [Fact]
    public void TheWoodenPlankIsTheDefaultTemplate()
    {
        Assert.Equal("wooden_plank", TimbnCraftRules.DefaultTemplate);
    }
}
