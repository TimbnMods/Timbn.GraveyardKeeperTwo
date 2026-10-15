namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSettleWindowTests
{
    private readonly TimbnSettleWindow<string> _window = new(3);

    [Fact]
    public void NobodySettlesUntilStarted() => Assert.False(_window.IsSettling("herm"));

    [Fact]
    public void SettlingEndsAfterTheTicksStandingStill()
    {
        _window.Start("herm");

        _window.Tick(_ => false);
        _window.Tick(_ => false);
        Assert.True(_window.IsSettling("herm"));

        _window.Tick(_ => false);
        Assert.False(_window.IsSettling("herm"));
    }

    [Fact]
    public void MovingStartsTheTicksOver()
    {
        _window.Start("herm");
        _window.Tick(_ => false);
        _window.Tick(_ => false);

        _window.Tick(_ => true);
        _window.Tick(_ => false);
        _window.Tick(_ => false);

        Assert.True(_window.IsSettling("herm"));
    }

    [Fact]
    public void EachPersonCountsOnTheirOwn()
    {
        _window.Start("herm");
        _window.Start("linda");

        _window.Tick(name => name == "linda");
        _window.Tick(name => name == "linda");
        _window.Tick(name => name == "linda");

        Assert.False(_window.IsSettling("herm"));
        Assert.True(_window.IsSettling("linda"));
    }

    [Fact]
    public void ClearEndsEveryWindow()
    {
        _window.Start("herm");

        _window.Clear();

        Assert.False(_window.IsSettling("herm"));
    }
}
