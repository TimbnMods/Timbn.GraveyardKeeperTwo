namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps what mods have registered with Core by mod guid. Each registration is undone by the handle it returns. The mod
/// keeps that handle in its bag, so nothing Core holds outlives a mod that unloads.
/// </summary>
internal sealed class TimbnPluginRegistry<T>
    where T : class
{
    private readonly List<Entry> _entries = [];

    internal IDisposable Add(string pluginGuid, T item)
    {
        var entry = new Entry(pluginGuid, item);
        _entries.Add(entry);
        return new TimbnUndo(() => _entries.Remove(entry));
    }

    /// <summary>Lists everything the mod registered, in the order it did.</summary>
    internal IEnumerable<T> For(string pluginGuid) => _entries.Where(entry => entry.PluginGuid == pluginGuid).Select(entry => entry.Item);

    /// <summary>Lists everything every mod registered, in the order it was added.</summary>
    internal IEnumerable<T> All => _entries.Select(entry => entry.Item);

    /// <summary>Returns the last thing the mod registered or null when it registered nothing.</summary>
    internal T? Latest(string pluginGuid) => For(pluginGuid).LastOrDefault();

    internal void Clear() => _entries.Clear();

    private sealed class Entry
    {
        public Entry(string pluginGuid, T item)
        {
            PluginGuid = pluginGuid;
            Item = item;
        }

        public string PluginGuid { get; }

        public T Item { get; }
    }
}
