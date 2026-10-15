namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnPauseCountsTests
{
    private readonly List<string> _log = [];
    private bool _walking = true;

    private TimbnPauseCounts<string> NewPauses() => new(
        key =>
        {
            _log.Add($"pause {key}");
            return _walking;
        },
        key => _log.Add($"resume {key}"));

    [Fact]
    public void WalkResumesOnlyAfterTheLastHold()
    {
        var pauses = NewPauses();

        var talk = pauses.Hold("larry");
        var scene = pauses.Hold("larry");
        talk.Dispose();

        Assert.Equal(["pause larry"], _log);
        Assert.True(pauses.IsHeld("larry"));
        scene.Dispose();
        Assert.Equal(["pause larry", "resume larry"], _log);
        Assert.False(pauses.IsHeld("larry"));
    }

    [Fact]
    public void ReleasingTwiceCountsOnce()
    {
        var pauses = NewPauses();

        var talk = pauses.Hold("larry");
        var scene = pauses.Hold("larry");
        talk.Dispose();
        talk.Dispose();

        Assert.True(pauses.IsHeld("larry"));
        scene.Dispose();
        Assert.Equal("resume larry", _log[^1]);
    }

    [Fact]
    public void NothingToPauseResumesNothing()
    {
        _walking = false;
        var pauses = NewPauses();

        pauses.Hold("larry").Dispose();

        Assert.Equal(["pause larry"], _log);
    }

    [Fact]
    public void LaterHoldPausesAWalkStartedAfterTheFirst()
    {
        _walking = false;
        var pauses = NewPauses();

        var talk = pauses.Hold("larry");
        _walking = true;
        var scene = pauses.Hold("larry");
        talk.Dispose();
        scene.Dispose();

        Assert.Equal(["pause larry", "pause larry", "resume larry"], _log);
    }

    [Fact]
    public void DroppedHoldsNeverResume()
    {
        var pauses = NewPauses();

        var talk = pauses.Hold("larry");

        Assert.True(pauses.Drop("larry"));
        talk.Dispose();
        Assert.Equal(["pause larry"], _log);
        Assert.False(pauses.IsHeld("larry"));
    }

    [Fact]
    public void HoldAfterDropStartsAgain()
    {
        var pauses = NewPauses();

        var before = pauses.Hold("larry");
        pauses.Drop("larry");
        var after = pauses.Hold("larry");
        before.Dispose();

        Assert.True(pauses.IsHeld("larry"));
        after.Dispose();
        Assert.Equal(["pause larry", "pause larry", "resume larry"], _log);
    }

    [Fact]
    public void PeopleArePausedApart()
    {
        var pauses = NewPauses();

        var larry = pauses.Hold("larry");
        pauses.Hold("herm").Dispose();

        Assert.Equal(["pause larry", "pause herm", "resume herm"], _log);
        larry.Dispose();
        Assert.Equal("resume larry", _log[^1]);
    }
}
