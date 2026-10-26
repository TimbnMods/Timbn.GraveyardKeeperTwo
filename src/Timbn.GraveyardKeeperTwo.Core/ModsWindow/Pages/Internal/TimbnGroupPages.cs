namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The pages mods have added to a group's page in the Mods window, by the group's key.</summary>
internal static class TimbnGroupPages
{
    private static readonly TimbnPluginRegistry<Entry> _registry = new();

    internal static IDisposable Add(string pluginGuid, string group, string title, Action<TimbnPageBuilder> build, ManualLogSource logger) =>
        _registry.Add(pluginGuid, new Entry(group, new TimbnModsPageRegistration(title, build, logger)));

    /// <summary>The pages added to the group, in the order they were added.</summary>
    internal static IEnumerable<TimbnModsPageRegistration> For(string group) =>
        _registry.All.Where(entry => entry.Group == group).Select(entry => entry.Page);

    internal static void Clear() => _registry.Clear();

    private sealed class Entry(string group, TimbnModsPageRegistration page)
    {
        public string Group { get; } = group;

        public TimbnModsPageRegistration Page { get; } = page;
    }
}
