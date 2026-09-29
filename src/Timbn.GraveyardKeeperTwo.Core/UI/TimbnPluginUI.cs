namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts a plugin's own things on screen, taken down again when the plugin unloads. See <see cref="TimbnUI"/> to
/// read the game's windows and fade the screen. Reach it through the plugin's UI property.
/// </summary>
public sealed class TimbnPluginUI
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginUI(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Makes a "press a key to do something" hint the plugin can show over any spot in the world. It is hidden
    /// when the plugin unloads and on the way to the main menu, and can be shown again afterwards.
    /// </summary>
    /// <returns>The hint, hidden until its Show is called.</returns>
    public TimbnInteractionHint CreateHint()
    {
        var hint = new TimbnInteractionHint();
        _owner.Subscriptions.Add(() => hint.Hide());
        _owner.Subscriptions.Add(TimbnGameEvents.GoToMainMenu(hint.Hide));
        return hint;
    }

    /// <summary>
    /// Draws Unity IMGUI every GUI pass, on the main menu and in game, while the plugin is enabled. Use it in place
    /// of an OnGUI method, for a debug window or an overlay. If the handler throws, the error is logged once and
    /// the handler is stopped.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.Gui(() =>
    /// {
    ///     if (_menuOpen)
    ///         GUILayout.Label($"Day {TimbnClock.Day}, {TimbnClock.Time}");
    /// });
    /// </code>
    /// </example>
    /// <param name="draw">The IMGUI code to run.</param>
    /// <returns>A handle that stops drawing when disposed. You don't need to keep it.</returns>
    public IDisposable Gui(Action draw) => _owner.Subscriptions.Add(TimbnGui.Add(draw, () => _owner.IsRunning, _owner.PluginLogger));
}
