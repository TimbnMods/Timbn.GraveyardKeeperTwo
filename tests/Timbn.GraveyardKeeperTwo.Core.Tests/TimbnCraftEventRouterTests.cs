namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnCraftEventRouterTests
{
    [Fact]
    public void AHandlerHearsOnlyItsOwnCraft()
    {
        TimbnCraftEventRouter<string> router = new();
        List<string> heard = [];
        router.Add("timbn_teach", heard.Add);

        foreach (var handler in router.HandlersFor("timbn_teach"))
            handler("taught");

        Assert.Equal(["taught"], heard);
        Assert.Empty(router.HandlersFor("wooden_plank"));
    }

    [Fact]
    public void HandlersForOneCraftComeOutInTheOrderTheyWereAdded()
    {
        TimbnCraftEventRouter<int> router = new();
        List<string> order = [];
        router.Add("craft", _ => order.Add("first"));
        router.Add("craft", _ => order.Add("second"));

        foreach (var handler in router.HandlersFor("craft"))
            handler(0);

        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public void DisposingAHandlerTakesOnlyThatHandlerOut()
    {
        TimbnCraftEventRouter<int> router = new();
        var removed = router.Add("craft", _ => { });
        router.Add("craft", _ => { });

        removed.Dispose();

        Assert.Single(router.HandlersFor("craft"));
        Assert.False(router.IsEmpty);
    }

    [Fact]
    public void TheRouterIsEmptyAgainOnceEveryHandlerIsGone()
    {
        TimbnCraftEventRouter<int> router = new();
        var only = router.Add("craft", _ => { });
        Assert.False(router.IsEmpty);

        only.Dispose();
        only.Dispose();

        Assert.True(router.IsEmpty);
        Assert.Empty(router.HandlersFor("craft"));
    }

    [Fact]
    public void AHandlerRemovedWhileAnEventIsBeingSentStillHearsIt()
    {
        TimbnCraftEventRouter<int> router = new();
        var heard = 0;
        IDisposable? first = null;
        first = router.Add("craft", _ =>
        {
            heard++;
            first!.Dispose();
        });
        router.Add("craft", _ => heard++);

        foreach (var handler in router.HandlersFor("craft"))
            handler(0);

        Assert.Equal(2, heard);
        Assert.Single(router.HandlersFor("craft"));
    }
}
