namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps what a page's callbacks decide, such as whether a button is enabled or a card is shown, up to date. It sits on
/// the page's top box, which stays active however a card inside it is hidden, and goes with the page when its area is
/// destroyed. A callback that throws is logged once and dropped.
/// </summary>
internal sealed class TimbnPageWatcher : MonoBehaviour
{
    private readonly List<Entry> _entries = [];
    private int _generation;

    internal List<TimbnWidget> Widgets { get; } = [];

    internal List<TimbnPageAction> Actions { get; } = [];

    internal ManualLogSource Logger { get; set; } = null!;

    // Forgets everything the page built, for a region that builds its content again.
    internal void Reset()
    {
        _generation++;
        _entries.Clear();
        Widgets.Clear();
        Actions.Clear();
    }

    internal void Watch(Func<bool> read, Action<bool> apply) => WatchValue(read, apply);

    // Calls apply with the value whenever read gives a different one.
    internal void WatchValue<T>(Func<T> read, Action<T> apply)
    {
        var entry = new Entry(() => read()!, value => apply((T)value));
        _entries.Add(entry);
        Check(entry);
    }

    // A callback may rebuild the region this watcher sits on, which clears the list, so the walk stops there.
    private void Update()
    {
        var generation = _generation;
        for (var index = _entries.Count - 1; index >= 0 && generation == _generation; index--)
        {
            if (index < _entries.Count)
                Check(_entries[index]);
        }
    }

    // A plain try and catch rather than TimbnSafe.Run, which would allocate a delegate for every entry every frame, and a
    // page has one entry per binding of every control on it.
    private void Check(Entry entry)
    {
        try
        {
            var value = entry.Read();
            if (entry.Seen && Equals(entry.Last, value))
                return;

            entry.Seen = true;

            entry.Last = value;
            entry.Apply(value);
        }
        catch (Exception exception)
        {
            Logger.LogError($"{nameof(TimbnPageBuilder)}|{TimbnSafe.Describe(entry.Apply)} threw and its binding was dropped. {exception}");
            _entries.Remove(entry);
        }
    }

    private sealed class Entry(Func<object> read, Action<object> apply)
    {
        public Func<object> Read { get; } = read;

        public Action<object> Apply { get; } = apply;

        public object? Last { get; set; }

        public bool Seen { get; set; }
    }
}
