namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnTalkWatchTests
{
    [Fact]
    public void LastsWhileTheGameHoldsThePlayer()
    {
        var watch = new TimbnTalkWatch();

        for (var i = 0; i < TimbnTalkWatch.GraceFrames * 3; i++)
            Assert.False(watch.Ended(gameHasPlayer: true));
    }

    [Fact]
    public void EndsOnTheFirstFrameTheGameLetsGo()
    {
        var watch = new TimbnTalkWatch();

        watch.Ended(gameHasPlayer: true);

        Assert.True(watch.Ended(gameHasPlayer: false));
    }

    [Fact]
    public void WaitsAFewFramesForTheGameToTakeThePlayer()
    {
        var watch = new TimbnTalkWatch();

        for (var i = 1; i < TimbnTalkWatch.GraceFrames; i++)
            Assert.False(watch.Ended(gameHasPlayer: false));

        Assert.False(watch.Ended(gameHasPlayer: true));
        Assert.True(watch.Ended(gameHasPlayer: false));
    }

    [Fact]
    public void EndsWhenTheGameNeverTakesThePlayer()
    {
        var watch = new TimbnTalkWatch();

        for (var i = 1; i < TimbnTalkWatch.GraceFrames; i++)
            Assert.False(watch.Ended(gameHasPlayer: false));

        Assert.True(watch.Ended(gameHasPlayer: false));
    }

    [Fact]
    public void RestartGivesTheGraceAgain()
    {
        var watch = new TimbnTalkWatch();
        watch.Ended(gameHasPlayer: true);

        watch.Restart();

        Assert.False(watch.Ended(gameHasPlayer: false));
    }
}
