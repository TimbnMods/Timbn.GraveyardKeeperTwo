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
    public void AQuestOnItsOwnOrInALoopOrOutsideTheListWithNoIconStaysEmpty()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("alone", null, ""), new Step("a", "b", ""), new Step("b", "a", ""), new Step("pay", "58_port_supplies_order", "")]);

        Assert.Equal("", icons["alone"]);
        Assert.Equal("", icons["a"]);
        Assert.Equal("", icons["pay"]);
    }

    [Fact]
    public void ALoopWithOneIconHandsItAround()
    {
        var icons = TimbnQuestIcons.Inherit([new Step("a", "c", ""), new Step("b", "a", ""), new Step("c", "b", "herm")]);

        Assert.Equal("herm", icons["a"]);
        Assert.Equal("herm", icons["b"]);
        Assert.Equal("herm", icons["c"]);
    }
}
