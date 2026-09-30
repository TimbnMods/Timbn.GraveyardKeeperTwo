namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps a plugin's changes to the game in step with its config, so a setting changed in a config menu applies
/// straight away instead of on the next start. A setting whose feature is broken, because a game update broke
/// its patches (see <see cref="TimbnFeatureAttribute"/>), reads as off here whatever its value. Reach it through
/// the plugin's Settings property.
/// </summary>
public sealed class TimbnPluginSettings
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginSettings(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Ties a toggle to the change it makes. While the toggle is on, apply runs on every save load and as soon as
    /// the toggle turns on during play. Revert runs as soon as the toggle turns off and when the plugin unloads.
    /// Both run straight away if a save is already loaded. At the main menu, revert only runs for a change that
    /// was applied.
    /// </summary>
    /// <example>
    /// <code>
    /// Settings.Toggle(PluginConfig.GreenThumbTalentBonus, _greenThumb.Apply, _greenThumb.Revert);
    /// Settings.Toggle(PluginConfig.StudyTableNoStuckCrafts, StudyTableStuckCraft.ClearStuckCrafts);
    /// </code>
    /// </example>
    /// <param name="entry">The toggle.</param>
    /// <param name="apply">Makes the change. It can run more than once, so it must skip work already done.</param>
    /// <param name="revert">
    /// Undoes the change. It can run when nothing was applied, so it must cope with that. Leave it out for a fix
    /// that runs once per save and has nothing to undo.
    /// </param>
    /// <returns>A handle that stops following the toggle and reverts when disposed. You don't need to keep it.</returns>
    public IDisposable Toggle(ConfigEntry<bool> entry, Action apply, Action? revert = null) =>
        Toggle(entry, on => on, apply, revert);

    /// <summary>
    /// Ties a setting of any type to the change it makes, like <see cref="Toggle(ConfigEntry{bool}, Action, Action)"/>
    /// with isOn deciding which values count as on. Any change to the value reverts first and then applies again if
    /// the new value is on, so apply can read the new value.
    /// </summary>
    /// <example>
    /// <code>
    /// Settings.Toggle(PluginConfig.TechPointCap, cap => cap > 999, _techPointCap.Apply, _techPointCap.Revert);
    /// </code>
    /// </example>
    /// <param name="entry">The setting.</param>
    /// <param name="isOn">Whether a value of the setting turns the change on.</param>
    /// <param name="apply">Makes the change. It can run more than once, so it must skip work already done.</param>
    /// <param name="revert">
    /// Undoes the change. It can run when nothing was applied, so it must cope with that. Can be null.
    /// </param>
    /// <returns>A handle that stops following the setting and reverts when disposed. You don't need to keep it.</returns>
    public IDisposable Toggle<T>(ConfigEntry<T> entry, Func<T, bool> isOn, Action apply, Action? revert = null) =>
        _owner.Subscriptions.Add(TimbnSettings.Toggle(entry, Working(entry, isOn), apply, revert, _owner.PluginLogger));

    /// <summary>
    /// Keeps a registration alive only while a toggle is on. Subscribe runs straight away if the toggle is on and
    /// again each time it turns on, and what it returns is disposed as soon as the toggle turns off. It suits any
    /// Core registration, such as a timer, an event handler, or a balance edit, so a tweak that is switched off
    /// costs nothing and its handlers need no check of the setting.
    /// </summary>
    /// <example>
    /// <code>
    /// Settings.While(PluginConfig.StuckCarriers, () => Events.Every(0.5f, stuckCarriers.Tick));
    /// </code>
    /// </example>
    /// <param name="entry">The toggle.</param>
    /// <param name="subscribe">Makes the registration and returns its handle.</param>
    /// <returns>A handle that stops following the toggle and disposes the registration when disposed. You don't need to keep it.</returns>
    public IDisposable While(ConfigEntry<bool> entry, Func<IDisposable> subscribe) =>
        While(entry, on => on, subscribe);

    /// <summary>
    /// Keeps a registration alive only while a setting of any type is on, like
    /// <see cref="While(ConfigEntry{bool}, Func{IDisposable})"/> with isOn deciding which values count as on. Any
    /// change to the value disposes the registration and makes it again if the new value is on, so subscribe can
    /// read the new value.
    /// </summary>
    /// <example>
    /// <code>
    /// Settings.While(PluginConfig.TechPointCap, cap => cap > 999, () => Balance.Edit&lt;GameResSystemDef&gt;("tech_red", Raise, Lower));
    /// </code>
    /// </example>
    /// <param name="entry">The setting.</param>
    /// <param name="isOn">Whether a value of the setting keeps the registration alive.</param>
    /// <param name="subscribe">Makes the registration and returns its handle.</param>
    /// <returns>A handle that stops following the setting and disposes the registration when disposed. You don't need to keep it.</returns>
    public IDisposable While<T>(ConfigEntry<T> entry, Func<T, bool> isOn, Func<IDisposable> subscribe) =>
        _owner.Subscriptions.Add(TimbnSettings.While(entry, Working(entry, isOn), subscribe, _owner.PluginLogger));

    /// <summary>
    /// Runs your code whenever a setting's value changes, such as from a config manager or a reloaded config file.
    /// The handler is removed when the plugin unloads, and a throw is logged. Prefer Toggle or While when the
    /// setting switches a change on and off.
    /// </summary>
    /// <example>
    /// <code>
    /// Settings.Changed(PluginConfig.Volume, volume => Logger.LogInfo($"Volume is now {volume}"));
    /// </code>
    /// </example>
    /// <param name="entry">The setting.</param>
    /// <param name="handler">Gets the new value.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable Changed<T>(ConfigEntry<T> entry, Action<T> handler) =>
        _owner.Subscriptions.Add(TimbnSettings.Changed(entry, handler, _owner.PluginLogger));

    /// <summary>
    /// Whether a toggle is on and its feature is working, which is what <see cref="Toggle(ConfigEntry{bool}, Action, Action)"/>
    /// and <see cref="While(ConfigEntry{bool}, Func{IDisposable})"/> go by. Use it in place of the entry's Value in a
    /// patch or a handler that reads the toggle directly.
    /// </summary>
    /// <param name="entry">The toggle.</param>
    /// <returns>True when the toggle is on and no game update has broken the feature's patches.</returns>
    public bool IsOn(ConfigEntry<bool> entry) => entry.Value && !_owner.IsFeatureBroken(entry.Definition.Key);

    private Func<T, bool> Working<T>(ConfigEntry<T> entry, Func<T, bool> isOn) =>
        value => !_owner.IsFeatureBroken(entry.Definition.Key) && isOn(value);
}
