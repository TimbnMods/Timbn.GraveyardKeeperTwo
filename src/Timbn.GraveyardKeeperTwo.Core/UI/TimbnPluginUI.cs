namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts a plugin's own things on screen (windows, hints and its own GUI) and fades the screen through black with code
/// the plugin can call off, all taken down again when the plugin unloads. See <see cref="TimbnUI"/> to read the
/// game's windows. Reach it through the plugin's UI property.
/// </summary>
public sealed class TimbnPluginUI
{
    private readonly TimbnFrameworkPlugin _owner;
    private readonly List<TimbnGuiWindow> _windows = [];

    internal TimbnPluginUI(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Fades the screen to black, runs your code while it is black, and fades back in, like
    /// <see cref="TimbnUI.FadeThrough"/>, but the code can be called off before the screen goes black, such as when
    /// the player changes their mind during the fade.
    /// </summary>
    /// <example>
    /// <code>
    /// _fade?.Dispose();
    /// _fade = UI.FadeThrough(SitDown);
    /// </code>
    /// </example>
    /// <param name="whileBlack">The code to run once the screen is black.</param>
    /// <returns>A handle that calls the code off when disposed. It is also called off when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable FadeThrough(Action whileBlack)
    {
        var calledOff = false;
        var owned = _owner.SessionSubscriptions.Add(() => calledOff = true);
        TimbnUI.FadeThrough(() =>
        {
            if (calledOff)
                return;

            owned.Dispose();
            TimbnSafe.Run(whileBlack, _owner.PluginLogger, $"{nameof(TimbnPluginUI)}.{nameof(FadeThrough)}|{TimbnSafe.Describe(whileBlack)}");
        });
        return owned;
    }

    /// <summary>
    /// Makes a "press a key to do something" hint the plugin can show over any spot in the world. It is hidden
    /// when the plugin unloads and on the way to the main menu, and can be shown again afterwards. Dispose one the
    /// plugin is done with, such as one made per visitor, so the plugin forgets it.
    /// </summary>
    /// <returns>The hint, hidden until its Show is called.</returns>
    public TimbnInteractionHint CreateHint()
    {
        var hint = new TimbnInteractionHint();
        hint.Owned = TimbnUndo.All([_owner.Events.SaveClosed(hint.Hide), _owner.Subscriptions.Add(hint.Hide)]);
        return hint;
    }

    /// <summary>
    /// Makes an icon bubble the plugin can show over any spot in the world, such as what a customer wants. It is
    /// hidden when the plugin unloads and on the way to the main menu, and can be shown again afterwards. Dispose
    /// one the plugin is done with, such as one made per customer, so the plugin forgets it.
    /// </summary>
    /// <example>
    /// <code>
    /// _wants = UI.CreateBubble();
    /// _wants.Show(customer.Position + Vector3.up * 2f, "i_cup_beer");
    /// </code>
    /// </example>
    /// <returns>The bubble, hidden until its Show is called.</returns>
    public TimbnBubble CreateBubble()
    {
        var bubble = new TimbnBubble();
        bubble.Owned = TimbnUndo.All([_owner.Events.SaveClosed(bubble.Hide), _owner.Subscriptions.Add(bubble.Hide)]);
        return bubble;
    }

    /// <summary>
    /// Opens the game's chest window on a storage only the plugin knows about, such as a keg, with the player's
    /// inventory beside it so they move items in and out the usual way. The storage is a list of item ids and
    /// counts the plugin keeps, usually in its save data, and every move the player makes is written back into it
    /// at once.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.OpenStorage(Data.Current.Keg, "my_mod_keg", 10, "beer");
    /// </code>
    /// </example>
    /// <param name="contents">The items in the storage, item id to count. It is changed as the player moves items.</param>
    /// <param name="titleKey">A text key added with Text.Add, shown over the storage, such as "Keg".</param>
    /// <param name="slots">How many slots the storage has. Items that do not fit stay in the contents but are not shown.</param>
    /// <param name="itemIds">The only items the player may put in. Leave it out to take anything.</param>
    /// <returns>A handle that closes the window when disposed, if it is still open. It is also closed when the plugin unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable OpenStorage(IDictionary<string, int> contents, string titleKey, int slots, params string[] itemIds) =>
        _owner.SessionSubscriptions.Add(TimbnStorageWindow.Open(contents, titleKey, slots, itemIds, _owner.PluginLogger));

    /// <summary>
    /// Adds something the player can do beside an object in the world that the game gives no interaction for. See
    /// <see cref="TimbnObjectAction"/>. The hint, the walk and the held controls are taken down when the handle is
    /// disposed, the plugin unloads or the player returns to the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.AddObjectAction(new TimbnObjectAction("common_box_08", new Vector3(-104.36f, 0f, -338.20f), "my_mod_pet"));
    /// </code>
    /// </example>
    /// <param name="action">What to do and where.</param>
    /// <returns>A handle that removes the action when disposed. You don't need to keep it.</returns>
    public IDisposable AddObjectAction(TimbnObjectAction action)
    {
        var runner = new TimbnObjectActionRunner(_owner, action);
        var update = _owner.Events.Update(runner.Tick);
        var closed = _owner.Events.SaveClosed(runner.Dispose);
        return _owner.Subscriptions.Add(() =>
        {
            update.Dispose();
            closed.Dispose();
            runner.Dispose();
        });
    }

    /// <summary>
    /// Draws Unity IMGUI every GUI pass, on the main menu and in game, while the plugin is enabled. Use it in place
    /// of an OnGUI method, for a debug window or an overlay. If the handler throws, the error is logged once and
    /// the handler is stopped.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.DrawGui(() =>
    /// {
    ///     if (_menuOpen)
    ///         GUILayout.Label($"Day {TimbnClock.Day}, {TimbnClock.TimeText}");
    /// });
    /// </code>
    /// </example>
    /// <param name="draw">The IMGUI code to run.</param>
    /// <returns>A handle that stops drawing when disposed. You don't need to keep it.</returns>
    public IDisposable DrawGui(Action draw) => _owner.Subscriptions.Add(TimbnGui.Add(draw, () => _owner.IsRunning, _owner.PluginLogger));

    /// <summary>
    /// Makes an IMGUI window for a debug or cheat menu. Core draws the box, a title line that names the key to
    /// close it, and a "load a save" note, so you only draw what goes inside. The window is taken down when the
    /// plugin unloads. Set its <see cref="TimbnGuiWindow.Area"/> and the other properties on the returned window.
    /// </summary>
    /// <example>
    /// <code>
    /// var menu = UI.CreateDebugWindow("Timbn Cheats", DrawCheats, PluginConfig.ToggleMenu);
    /// </code>
    /// </example>
    /// <param name="title">The window's title, drawn on its first line with the key that closes it.</param>
    /// <param name="draw">Draws the contents with GUILayout calls. It runs only while the window is visible.</param>
    /// <param name="toggle">The key that opens and closes the window, or null to set <see cref="TimbnGuiWindow.IsVisible"/> from code.</param>
    /// <returns>The window.</returns>
    public TimbnGuiWindow CreateDebugWindow(string title, Action draw, ConfigEntry<KeyboardShortcut>? toggle = null)
    {
        var window = new TimbnGuiWindow(title, draw, toggle);
        _windows.Add(window);
        _owner.Subscriptions.Add(() => _windows.Remove(window));
        DrawGui(window.Draw);
        return window;
    }

    internal void Tick()
    {
        foreach (var window in _windows)
            window.Tick();
    }
}
