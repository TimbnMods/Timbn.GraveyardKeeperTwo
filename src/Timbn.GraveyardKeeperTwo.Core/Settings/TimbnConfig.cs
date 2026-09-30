using BepInEx.Bootstrap;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Re-reads the config files of loaded plugins, so a setting edited by hand takes effect without a restart. Each
/// changed value goes through the entry's normal setter, which raises its SettingChanged event, so anything tied
/// to it with <see cref="TimbnPluginSettings.Toggle(ConfigEntry{bool}, Action, Action)"/> applies or reverts on its
/// own. Call these on the main thread, since the change handlers touch the game.
/// </summary>
public static class TimbnConfig
{
    /// <summary>
    /// Reloads the config file of every loaded plugin, Timbn or not. A failure in one plugin is logged and does
    /// not stop the rest. A key removed from a file keeps its current value instead of returning to its default.
    /// </summary>
    /// <example>
    /// <code>
    /// var reloaded = TimbnConfig.ReloadAll();
    /// </code>
    /// </example>
    /// <returns>How many plugins were reloaded.</returns>
    public static int ReloadAll()
    {
        var reloaded = 0;
        var plugins = Chainloader.PluginInfos.Values
            .Select(info => info.Instance)
            .Where(instance => instance != null)
            .Distinct()
            .ToList();

        foreach (var plugin in plugins)
        {
            if (Reload(plugin))
                reloaded++;
        }

        return reloaded;
    }

    /// <summary>
    /// Moves the values of config entries a plugin has renamed or moved into their new entries, so players keep
    /// their settings across the change. Call it at the end of <c>BindConfig</c>, once every new entry is bound.
    /// An old entry is only used while it is still in the file, and it is removed from the file afterwards.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnConfig.CarryOver(
    ///     config,
    ///     (PerkTalentBonus, "GreenThumb", "TalentBonus"),
    ///     (UnstuckKey, "Unstuck", "Key"));
    /// </code>
    /// </example>
    /// <param name="config">The plugin's config file.</param>
    /// <param name="moves">Each new entry with the section and key it used to have.</param>
    public static void CarryOver(ConfigFile config, params (ConfigEntryBase Entry, string Section, string Key)[] moves)
    {
        if (AccessTools.Property(typeof(ConfigFile), "OrphanedEntries")?.GetValue(config) is not Dictionary<ConfigDefinition, string> orphaned)
            return;

        var carried = 0;
        foreach (var (entry, section, key) in moves)
        {
            var old = new ConfigDefinition(section, key);
            if (!orphaned.TryGetValue(old, out var value))
                continue;

            entry.SetSerializedValue(value);
            orphaned.Remove(old);
            carried++;
        }

        if (carried > 0)
            config.Save();
    }

    /// <summary>
    /// Binds a setting that only accepts values between a lowest and a highest, which a config manager shows as a
    /// slider. It is the same as ConfigFile.Bind with an AcceptableValueRange, in one line.
    /// </summary>
    /// <example>
    /// <code>
    /// TimeSpeed = config.Bind("Meditation", "TimeSpeed", 7f, "How many times faster time runs while you meditate.", 1f, 50f);
    /// </code>
    /// </example>
    /// <param name="config">The plugin's config file.</param>
    /// <param name="section">The section it goes under.</param>
    /// <param name="key">The setting's name.</param>
    /// <param name="defaultValue">The value until the player changes it.</param>
    /// <param name="description">What the setting does, shown to players.</param>
    /// <param name="min">The lowest value allowed.</param>
    /// <param name="max">The highest value allowed.</param>
    /// <returns>The bound setting.</returns>
    public static ConfigEntry<T> Bind<T>(this ConfigFile config, string section, string key, T defaultValue, string description, T min, T max)
        where T : IComparable =>
        config.Bind(section, key, defaultValue, new ConfigDescription(description, new AcceptableValueRange<T>(min, max)));

    /// <summary>Reloads the config file of one plugin, as <see cref="ReloadAll"/> does for every plugin.</summary>
    /// <param name="plugin">The plugin whose config file to re-read.</param>
    /// <returns>True when the file was read, false when it does not exist yet or could not be read.</returns>
    public static bool Reload(BaseUnityPlugin plugin)
    {
        if (!File.Exists(plugin.Config.ConfigFilePath))
            return false;

        try
        {
            plugin.Config.Reload();
            return true;
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnConfig)}|Could not reload {plugin.Config.ConfigFilePath}: {ex}");
            return false;
        }
    }
}
