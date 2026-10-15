namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnPauseCounts<TKey>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Entry> _entries = [];
    private readonly Func<TKey, bool> _pause;
    private readonly Action<TKey> _resume;

    internal TimbnPauseCounts(Func<TKey, bool> pause, Action<TKey> resume)
    {
        _pause = pause;
        _resume = resume;
    }

    internal IEnumerable<TKey> Keys => _entries.Keys;

    internal bool IsHeld(TKey key) => _entries.ContainsKey(key);

    internal IDisposable Hold(TKey key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            _entries[key] = entry = new Entry();

        entry.Count++;
        if (!entry.Paused)
            entry.Paused = _pause(key);

        return new TimbnUndo(() => Release(key, entry));
    }

    internal bool Drop(TKey key) => _entries.Remove(key, out var entry) && entry.Paused;

    internal void Clear() => _entries.Clear();

    private void Release(TKey key, Entry entry)
    {
        if (!_entries.TryGetValue(key, out var current) || current != entry || --entry.Count > 0)
            return;

        _entries.Remove(key);
        if (entry.Paused)
            _resume(key);
    }

    private sealed class Entry
    {
        public int Count { get; set; }

        public bool Paused { get; set; }
    }
}
