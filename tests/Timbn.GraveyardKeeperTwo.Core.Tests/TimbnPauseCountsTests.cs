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
}
