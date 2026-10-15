namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestRunTests
{
    [Fact]
    public void RunValuesReadBack()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData { Day = 12 })
            .Npc("villager", "npc_larry")
            .Item("dish", "cook_muffin")
            .Number("count", 3);

        Assert.Equal("npc_larry", run.NpcId("villager"));
        Assert.Equal("cook_muffin", run.Item("dish"));
        Assert.Equal(3, run.Number("count"));
        Assert.Equal(12, run.Day);
    }

    [Fact]
    public void ARunValueCanBeSetAgainAsTheSameKind()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Item("dish", "cook_muffin").Item("dish", "bread");

        Assert.Equal("bread", run.Item("dish"));
    }

    [Fact]
    public void ARunValueNameHoldsOneKind()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Npc("who", "npc_larry");

        Assert.Throws<ArgumentException>(() => run.Item("who", "bread"));
    }

    [Fact]
    public void ARunValueNameIsLowercase()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData());

        Assert.Throws<ArgumentException>(() => run.Number("Count", 1));
    }

    [Fact]
    public void AMissingRunValueSaysWhichOne()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData());

        var error = Assert.Throws<KeyNotFoundException>(() => run.Item("dish"));
        Assert.Contains("'dish'", error.Message);
    }

    [Fact]
    public void ANumberShowsAsItselfAndAnUnknownNameShowsNothing()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Number("count", 3);

        Assert.Equal(3.ToString(System.Globalization.CultureInfo.CurrentCulture), run.Shown("count"));
        Assert.Null(run.Shown("dish"));
    }

    [Fact]
    public void ATextShowsItsLineByKey()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Text("delivering", "my_errand_potion");

        Assert.Equal("my_errand_potion", run.Text("delivering"));
        Assert.Equal("#(my_errand_potion)", run.Shown("delivering"));
    }

    [Fact]
    public void PlaceholdersAreLowercaseNamesInBraces()
    {
        Assert.Matches(TimbnQuestBuilder.Placeholder, "Cook {dish} for {villager_2}.");
        Assert.DoesNotMatch(TimbnQuestBuilder.Placeholder, "Cook {Dish} or {0} or { dish }.");
    }

    [Fact]
    public void TextWithAPlaceholderNeedsASetup()
    {
        var error = Assert.Throws<ArgumentException>(() => Build(q => q.Description("Cook {dish}.").ReadyByCode()));
        Assert.Contains("{dish}", error.Message);
    }

    [Fact]
    public void ALineWithAPlaceholderNeedsASetup() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.Talk(talk => talk.Say("Got my {dish}?").Accept("Here."))));

    [Fact]
    public void ACostReadFromTheRunNeedsASetup() =>
        Assert.Throws<ArgumentException>(() => Build(q => q
            .Cost((cost, run) => cost.Item(run.Item("dish")))
            .Talk(talk => talk.Accept("Here."))));

    [Fact]
    public void AnIconReadFromTheRunNeedsASetup() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.Icon(run => run.Item("dish")).ReadyByCode()));

    [Fact]
    public void AnIconReadFromTheRunReachesEveryStage()
    {
        var quest = Build(q => q
            .Setup(_ => true)
            .Icon(run => run.Item("dish"))
            .AddStage("one", "One.", stage => stage.ReadyByCode())
            .AddStage("two", "Two.", stage => stage.ReadyByCode()));

        Assert.All(quest.Steps, step => Assert.NotNull(step.IconOf));
    }

    [Fact]
    public void ExpiringNeedsASetup() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.ExpiresAfter(7).ReadyByCode()));

    [Fact]
    public void ExpiringTakesAtLeastADay() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(q => q.Setup(_ => true).ExpiresAfter(0).ReadyByCode()));

    [Fact]
    public void AQuestWithNoNpcNeedsAGiver()
    {
        var builder = new TimbnQuestBuilder("q", "", "Quest");
        builder.ReadyByCode();

        Assert.Throws<ArgumentException>(builder.Finish);
    }

    [Fact]
    public void AGiverReadFromTheRunNeedsASetup()
    {
        var builder = new TimbnQuestBuilder("q", "", "Quest");
        builder.Giver(run => run.NpcId("villager")).ReadyByCode();

        Assert.Throws<ArgumentException>(builder.Finish);
    }

    [Fact]
    public void AGiverReadFromTheRunReachesEveryStage()
    {
        var builder = new TimbnQuestBuilder("q", "", "Quest");
        builder
            .Setup(run =>
            {
                run.Npc("villager", "npc_larry");
                return true;
            })
            .Giver(run => run.NpcId("villager"))
            .AddStage("ask", "Ask {villager}.", stage => stage.ReadyByCode())
            .AddStage("bring", "Bring it.", stage => stage.ReadyByCode());
        builder.Finish();

        Assert.All(builder.Steps, step => Assert.NotNull(step.GiverOf));
    }

    [Fact]
    public void ACostReadFromTheRunUsesTheRun()
    {
        var quest = Build(q => q
            .Setup(_ => true)
            .Cost(cost => cost.Item("bread"))
            .Cost((cost, run) => cost.Item(run.Item("dish"), 2))
            .Talk(talk => talk.Accept("Here.")));
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Item("dish", "cook_muffin");

        var items = quest.CostFor(run).ToRequirements(null).Select(requirement => (requirement.itemCount.itemId, requirement.itemCount.count));
        Assert.Equal([("bread", 1), ("cook_muffin", 2)], items);
        Assert.Single(quest.CostFor(null).ToRequirements(null));
    }

    [Fact]
    public void ARewardReadFromTheRunUsesTheRun()
    {
        var quest = Build(q => q
            .Setup(_ => true)
            .Reward(reward => reward.Money(5))
            .Reward((reward, run) => reward.Money(run.Number("price")))
            .Talk(talk => talk.Accept("Here.")));
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Number("price", 40);

        Assert.Equal(45, quest.RewardsFor(run).MoneyTotal);
        Assert.Equal(5, quest.RewardsFor(null).MoneyTotal);
    }

    [Fact]
    public void ANpcReadFromTheRunMakesTheQuestItsOwnStep()
    {
        var quest = Build(q => q
            .Setup(_ => true)
            .TalksWith(run => run.NpcId("to"))
            .AddStage("next", "Next.", stage => stage.ReadyByCode())
            .Talk(talk => talk.Accept("Here.")));

        Assert.Equal(["q", "q_next"], quest.Steps.Select(step => step.Id));
    }

    [Fact]
    public void AnNpcShowsAsTheirName()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Npc("villager", "npc_larry");

        Assert.Equal("#(npc_larry)", run.Shown("villager"));
        Assert.Equal("npc_larry", run.NpcId("villager"));
    }

    [Fact]
    public void AnItemShowsAsItsNameWithoutTheQualitySuffixWhenTheGameHasNoLineForIt()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData()).Item("dish", "grape_wine:2").Item("wood", "flitch");

        Assert.Equal("#(grape_wine)", run.Shown("dish"));
        Assert.Equal("#(flitch)", run.Shown("wood"));
        Assert.Equal("grape_wine:2", run.Item("dish"));
    }

    [Fact]
    public void EachKindOfMissingValueNamesItsKind()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData());

        Assert.Contains("text value named 'a'", Assert.Throws<KeyNotFoundException>(() => run.Text("a")).Message);
        Assert.Contains("npcid value named 'a'", Assert.Throws<KeyNotFoundException>(() => run.NpcId("a")).Message);
        Assert.Contains("item value named 'a'", Assert.Throws<KeyNotFoundException>(() => run.Item("a")).Message);
        Assert.Contains("number value named 'a'", Assert.Throws<KeyNotFoundException>(() => run.Number("a")).Message);
    }

    [Fact]
    public void TheDayComesFromTheSavedData()
    {
        var run = new TimbnQuestRun(new TimbnQuestRunData { Day = 14 });

        Assert.Equal(14, run.Day);
    }

    [Fact]
    public void SettingWritesIntoTheSavedData()
    {
        var data = new TimbnQuestRunData();

        new TimbnQuestRun(data).Npc("who", "npc_herm").Item("what", "flitch").Number("count", 2).Text("line", "my_line");

        Assert.Equal("npc_herm", data.Npcs["who"]);
        Assert.Equal("flitch", data.Items["what"]);
        Assert.Equal(2, data.Numbers["count"]);
        Assert.Equal("my_line", data.Texts["line"]);
    }

    private static TimbnQuestBuilder Build(Action<TimbnQuestBuilder> quest)
    {
        var builder = new TimbnQuestBuilder("q", "npc_larry", "Quest");
        quest(builder);
        builder.Finish();
        return builder;
    }
}
