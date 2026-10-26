namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnCraftQueueStripTests
{
    private static bool IsMod(string id) => id.StartsWith("mod", StringComparison.Ordinal);

    [Fact]
    public void AModCraftAfterTheCurrentOneLeavesTheIndexAlone()
    {
        var plan = TimbnCraftQueueStrip.Plan(["flour", "mod_pie", "bread"], 0, CraftComponentStatus.Started, IsMod);

        Assert.Equal([1], plan.Removed);
        Assert.Equal(0, plan.CurrentIndex);
        Assert.Equal(CraftComponentStatus.Started, plan.Status);
        Assert.False(plan.CurrentRemoved);
    }

    [Fact]
    public void ModCraftsBeforeTheCurrentOneMoveTheIndexUp()
    {
        var plan = TimbnCraftQueueStrip.Plan(["mod_pie", "mod_cake", "flour", "bread"], 3, CraftComponentStatus.QueueDelayed, IsMod);

        Assert.Equal([0, 1], plan.Removed);
        Assert.Equal(1, plan.CurrentIndex);
        Assert.Equal(CraftComponentStatus.QueueDelayed, plan.Status);
    }

    [Fact]
    public void TakingOutTheCurrentCraftWaitsAtTheNextOne()
    {
        var plan = TimbnCraftQueueStrip.Plan(["flour", "mod_pie", "bread"], 1, CraftComponentStatus.Started, IsMod);

        Assert.Equal([1], plan.Removed);
        Assert.Equal(1, plan.CurrentIndex);
        Assert.Equal(CraftComponentStatus.ReadyToStartCraft, plan.Status);
        Assert.True(plan.CurrentRemoved);
    }

    [Fact]
    public void TakingOutTheLastCurrentCraftClampsToTheEnd()
    {
        var plan = TimbnCraftQueueStrip.Plan(["flour", "bread", "mod_pie"], 2, CraftComponentStatus.Started, IsMod);

        Assert.Equal(1, plan.CurrentIndex);
        Assert.Equal(CraftComponentStatus.ReadyToStartCraft, plan.Status);
        Assert.True(plan.CurrentRemoved);
    }

    [Fact]
    public void EmptyingTheQueueEndsItTheWayTheGameDoes()
    {
        var plan = TimbnCraftQueueStrip.Plan(["mod_pie", "mod_cake"], 0, CraftComponentStatus.Started, IsMod);

        Assert.Equal([0, 1], plan.Removed);
        Assert.Equal(-1, plan.CurrentIndex);
        Assert.Equal(CraftComponentStatus.Finished, plan.Status);
        Assert.True(plan.CurrentRemoved);
        Assert.True(plan.Emptied);
    }
}
