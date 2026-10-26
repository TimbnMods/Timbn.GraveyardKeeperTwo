namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestRunTests
{
    [Fact]
    public void PlaceholdersAreLowercaseNamesInBraces()
    {
        Assert.Matches(TimbnQuestBuilder.Placeholder, "Cook {dish} for {villager_2}.");
        Assert.DoesNotMatch(TimbnQuestBuilder.Placeholder, "Cook {Dish} or {0} or { dish }.");
    }

    [Fact]
    public void AnItemShowsAsItsNameWithoutTheQualitySuffixWhenTheGameHasNoLineForIt()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Item("dish", "grape_wine:2").Item("wood", "flitch");

        Assert.Equal("#(grape_wine)", run.Shown("dish"));
        Assert.Equal("#(flitch)", run.Shown("wood"));
        Assert.Equal("grape_wine:2", run.Item("dish"));

        var other = new TimbnQuestRun(new TimbnQuestRunData()).Npc("villager", "npc_larry").Number("count", 3).Text("delivering", "my_errand_potion");

        Assert.Equal("#(npc_larry)", other.Shown("villager"));
        Assert.Equal(3.ToString(System.Globalization.CultureInfo.CurrentCulture), other.Shown("count"));
        Assert.Equal("#(my_errand_potion)", other.Shown("delivering"));
        Assert.Null(other.Shown("dish"));
    }

    private static TimbnQuestBuilder Build(Action<TimbnQuestBuilder> quest)
    {
        var builder = new TimbnQuestBuilder("q", "npc_larry", "Quest");
        quest(builder);
        builder.Finish();
        return builder;
    }
}
