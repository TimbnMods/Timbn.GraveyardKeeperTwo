namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnGameEventsTests
{
    private event Action? Plain;

    private event Action<int>? OneArgument;

    private event Action<string, int>? TwoArguments;

    [Fact]
    public void ThePlainHandlerIsWrappedAndRemovedOnDispose()
    {
        var calls = 0;
        var handle = TimbnGameEvents.On(() => calls++, h => Plain += h, h => Plain -= h);

        Plain?.Invoke();
        Assert.Equal(1, calls);

        handle.Dispose();
        Assert.Null(Plain);
    }

    [Fact]
    public void TheOneArgumentHandlerGetsItsArgument()
    {
        List<int> seen = [];
        var handle = TimbnGameEvents.On<int>(value => seen.Add(value), h => OneArgument += h, h => OneArgument -= h);

        OneArgument?.Invoke(7);
        OneArgument?.Invoke(9);
        Assert.Equal([7, 9], seen);

        handle.Dispose();
        Assert.Null(OneArgument);
    }

    [Fact]
    public void TheTwoArgumentHandlerGetsBothArguments()
    {
        (string, int)? seen = null;
        var handle = TimbnGameEvents.On<string, int>((first, second) => seen = (first, second), h => TwoArguments += h, h => TwoArguments -= h);

        TwoArguments?.Invoke("day", 3);
        Assert.Equal(("day", 3), seen);

        handle.Dispose();
        Assert.Null(TwoArguments);
    }

    [Fact]
    public void TwoHandlersOnOneEventComeOffOneAtATime()
    {
        var first = 0;
        var second = 0;
        var firstHandle = TimbnGameEvents.On(() => first++, h => Plain += h, h => Plain -= h);
        var secondHandle = TimbnGameEvents.On(() => second++, h => Plain += h, h => Plain -= h);

        firstHandle.Dispose();
        Plain?.Invoke();

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        secondHandle.Dispose();
        Assert.Null(Plain);
    }

    [Fact]
    public void DisposingTwiceRemovesOnlyOnce()
    {
        var calls = 0;
        var handle = TimbnGameEvents.On(() => calls++, h => Plain += h, h => Plain -= h);
        Plain += () => calls += 10;

        handle.Dispose();
        handle.Dispose();
        Plain?.Invoke();

        Assert.Equal(10, calls);
    }
}
