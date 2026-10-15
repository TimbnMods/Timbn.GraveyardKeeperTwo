using Step = Timbn.GraveyardKeeperTwo.Core.Framework.TimbnQuestPhases.Step;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestPhasesTests
{
    [Theory]
    [InlineData(0, new int[0])]
    [InlineData(1, new[] { 1 })]
    [InlineData(2, new[] { 2, 4 })]
    [InlineData(3, new[] { 1, 3, 5 })]
    [InlineData(4, new[] { 1, 2, 3, 4 })]
    [InlineData(5, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(6, new[] { 1, 1, 2, 3, 4, 5 })]
    [InlineData(7, new[] { 1, 1, 2, 3, 3, 4, 5 })]
    [InlineData(10, new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 })]
    public void ChainsSpreadAcrossTheRingsLikeTheGamesStorylines(int count, int[] expected) =>
        Assert.Equal(expected, TimbnQuestPhases.Spread(count));

    [Fact]
    public void ASpreadNeverLeavesTheFiveRingsAndNeverGoesBack()
    {
        for (var count = 1; count <= 40; count++)
        {
            var spread = TimbnQuestPhases.Spread(count);
            Assert.Equal(count, spread.Length);
            Assert.All(spread, ring => Assert.InRange(ring, 1, 5));
            Assert.Equal(spread.OrderBy(ring => ring), spread);
        }
    }

    [Fact]
    public void EachStepOfAChainGetsItsRing()
    {
        var phases = TimbnQuestPhases.Assign([new Step("ask", null, 0), new Step("pay", "ask", 0), new Step("deliver", "pay", 0)]);

        Assert.Equal(1, phases["ask"]);
        Assert.Equal(3, phases["pay"]);
        Assert.Equal(5, phases["deliver"]);
    }

    [Fact]
    public void APhaseTheAuthorSetIsKept()
    {
        var phases = TimbnQuestPhases.Assign([new Step("ask", null, 0), new Step("pay", "ask", 5)]);

        Assert.Equal(2, phases["ask"]);
        Assert.Equal(5, phases["pay"]);
    }

    [Fact]
    public void AnAuthorPhaseInTheMiddleLeavesTheRestOfTheChainSpreadOverItsFullLength()
    {
        var phases = TimbnQuestPhases.Assign([new Step("a", null, 0), new Step("b", "a", 4), new Step("c", "b", 0)]);

        Assert.Equal(1, phases["a"]);
        Assert.Equal(4, phases["b"]);
        Assert.Equal(5, phases["c"]);
    }

    [Fact]
    public void AQuestOnItsOwnKeepsItsOwnPhase()
    {
        var phases = TimbnQuestPhases.Assign([new Step("alone", null, 0), new Step("chosen", null, 3)]);

        Assert.Equal(0, phases["alone"]);
        Assert.Equal(3, phases["chosen"]);
    }

    [Fact]
    public void OnlyTheFirstQuestContinuingAStepIsItsNextStep()
    {
        var phases = TimbnQuestPhases.Assign([new Step("ask", null, 0), new Step("pay", "ask", 0), new Step("other", "ask", 0)]);

        Assert.Equal(2, phases["ask"]);
        Assert.Equal(4, phases["pay"]);
        Assert.Equal(0, phases["other"]);
    }

    [Fact]
    public void AChildListedBeforeItsParentStillJoinsTheChain()
    {
        var phases = TimbnQuestPhases.Assign([new Step("deliver", "pay", 0), new Step("pay", "ask", 0), new Step("ask", null, 0)]);

        Assert.Equal(1, phases["ask"]);
        Assert.Equal(3, phases["pay"]);
        Assert.Equal(5, phases["deliver"]);
    }

    [Fact]
    public void AParentOutsideTheListMakesNoChain()
    {
        var phases = TimbnQuestPhases.Assign([new Step("pay", "58_port_supplies_order", 0), new Step("deliver", "pay", 0)]);

        Assert.Equal(2, phases["pay"]);
        Assert.Equal(4, phases["deliver"]);
    }

    [Fact]
    public void TwoQuestsContinuingEachOtherHaveNoHeadAndKeepTheirPhases()
    {
        var phases = TimbnQuestPhases.Assign([new Step("a", "b", 0), new Step("b", "a", 2)]);

        Assert.Equal(0, phases["a"]);
        Assert.Equal(2, phases["b"]);
    }

    [Fact]
    public void TwoChainsAreSpreadOnTheirOwn()
    {
        var phases = TimbnQuestPhases.Assign(
        [
            new Step("a1", null, 0), new Step("a2", "a1", 0),
            new Step("b1", null, 0), new Step("b2", "b1", 0), new Step("b3", "b2", 0), new Step("b4", "b3", 0),
        ]);

        Assert.Equal([2, 4], new[] { phases["a1"], phases["a2"] });
        Assert.Equal([1, 2, 3, 4], new[] { phases["b1"], phases["b2"], phases["b3"], phases["b4"] });
    }

    [Fact]
    public void NoStepsGiveNoPhases() =>
        Assert.Empty(TimbnQuestPhases.Assign([]));
}
