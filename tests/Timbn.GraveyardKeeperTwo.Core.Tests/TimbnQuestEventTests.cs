using EventType = GlobalEventsSystem.Event.Type;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestEventTests
{
    public static TheoryData<TimbnQuestEvent, EventType, string> Factories => new()
    {
        { TimbnQuestEvent.Crafted("candle"), EventType.CraftFinish, "candle" },
        { TimbnQuestEvent.Built("workbench"), EventType.BuildBuilding, "workbench" },
        { TimbnQuestEvent.EnteredZone("zone_church"), EventType.PlayerEnterGDZone, "zone_church" },
        { TimbnQuestEvent.EnteredWorldZone("graveyard"), EventType.WalkIntoWorldZone, "graveyard" },
        { TimbnQuestEvent.WonFight("fight_1"), EventType.FightWon, "fight_1" },
        { TimbnQuestEvent.Slept(), EventType.AfterSleep, "" },
        { TimbnQuestEvent.UsedItem("grape_wine:2"), EventType.PlayerUseItem, "grape_wine:2" },
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void EachFactoryMatchesItsOwnGameEvent(TimbnQuestEvent quest, EventType type, string id)
    {
        Assert.True(quest.Matches(type, id));
    }

    [Fact]
    public void AnotherTypeWithTheSameIdDoesNotMatch() =>
        Assert.False(TimbnQuestEvent.Crafted("candle").Matches(EventType.BuildBuilding, "candle"));

    [Fact]
    public void AnotherIdOfTheSameTypeDoesNotMatch() =>
        Assert.False(TimbnQuestEvent.Crafted("candle").Matches(EventType.CraftFinish, "torch"));

    [Fact]
    public void IdsCompareExactlyByCase() =>
        Assert.False(TimbnQuestEvent.Crafted("candle").Matches(EventType.CraftFinish, "Candle"));

    [Fact]
    public void ANullIdCountsAsEmpty()
    {
        Assert.True(TimbnQuestEvent.Slept().Matches(EventType.AfterSleep, null!));
        Assert.False(TimbnQuestEvent.Crafted("candle").Matches(EventType.CraftFinish, null!));
    }

    [Fact]
    public void ToStringNamesTheTypeAndId()
    {
        Assert.Equal("AfterSleep", TimbnQuestEvent.Slept().ToString());
        Assert.Equal("CraftFinish candle", TimbnQuestEvent.Crafted("candle").ToString());
    }
}
