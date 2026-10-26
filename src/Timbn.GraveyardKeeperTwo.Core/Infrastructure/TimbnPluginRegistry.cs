namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// What plugins have registered with Core, kept by plugin guid. Each registration is undone by the handle it returns, which
/// the plugin keeps in its own bag, so nothing Core holds outlives a plugin that unloads.
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

    /// <summary>Everything the plugin registered, in the order it did.</summary>
    internal IEnumerable<T> For(string pluginGuid) => _entries.Where(entry => entry.PluginGuid == pluginGuid).Select(entry => entry.Item);

    /// <summary>Everything every plugin registered, in the order it was added.</summary>
    internal IEnumerable<T> All => _entries.Select(entry => entry.Item);

    /// <summary>The last thing the plugin registered, or null when it registered nothing.</summary>
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
