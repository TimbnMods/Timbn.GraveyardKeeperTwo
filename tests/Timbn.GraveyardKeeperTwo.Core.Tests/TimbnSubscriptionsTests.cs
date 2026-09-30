using BepInEx.Logging;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSubscriptionsTests
{
    private readonly ManualLogSource _logger = new("Tests");

    [Fact]
    public void DisposeRunsUndosNewestFirst()
    {
        List<int> order = [];
        var bag = new TimbnSubscriptions(_logger);
        bag.Add(() => order.Add(1));
        bag.Add(() => order.Add(2));
        bag.Add(() => order.Add(3));

        bag.Dispose();

        Assert.Equal([3, 2, 1], order);
    }

    [Fact]
    public void DisposingAHandleEarlyRunsItOnceAndRemovesIt()
    {
        var runs = 0;
        var bag = new TimbnSubscriptions(_logger);
        var handle = bag.Add(() => runs++);

        handle.Dispose();
        handle.Dispose();
        bag.Dispose();

        Assert.Equal(1, runs);
    }

    [Fact]
    public void AThrowingUndoDoesNotStopTheRest()
    {
        var ran = false;
        var errors = 0;
        _logger.LogEvent += (_, args) => errors += args.Level == LogLevel.Error ? 1 : 0;
        var bag = new TimbnSubscriptions(_logger);
        bag.Add(() => ran = true);
        bag.Add(() => throw new InvalidOperationException("boom"));

        bag.Dispose();

        Assert.True(ran);
        Assert.Equal(1, errors);
    }

    [Fact]
    public void UndoRunsOnce()
    {
        var runs = 0;
        var undo = new TimbnUndo(() => runs++);

        undo.Dispose();
        undo.Dispose();

        Assert.Equal(1, runs);
    }

    [Fact]
    public void SharedEmptyUndoIsHarmless()
    {
        TimbnUndo.None.Dispose();
        TimbnUndo.None.Dispose();
    }

    [Fact]
    public void SafeRunReportsAThrow()
    {
        var messages = new List<string>();
        _logger.LogEvent += (_, args) => messages.Add(args.Data?.ToString() ?? "");

        var ok = TimbnSafe.Run(() => throw new InvalidOperationException("boom"), _logger, "Something");
        var fine = TimbnSafe.Run(() => { }, _logger, "Something");

        Assert.False(ok);
        Assert.True(fine);
        Assert.Single(messages);
        Assert.StartsWith("Something threw:", messages[0]);
    }
}
