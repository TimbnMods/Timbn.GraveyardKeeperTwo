using Step = Timbn.GraveyardKeeperTwo.Core.Framework.TimbnQuestPhases.Step;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestPhasesTests
{
    [Theory]
    [InlineData(2, new[] { 2, 4 })]
    [InlineData(3, new[] { 1, 3, 5 })]
    [InlineData(6, new[] { 1, 1, 2, 3, 4, 5 })]
    [InlineData(10, new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 })]
    public void ChainsSpreadAcrossTheRingsLikeTheGamesStorylines(int count, int[] expected) =>
        Assert.Equal(expected, TimbnQuestPhases.Spread(count));

    [Fact]
    public void AnAuthorPhaseInTheMiddleLeavesTheRestOfTheChainSpreadOverItsFullLength()
    {
        var phases = TimbnQuestPhases.Assign([new Step("a", null, 0), new Step("b", "a", 4), new Step("c", "b", 0)]);

        Assert.Equal(1, phases["a"]);
        Assert.Equal(4, phases["b"]);
        Assert.Equal(5, phases["c"]);

        var set = TimbnQuestPhases.Assign([new Step("ask", null, 0), new Step("pay", "ask", 5)]);

        Assert.Equal(2, set["ask"]);
        Assert.Equal(5, set["pay"]);
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

        var alone = TimbnQuestPhases.Assign([new Step("alone", null, 0), new Step("chosen", null, 3)]);

        Assert.Equal(0, alone["alone"]);
        Assert.Equal(3, alone["chosen"]);
    }
}
