namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnGui
{
    private static readonly List<Entry> _entries = [];

    internal static IDisposable Add(Action draw, Func<bool> isRunning, ManualLogSource logger)
    {
        var entry = new Entry(draw, isRunning, logger);
        _entries.Add(entry);
        return new TimbnUndo(() => _entries.Remove(entry));
    }

    internal static void Draw()
    {
        if (_entries.Count == 0)
            return;

        foreach (var entry in _entries.ToList())
        {
            if (!entry.IsRunning())
                continue;

            try
            {
                entry.Draw();
            }
            catch (Exception ex)
            {
                _entries.Remove(entry);
                entry.Logger.LogError($"{nameof(TimbnGui)}|A GUI handler threw and was stopped: {ex}");
            }
        }
    }

    private sealed class Entry(Action draw, Func<bool> isRunning, ManualLogSource logger)
    {
        public Action Draw { get; } = draw;

        public Func<bool> IsRunning { get; } = isRunning;

        public ManualLogSource Logger { get; } = logger;
    }
}
