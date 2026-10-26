namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The pages mods have added to the Mods window, and which mods replaced their default page.</summary>
internal static class TimbnSettingsPages
{
    private static readonly TimbnPluginRegistry<Entry> _registry = new();

    internal static IDisposable Add(string pluginGuid, string title, Action<TimbnPageBuilder> build, bool replaceDefault, ManualLogSource logger) =>
        _registry.Add(pluginGuid, new Entry(new TimbnModsPageRegistration(title, build, logger), replaceDefault));

    /// <summary>Whether the mod asked for its own pages to replace the settings page Core makes from its config.</summary>
    internal static bool ReplacesDefault(string pluginGuid) => _registry.For(pluginGuid).Any(entry => entry.ReplaceDefault);

    internal static void Clear() => _registry.Clear();

    internal static IEnumerable<TimbnModsPageRegistration> For(string pluginGuid) => _registry.For(pluginGuid).Select(entry => entry.Page);

    private sealed class Entry(TimbnModsPageRegistration page, bool replaceDefault)
    {
        public TimbnModsPageRegistration Page { get; } = page;

        public bool ReplaceDefault { get; } = replaceDefault;
    }
}
