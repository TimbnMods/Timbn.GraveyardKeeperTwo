namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Helpers for a plugin's config file, for binding a slider or a hidden setting in one line and carrying a
/// player's values over when a setting is renamed or moved.
/// </summary>
public static class TimbnConfig
{
    /// <summary>
    /// Moves the values of config entries a plugin has renamed or moved into their new entries, so players keep
    /// their settings across the change. Call it at the end of <c>BindConfig</c>, once every new entry is bound.
    /// An old entry is only used while it is still in the file, and it is removed from the file afterwards.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnConfig.CarryOver(
    ///     config,
    ///     (StuckCarriers, "Carriers", "ShowWhenStuck"),
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
    /// Binds a setting that only accepts values between a lowest and a highest, which the Mods window shows as a
    /// slider. It is the same as ConfigFile.Bind with an AcceptableValueRange, in one line.
    /// </summary>
    /// <example>
    /// <code>
    /// TimeSpeed = config.BindRange("Meditation", "TimeSpeed", 7f, "How many times faster time runs while you meditate.", 1f, 50f);
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
    public static ConfigEntry<T> BindRange<T>(this ConfigFile config, string section, string key, T defaultValue, string description, T min, T max)
        where T : IComparable =>
        config.Bind(section, key, defaultValue, new ConfigDescription(description, new AcceptableValueRange<T>(min, max)));

    /// <summary>
    /// Binds a setting that stays out of the Mods window, for a switch only a developer needs, such as
    /// turning on a plugin's debug log. It is still written to the config file and read like any other entry.
    /// </summary>
    /// <example>
    /// <code>
    /// WriteDebugLog = config.BindHidden("Debug", "WriteDebugLog", false, "Writes the debug log.");
    /// </code>
    /// </example>
    /// <param name="config">The plugin's config file.</param>
    /// <param name="section">The section it goes under.</param>
    /// <param name="key">The setting's name.</param>
    /// <param name="defaultValue">The value until it is changed.</param>
    /// <param name="description">What the setting does.</param>
    /// <returns>The bound setting.</returns>
    public static ConfigEntry<T> BindHidden<T>(this ConfigFile config, string section, string key, T defaultValue, string description) =>
        config.Bind(section, key, defaultValue, new ConfigDescription(description, null, new TimbnSettingTags { Browsable = false }));
}
