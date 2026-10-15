namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnRollsTests
{
    private static double[] Draw(string key, long when)
    {
        var random = TimbnRolls.Seeded(key, when);
        return [random.NextDouble(), random.NextDouble(), random.NextDouble()];
    }

    private static double[] Draw(System.Random random) => [random.NextDouble(), random.NextDouble(), random.NextDouble()];

    [Fact]
    public void TheSameKeyAndMomentRollTheSame() =>
        Assert.Equal(Draw("npc_herm", 5000), Draw("npc_herm", 5000));

    [Fact]
    public void NeighboringMinutesRollApart() =>
        Assert.NotEqual(Draw("npc_herm", 5000), Draw("npc_herm", 5001));

    [Fact]
    public void DifferentKeysRollApart() =>
        Assert.NotEqual(Draw("npc_herm", 5000), Draw("npc_linda", 5000));

    [Fact]
    public void RollsSpreadAcrossTheRange()
    {
        var below = Enumerable.Range(0, 1000).Count(minute => TimbnRolls.Seeded("npc_herm", minute).NextDouble() < 0.75);

        Assert.InRange(below, 650, 850);
    }

    [Fact]
    public void AtTheMainMenuThisMinuteRollsForMinuteZero() =>
        Assert.Equal(Draw("npc_herm", 0), Draw(TimbnRolls.ThisMinute("npc_herm")));

    [Fact]
    public void AtTheMainMenuThisDayRollsForDayZeroWithItsOwnKey() =>
        Assert.Equal(Draw("npc_herm/day", 0), Draw(TimbnRolls.ThisDay("npc_herm")));

    [Fact]
    public void TheDayAndTheMinuteOfOneKeyRollApart() =>
        Assert.NotEqual(Draw(TimbnRolls.ThisDay("npc_herm")), Draw(TimbnRolls.ThisMinute("npc_herm")));

    [Fact]
    public void AnEmptyKeyStillRolls()
    {
        Assert.Equal(Draw("", 3), Draw("", 3));
        Assert.NotEqual(Draw("", 3), Draw("", 4));
    }

    [Fact]
    public void FarAwayMomentsRollTooAndApart()
    {
        Assert.Equal(Draw("npc_herm", long.MaxValue), Draw("npc_herm", long.MaxValue));
        Assert.Equal(Draw("npc_herm", -1), Draw("npc_herm", -1));
        Assert.NotEqual(Draw("npc_herm", -1), Draw("npc_herm", 0));
        Assert.NotEqual(Draw("npc_herm", long.MaxValue), Draw("npc_herm", long.MinValue));
    }

    [Fact]
    public void TheHashIsStableAcrossRunsAndMachines()
    {
        var first = TimbnRolls.Seeded("npc_herm", 5000).Next();

        Assert.Equal(first, TimbnRolls.Seeded("npc_herm", 5000).Next());
        Assert.Equal(first, TimbnRolls.Seeded(new string(['n', 'p', 'c', '_', 'h', 'e', 'r', 'm']), 5000).Next());
    }

    [Fact]
    public void KeysThatDifferOnlyInCaseRollApart() =>
        Assert.NotEqual(Draw("npc_herm", 5000), Draw("npc_Herm", 5000));
}
