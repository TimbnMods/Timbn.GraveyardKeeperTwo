namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnWantsTests
{
    [Fact]
    public void NeedsLockTheAnswerAndCostsPriceIt()
    {
        var wants = new TimbnWants();
        new TimbnNeeds(wants).Item("shovel").Money(100);
        new TimbnCost(wants).Item("flitch", 2).Resource("faith", 1);
        var answer = new TimbnAnswer("a");

        wants.ApplyTo(answer, null);

        var data = Assert.IsType<AnswerData>(answer.ToData());
        Assert.Equal("shovel", Assert.Single(data.lockRes.items).itemId);
        Assert.Equal(100f, Assert.Single(data.lockRes.gameRes.List).value);
        Assert.Equal(2, Assert.Single(data.costRes.items, i => i.itemId == "flitch").count);
        Assert.Equal("faith", Assert.Single(data.costRes.gameRes.List).type);

        var priced = new TimbnWants();
        new TimbnCost(priced).Resource("faith", 2.5f).Money(150);

        Assert.Equal([("faith", 2.5f), ("money", 150f)], priced.ToRequirements(null).Select(r => (r.gameResAtom.type, r.gameResAtom.value)));
        Assert.All(priced.ToRequirements(null), r => Assert.Equal(QuestPhraseRequirement.Requirement.Price, r.requirement));
    }

    [Fact]
    public void ANegativeReputationRewardStillShows()
    {
        var answer = new TimbnAnswer("a");

        new TimbnRewards().Reputation(-5).ApplyTo(answer, "npc_larry_REP");

        var data = Assert.IsType<AnswerData>(answer.ToData());
        Assert.Equal(-5f, Assert.Single(data.fakeRewardRes.gameRes.List).value);
    }

    [Fact]
    public void ShownRewardsMergeIntoOneList()
    {
        var answer = new TimbnAnswer("a");

        new TimbnRewards().ShowItem("flitch", 2).ShowItem("stone").ShowResource("money", 5).ShowResource("faith", 1).ApplyTo(answer, null);

        var data = Assert.IsType<AnswerData>(answer.ToData());
        Assert.Equal(["flitch", "stone"], data.fakeRewardRes.items.Select(i => i.itemId));
        Assert.Equal(["money", "faith"], data.fakeRewardRes.gameRes.List.Select(r => r.type));

        var noMoney = new TimbnAnswer("b");

        new TimbnRewards().Item("flitch").ApplyTo(noMoney, null);

        Assert.Empty(Assert.IsType<AnswerData>(noMoney.ToData()).rewardRes.gameRes.List);
    }
}
