namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>What each mod told the Mods window about itself, by plugin guid. The latest registration of a mod wins.</summary>
internal static class TimbnModInfos
{
    private static readonly TimbnPluginRegistry<TimbnModInfo> _registry = new();

    internal static IDisposable Add(string pluginGuid, TimbnModInfo info) => _registry.Add(pluginGuid, info);

    internal static TimbnModInfo? For(string pluginGuid) => _registry.Latest(pluginGuid);

    internal static void Clear() => _registry.Clear();

    /// <summary>The group the mod is listed in, or null when it has none or asked to stay out of it.</summary>
    internal static TimbnModGroup? GroupOf(string pluginGuid) =>
        For(pluginGuid) is { ShowInGroup: true, ModGroupName: { } name } info && !string.IsNullOrWhiteSpace(name)
            ? TimbnModGroup.Of(name.Trim(), info.ModGroupId)
            : null;
}

/// <summary>
/// The group a mod is in. Mods match on the key, which is the group's id when the mod gave one and its name when it did
/// not, so a group with an id never takes in a mod that only shares its name.
/// </summary>
internal sealed class TimbnModGroup
{
    private TimbnModGroup(string key, string name)
    {
        Key = key;
        Name = name;
    }

    public string Key { get; }

    public string Name { get; }

    // The prefixes keep an id that reads like some group's name from matching it.
    internal static TimbnModGroup Of(string name, string? id) =>
        new(string.IsNullOrWhiteSpace(id) ? $"name:{name}" : $"id:{id!.Trim().ToLowerInvariant()}", name);
}
