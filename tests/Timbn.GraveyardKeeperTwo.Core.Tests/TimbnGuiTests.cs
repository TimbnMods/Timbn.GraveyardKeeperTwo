using BepInEx.Logging;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnGuiTests
{
    private readonly ManualLogSource _logger = new("Tests");
    private readonly List<LogEventArgs> _logged = [];

    public TimbnGuiTests() => _logger.LogEvent += (_, args) => _logged.Add(args);

    [Fact]
    public void ARunningEntryDrawsEachFrame()
    {
        var draws = 0;
        using var handle = TimbnGui.Add(() => draws++, () => true, _logger);

        TimbnGui.Draw();
        TimbnGui.Draw();

        Assert.Equal(2, draws);
    }

    [Fact]
    public void AnEntryThatIsNotRunningIsSkipped()
    {
        var draws = 0;
        var running = false;
        using var handle = TimbnGui.Add(() => draws++, () => running, _logger);

        TimbnGui.Draw();
        running = true;
        TimbnGui.Draw();

        Assert.Equal(1, draws);
    }

    [Fact]
    public void ADisposedEntryNoLongerDraws()
    {
        var draws = 0;
        var handle = TimbnGui.Add(() => draws++, () => true, _logger);

        handle.Dispose();
        TimbnGui.Draw();

        Assert.Equal(0, draws);
    }

    [Fact]
    public void AnEntryThatThrowsIsLoggedAndDropped()
    {
        var draws = 0;
        using var handle = TimbnGui.Add(() => { draws++; throw new InvalidOperationException("bad draw"); }, () => true, _logger);

        TimbnGui.Draw();
        TimbnGui.Draw();

        Assert.Equal(1, draws);
        var entry = Assert.Single(_logged);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("bad draw", entry.Data.ToString());
    }

    [Fact]
    public void OtherEntriesStillDrawWhenOneThrows()
    {
        var good = 0;
        using var bad = TimbnGui.Add(() => throw new InvalidOperationException(), () => true, _logger);
        using var fine = TimbnGui.Add(() => good++, () => true, _logger);

        TimbnGui.Draw();

        Assert.Equal(1, good);
    }
}
