using Step = Timbn.GraveyardKeeperTwo.Core.Framework.TimbnQuestIcons.Step;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestIconsTests
{
    [Fact]
    public void AStepWithNoIconUsesTheIconUpItsChain()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("ask", null, "larry"), new Step("pay", "ask", ""), new Step("deliver", "pay", "")]);

        Assert.Equal("larry", icons["pay"]);
        Assert.Equal("larry", icons["deliver"]);
    }

    [Fact]
    public void AStepKeepsItsOwnIconAndPassesItOn()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("deliver", "pay", ""), new Step("pay", "ask", "herm"), new Step("ask", null, "larry")]);

        Assert.Equal("herm", icons["pay"]);
        Assert.Equal("herm", icons["deliver"]);
        Assert.Equal("larry", icons["ask"]);
    }

    [Fact]
    public void AQuestOnItsOwnOrInALoopWithNoIconStaysEmpty()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("alone", null, ""), new Step("a", "b", ""), new Step("b", "a", "")]);

        Assert.Equal("", icons["alone"]);
        Assert.Equal("", icons["a"]);
    }

    [Fact]
    public void ALoopWithOneIconHandsItAround()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("a", "c", ""), new Step("b", "a", ""), new Step("c", "b", "herm")]);

        Assert.Equal("herm", icons["a"]);
        Assert.Equal("herm", icons["b"]);
        Assert.Equal("herm", icons["c"]);
    }

    [Fact]
    public void ContinuingAQuestOutsideTheListGivesNoIcon()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("pay", "58_port_supplies_order", "")]);

        Assert.Equal("", icons["pay"]);
    }

    [Fact]
    public void ALongChainReachesTheHead()
    {
        List<Step> steps = [new Step("s0", null, "larry")];
        for (var i = 1; i < 12; i++)
            steps.Add(new Step($"s{i}", $"s{i - 1}", ""));

        var icons = TimbnQuestIcons.Inherit(steps);

        Assert.All(steps, step => Assert.Equal("larry", icons[step.Id]));
    }

    [Fact]
    public void TheLastOfTwoStepsWithOneIdWins()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("ask", null, "larry"), new Step("ask", null, "herm"), new Step("pay", "ask", "")]);

        Assert.Equal("herm", icons["pay"]);
        Assert.Equal(2, icons.Count);
    }

    [Fact]
    public void EveryStepGetsAnEntry()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("a", null, ""), new Step("b", "a", ""), new Step("c", null, "x")]);

        Assert.Equal(["a", "b", "c"], icons.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void NoStepsGiveNoIcons() =>
        Assert.Empty(TimbnQuestIcons.Inherit([]));
}
