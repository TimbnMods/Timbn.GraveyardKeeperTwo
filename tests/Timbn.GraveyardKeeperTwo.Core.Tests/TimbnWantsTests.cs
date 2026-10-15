namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnWantsTests
{
    [Fact]
    public void NothingAskedGivesNoWants() =>
        Assert.Null(TimbnWants.From(null, null));

    [Fact]
    public void EitherNeedsOrCostMakesWants()
    {
        var fromNeeds = TimbnWants.From(need => need.Item("shovel"), null);
        var fromCost = TimbnWants.From(null, cost => cost.Money(50));

        Assert.NotNull(fromNeeds);
        Assert.NotNull(fromCost);
        Assert.False(fromNeeds!.IsEmpty);
        Assert.False(fromCost!.IsEmpty);
    }

    [Fact]
    public void WantsBuiltWithNothingAreEmpty()
    {
        var wants = TimbnWants.From(_ => { }, _ => { });

        Assert.NotNull(wants);
        Assert.True(wants!.IsEmpty);
        Assert.False(wants.WantsReputation);
    }

    [Fact]
    public void ReputationAloneIsNotEmpty()
    {
        var wants = new TimbnWants();
        new TimbnNeeds(wants).Reputation(20);

        Assert.False(wants.IsEmpty);
        Assert.True(wants.WantsReputation);
    }

    [Fact]
    public void ReputationNeedsAddUp()
    {
        var wants = new TimbnWants();
        new TimbnNeeds(wants).Reputation(20).Reputation(15);

        var requirement = Assert.Single(wants.ToRequirements("npc_larry_REP"));
        Assert.Equal(35f, requirement.gameResAtom.value);
    }

    [Fact]
    public void GratitudeIsALockOnHappiness()
    {
        var wants = new TimbnWants();
        new TimbnNeeds(wants).Gratitude(3);

        var requirement = Assert.Single(wants.ToRequirements(null));
        Assert.Equal(QuestPhraseRequirement.Requirement.Lock, requirement.requirement);
        Assert.Equal("happiness", requirement.gameResAtom.type);
        Assert.Equal(3f, requirement.gameResAtom.value);
    }

    [Fact]
    public void ACostResourceIsAPrice()
    {
        var wants = new TimbnWants();
        new TimbnCost(wants).Resource("faith", 2.5f).Money(150);

        Assert.Collection(
            wants.ToRequirements(null),
            r =>
            {
                Assert.Equal(QuestPhraseRequirement.Requirement.Price, r.requirement);
                Assert.Equal("faith", r.gameResAtom.type);
                Assert.Equal(2.5f, r.gameResAtom.value);
            },
            r =>
            {
                Assert.Equal(QuestPhraseRequirement.Requirement.Price, r.requirement);
                Assert.Equal("money", r.gameResAtom.type);
                Assert.Equal(150f, r.gameResAtom.value);
            });
    }

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
    }

    [Fact]
    public void ADayAndAnOrderReachTheAnswer()
    {
        var wants = new TimbnWants();
        new TimbnNeeds(wants).OnWeekday(TimbnWeekday.Wrath).OrderCompleted("quest_order_iron");
        var answer = new TimbnAnswer("a");

        wants.ApplyTo(answer, null);

        var data = Assert.IsType<AnswerData>(answer.ToData());
        Assert.Equal(TimbnClock.WeekdayId(TimbnWeekday.Wrath), data.dayNumber);
        Assert.Equal("quest_order_iron", data.order);
    }

    [Fact]
    public void AnAnswerWithNoExtrasHasNoData() =>
        Assert.Null(new TimbnAnswer("a").ToData());

    [Fact]
    public void RewardsStartEmptyAndFillUp()
    {
        var rewards = new TimbnRewards();

        Assert.True(rewards.IsEmpty);
        Assert.False(rewards.Money(1).IsEmpty);
        Assert.False(new TimbnRewards().Item("flitch").IsEmpty);
        Assert.False(new TimbnRewards().Resource("faith", 1).IsEmpty);
        Assert.False(new TimbnRewards().Reputation(1).IsEmpty);
        Assert.False(new TimbnRewards().ShowItem("flitch").IsEmpty);
        Assert.False(new TimbnRewards().ShowResource("money", 1).IsEmpty);
    }

    [Fact]
    public void MoneyAndReputationAddUp()
    {
        var rewards = new TimbnRewards().Money(100).Money(250).Reputation(5).Reputation(-2);

        Assert.Equal(350, rewards.MoneyTotal);
        Assert.Equal(3, rewards.ReputationTotal);
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
    }

    [Fact]
    public void NoMoneyRewardAddsNoMoneyLine()
    {
        var answer = new TimbnAnswer("a");

        new TimbnRewards().Item("flitch").ApplyTo(answer, null);

        var data = Assert.IsType<AnswerData>(answer.ToData());
        Assert.Empty(data.rewardRes.gameRes.List);
    }
}
