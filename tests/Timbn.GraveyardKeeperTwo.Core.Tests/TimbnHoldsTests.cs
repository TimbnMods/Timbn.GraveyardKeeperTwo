namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnHoldsTests
{
    private readonly List<string> _log = [];

    private TimbnHolds<float> NewHolds() => new(value => _log.Add($"apply {value}"), () => _log.Add("release"));

    [Fact]
    public void FirstHoldAppliesItsValue()
    {
        var holds = NewHolds();

        holds.Take(2f);

        Assert.Equal(["apply 2"], _log);
        Assert.True(holds.IsHeld);
    }

    [Fact]
    public void NewestHoldWins()
    {
        var holds = NewHolds();

        var first = holds.Take(2f);
        var second = holds.Take(3f);
        second.Dispose();

        Assert.Equal(["apply 2", "apply 3", "apply 2"], _log);
        first.Dispose();
        Assert.Equal("release", _log[^1]);
        Assert.False(holds.IsHeld);
    }

    [Fact]
    public void ReleasingAnOlderHoldChangesNothing()
    {
        var holds = NewHolds();

        var first = holds.Take(2f);
        holds.Take(3f);
        first.Dispose();

        Assert.Equal(["apply 2", "apply 3"], _log);
        Assert.True(holds.IsHeld);
    }

    [Fact]
    public void SameValueTwiceAppliesOnce()
    {
        var holds = NewHolds();

        holds.Take(2f);
        holds.Take(2f);

        Assert.Equal(["apply 2"], _log);
    }

    [Fact]
    public void ReleaseAllReleasesOnceAndDeadensHandles()
    {
        var holds = NewHolds();
        var hold = holds.Take(2f);

        holds.ReleaseAll();
        hold.Dispose();
        holds.ReleaseAll();

        Assert.Equal(["apply 2", "release"], _log);
        Assert.False(holds.IsHeld);
    }

    [Fact]
    public void DisposingTwiceReleasesOnce()
    {
        var holds = NewHolds();
        var hold = holds.Take(2f);

        hold.Dispose();
        hold.Dispose();

        Assert.Equal(["apply 2", "release"], _log);
    }
}
