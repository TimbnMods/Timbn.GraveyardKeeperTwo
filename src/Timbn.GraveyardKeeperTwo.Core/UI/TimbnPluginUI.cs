using System.ComponentModel;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts a mod's things on screen (windows, hints and its own GUI). It also fades the screen through black with code
/// the mod can call off. Everything is taken down again when the mod unloads. See <see cref="TimbnUI"/> to read the
/// game's windows. Reach it through the UI property.
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
    /// Fades the screen to black, runs your code while it is black, and fades back in, the way the game hides a
    /// change such as the player sitting down or a teleport. The code can be called off before the screen goes black,
    /// such as when the player changes their mind during the fade. When the game has no fade to show, or one is
    /// already showing, the code runs straight away.
    /// </summary>
    /// <example>
    /// <code>
    /// _fade?.Dispose();
    /// _fade = UI.FadeThrough(SitDown);
    /// </code>
    /// </example>
    /// <param name="whileBlack">The code to run once the screen is black.</param>
    /// <returns>A handle that calls the code off when disposed. It is also called off when the mod unloads and on the way to the main menu.</returns>
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
    /// Shows a bar of key hints at the bottom of the screen, the one the game shows while the player fishes, with the
    /// keys' icons for the keyboard or gamepad in use, such as how to get up while the player sits. It stays until the
    /// returned handle is disposed, the mod unloads or the player leaves the save. When several are shown the newest
    /// one is drawn. It only shows the hints, so the mod still reads the keys itself.
    /// </summary>
    /// <example>
    /// <code>
    /// _hints = UI.ShowKeyHints(new TimbnKeyHint(GameKey.Interaction, Text.Key("my_mod_get_up")), new TimbnKeyHint(GameKey.Back, Text.Key("my_mod_get_up")));
    /// </code>
    /// </example>
    /// <param name="hints">The keys and what they do, left to right.</param>
    /// <returns>A handle that takes the bar away when disposed.</returns>
    public IDisposable ShowKeyHints(params TimbnKeyHint[] hints) =>
        _owner.SessionSubscriptions.Add(TimbnKeyHints.Show(hints, _owner.PluginLogger));

    /// <summary>
    /// Makes a "press a key to do something" hint the mod can show over any spot in the world. It is hidden when
    /// the mod unloads and on the way to the main menu and can be shown again afterwards. Dispose a hint the mod is
    /// done with, for example one made per visitor, so the mod forgets it.
    /// </summary>
    /// <returns>The hint, hidden until its Show is called.</returns>
    public TimbnInteractionHint CreateHint()
    {
        var hint = new TimbnInteractionHint();
        hint.Owned = TimbnUndo.All([_owner.Events.SaveClosed(hint.Hide), _owner.Subscriptions.Add(hint.Hide)]);
        return hint;
    }

    /// <summary>
    /// Makes an icon bubble the mod can show over any spot in the world, such as what a customer wants. It is hidden
    /// when the mod unloads and on the way to the main menu and can be shown again afterwards. Dispose a bubble the
    /// mod is done with, for example one made per customer, so the mod forgets it.
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
    /// Opens the game's chest window on a storage only the mod knows about, such as a keg. The player's inventory
    /// sits beside it so they move items in and out as usual. The storage is a list of item ids and counts the mod
    /// keeps, usually in its save data. Every move the player makes is written back into it at once. It does nothing
    /// when no save is loaded or another chest window is open.
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
    /// <returns>A handle that closes the window when disposed, if it is still open. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable OpenStorage(IDictionary<string, int> contents, string titleKey, int slots, params string[] itemIds) =>
        OwnPopup(closed => TimbnStorageWindow.Open(contents, titleKey, slots, itemIds, _owner.PluginLogger, closed));

    /// <summary>
    /// Asks the player for one line of text in a small popup built from the same pieces as <see cref="ShowPage(TimbnPage, bool)"/>,
    /// with the text box under the title and an OK button, such as to name a chest. The box has the caret as soon as it
    /// opens, typing never sets off the game's hotkeys, and the game pauses the way it does for any popup. Saving with
    /// empty text still calls the handler, so a mod can treat it as a removal. It opens over a page that is showing
    /// without closing it, and another popup opened over it stacks on top.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.ShowPrompt("Chest Label", Data.Current.Labels[id], text => Data.Current.Labels[id] = text, "Type a name");
    /// </code>
    /// </example>
    /// <param name="title">The window's header, already in the player's language.</param>
    /// <param name="current">The text the field starts with.</param>
    /// <param name="onSave">Gets the typed text when the player presses OK or Enter.</param>
    /// <param name="placeholder">The hint shown in the empty field.</param>
    /// <param name="onCancel">Runs when the player backs out with Cancel, the close button or Esc.</param>
    /// <returns>A handle that closes the window without calling any handler when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowPrompt(string title, string current, Action<string> onSave, string placeholder = "", Action? onCancel = null) =>
        OwnPopup(closed => TimbnPrompts.Prompt(title, current, placeholder, onSave, onCancel, _owner.PluginLogger, closed));

    /// <summary>
    /// Asks the player a yes or no question in a small popup built from the same pieces as
    /// <see cref="ShowPage(TimbnPage, bool)"/>, such as before something that cannot be undone. The game pauses the way it
    /// does for any popup. It opens over a page that is showing without closing it, and another popup opened over it stacks
    /// on top.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.ShowConfirm("Empty the keg?", "The beer inside is lost.", EmptyKeg);
    /// </code>
    /// </example>
    /// <param name="title">The window's header, already in the player's language.</param>
    /// <param name="message">The question, already in the player's language.</param>
    /// <param name="onYes">Runs when the player answers Yes.</param>
    /// <param name="onNo">Runs when the player answers No, uses the close button or presses Esc.</param>
    /// <returns>A handle that closes the window without calling any handler when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowConfirm(string title, string message, Action onYes, Action? onNo = null) =>
        OwnPopup(closed => TimbnPrompts.Confirm(title, message, onYes, onNo, _owner.PluginLogger, closed));

    /// <summary>
    /// Tells the player something in a small popup built from the same pieces as <see cref="ShowPage(TimbnPage, bool)"/>, with
    /// one OK button. The game pauses the way it does for any popup. It opens over a page that is showing without closing
    /// it, and another popup opened over it stacks on top.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.ShowMessage("Closing time", "The tavern is shut for the night.");
    /// </code>
    /// </example>
    /// <param name="title">The window's header, already in the player's language.</param>
    /// <param name="message">The text, already in the player's language.</param>
    /// <param name="onOk">Runs when the player presses OK, uses the close button or presses Esc.</param>
    /// <returns>A handle that closes the window without calling any handler when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowMessage(string title, string message, Action? onOk = null) =>
        OwnPopup(closed => TimbnPrompts.Message(title, message, onOk, _owner.PluginLogger, closed));

    /// <summary>
    /// Opens the game's own right click menu, a list of lines the player picks one from. The menu closes when a line is
    /// picked, then runs that line. A click outside it, Esc or B closes it with nothing picked. Nothing opens while
    /// another menu is showing.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.ShowMenu([new TimbnMenuItem("Rename", Rename), new TimbnMenuItem("Delete", Delete, enabled: _canDelete)]);
    /// </code>
    /// </example>
    /// <param name="items">The lines, top to bottom.</param>
    /// <param name="position">Where the menu goes on the screen in pixels, or null for the mouse pointer (the middle of the screen with a gamepad or when the pointer is outside the window).</param>
    /// <returns>A handle that closes the menu when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowMenu(IReadOnlyList<TimbnMenuItem> items, Vector2? position = null) =>
        OwnPopup(closed => TimbnGameWindows.Menu(items, position, _owner.PluginLogger, closed));

    /// <summary>
    /// Opens the game's own inventory for picking one item, the player's bag and the chests nearby, with the items the
    /// filter turns down dimmed so they cannot be picked. Picking one closes the window and calls the handler with its id.
    /// Esc or B closes it with nothing picked. Nothing opens while another inventory window is showing.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.ShowItemPicker(id => TimbnGameIds.Items.Apple == id, id => Log.LogInfo($"Picked {id}"));
    /// </code>
    /// </example>
    /// <param name="filter">Says whether an item can be picked, or null to let every item be picked.</param>
    /// <param name="onChosen">Gets the picked item's id.</param>
    /// <param name="headerKey">A text key added with Text.Add, shown over the window, or null for the game's own header.</param>
    /// <returns>A handle that closes the window when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowItemPicker(Func<string, bool>? filter, Action<string> onChosen, string? headerKey = null) =>
        OwnPopup(closed => TimbnGameWindows.ItemPicker(filter, onChosen, headerKey, _owner.PluginLogger, closed));

    /// <summary>
    /// Opens the game's own window for choosing how many of an item, a slider with the count typed or stepped, and OK and
    /// Cancel. It does nothing when the item does not exist. Nothing opens while another dialog is showing.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.ShowItemCount(TimbnGameIds.Items.Apple, 1, 25, count => Sell(count));
    /// </code>
    /// </example>
    /// <param name="itemId">The item's id, such as one from TimbnGameIds.Items.</param>
    /// <param name="min">The fewest the player can choose.</param>
    /// <param name="max">The most the player can choose, which is also the count the window shows the item with.</param>
    /// <param name="onConfirm">Gets the count when the player presses OK.</param>
    /// <returns>A handle that closes the window without calling the handler when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowItemCount(string itemId, int min, int max, Action<int> onConfirm) =>
        OwnPopup(closed => TimbnGameWindows.ItemCount(itemId, min, max, onConfirm, _owner.PluginLogger, closed));

    [Obsolete("Use Interactions.Register with a position, then Player.WalkTo and TimbnPlayer.Face. This does nothing and goes away in the next major version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IDisposable AddObjectAction(TimbnObjectAction action) => TimbnUndo.None;

    /// <summary>
    /// Draws Unity IMGUI every GUI pass, on the main menu and in game, while the mod is enabled. Use it in place
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
    /// Adds a tab of the mod's own in the Mods window. When the player picks the tab, the lambda gets a
    /// <see cref="TimbnPageBuilder"/> for an empty page under the tab bar and fills it. Core adds no scrolling or saving
    /// of its own, so start with a <see cref="TimbnScroll"/> for a tab that can grow taller than the window. The
    /// page is made fresh each time the tab is shown and destroyed with everything in it when the player leaves the tab
    /// or closes the window. By default the Settings page made from the mod's config entries stays and this tab is
    /// added after it. Pass true for <paramref name="replaceDefault"/> to show only your tabs. The tab is removed when
    /// the mod unloads.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.AddSettingsPage("Labels", page => page.Scroll(list =>
    /// {
    ///     list.Card(card => card.Add(new TimbnField("Show labels", "", new TimbnToggle(() => _show, value => _show = value))));
    /// }));
    /// </code>
    /// </example>
    /// <param name="title">The tab's name, already in the player's language.</param>
    /// <param name="build">Fills the page.</param>
    /// <param name="replaceDefault">Whether to hide the page made from the mod's config entries.</param>
    /// <returns>A handle that removes the tab early when disposed. You don't need to keep it.</returns>
    public IDisposable AddSettingsPage(string title, Action<TimbnPageBuilder> build, bool replaceDefault = false) =>
        _owner.Subscriptions.Add(TimbnSettingsPages.Add(_owner.Metadata.GUID, title, build, replaceDefault, _owner.PluginLogger));

    /// <summary>
    /// Adds a whole <see cref="TimbnPage"/> as a tab of the mod's own in the Mods window, named by the page's
    /// <see cref="TimbnPage.Title"/>. Core calls its build with a fresh empty page each time the player picks the tab.
    /// It destroys the page when they leave the tab or close the window. The same page can also be shown as a popup
    /// with <see cref="ShowPage(TimbnPage, bool)"/>. In a tab the page's width, height and pause settings are ignored and
    /// closing it does nothing. By default the Settings page made from the mod's config entries stays and this tab is
    /// added after it. Pass true for <paramref name="replaceDefault"/> to show only your tabs. The tab is removed when
    /// the mod unloads.
    /// </summary>
    /// <param name="page">The page to show in the tab.</param>
    /// <param name="replaceDefault">Whether to hide the page made from the mod's config entries.</param>
    /// <returns>A handle that removes the tab early when disposed. You don't need to keep it.</returns>
    public IDisposable AddSettingsPage(TimbnPage page, bool replaceDefault = false) =>
        _owner.Subscriptions.Add(TimbnSettingsPages.Add(_owner.Metadata.GUID, page.Title, builder => page.Mount(builder.Root, null, _owner.PluginLogger), replaceDefault, _owner.PluginLogger));

    /// <summary>
    /// Adds a tab for the mod to its group's page in the Mods window. The tab goes after the tab that lists the group's
    /// mods. A shared changelog or credits page is a good fit. The group is the one the mod is in (see
    /// <see cref="TimbnModInfo.ModGroupId"/> and <see cref="TimbnModInfo.ModGroupName"/>). A mod in no group gets a
    /// warning and no tab. The tab only shows while the group has two or more mods. When the player picks the tab, the
    /// lambda fills an empty page from its <see cref="TimbnPageBuilder"/>, just like
    /// <see cref="AddSettingsPage(string, Action{TimbnPageBuilder}, bool)"/>. The tab is removed when the mod unloads.
    /// </summary>
    /// <param name="title">The tab's name, already in the player's language.</param>
    /// <param name="build">Fills the page.</param>
    /// <returns>A handle that removes the tab early when disposed. You don't need to keep it.</returns>
    public IDisposable AddGroupPage(string title, Action<TimbnPageBuilder> build)
    {
        if (TimbnModInfos.GroupOf(_owner.Metadata.GUID) is not { } group)
        {
            _owner.PluginLogger.LogWarning($"{nameof(AddGroupPage)}|Skipped the group tab '{title}', since the mod is in no group. Give it a ModGroupName in its ModInfo first.");
            return TimbnUndo.None;
        }

        return _owner.Subscriptions.Add(TimbnGroupPages.Add(_owner.Metadata.GUID, group.Key, title, build, _owner.PluginLogger));
    }

    /// <summary>
    /// Shows a <see cref="TimbnPage"/> in a popup window in the game's own look, with the gold title bar, the close
    /// button, Esc and gamepad navigation. It is a window of its own with no mod list or tabs, and it freezes the game
    /// clock while open unless the page says otherwise. Core calls the page's build with an empty page and destroys the
    /// page when the popup closes. A page shown while another is open takes its place.
    /// </summary>
    /// <example>
    /// <code>
    /// _popup = UI.ShowPage(new RenamePage());
    /// </code>
    /// </example>
    /// <param name="page">The page to show.</param>
    /// <param name="overOthers">True to open it in a window of its own over the page that is showing, such as a question about that page, instead of taking its place.</param>
    /// <returns>A handle that closes the popup without telling the page it was dismissed when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowPage(TimbnPage page, bool overOthers = false) =>
        OwnPopup(closed => TimbnPageWindow.Show(page, _owner.PluginLogger, closed, overOthers));

    /// <summary>
    /// Whether a page can open now, which is once the game has made the window pages are copied from. A page shown before
    /// that opens nothing and logs a warning, so a mod that shows one on its own, such as at start-up, checks this first.
    /// </summary>
    public bool CanShowPage => TimbnPageWindow.CanShow;

    /// <summary>
    /// Shows a popup made from a title and a lambda, with no class of its own. It is the same popup as
    /// <see cref="ShowPage(TimbnPage, bool)"/>, with the game's gold title bar, close button, Esc and gamepad navigation.
    /// </summary>
    /// <example>
    /// <code>
    /// UI.ShowPage("Rename", (page, close) =>
    /// {
    ///     page.Add(new TimbnTextBox(() => _name, value => _name = value));
    ///     page.Row(TimbnAlign.Center, row =>
    ///     {
    ///         row.Add(new TimbnButton("OK", () => { Save(_name); close(); }) { Disabled = new(() => _name.Length == 0) });
    ///         row.Add(new TimbnButton("Cancel", close));
    ///     });
    /// }, width: TimbnLength.Pixels(180));
    /// </code>
    /// </example>
    /// <param name="title">The title bar's text, already in the player's language.</param>
    /// <param name="build">Fills the page, and gets an action that closes the popup.</param>
    /// <param name="width">How wide the popup is, or null for half the screen.</param>
    /// <param name="height">How tall the popup is, or null to fit its content.</param>
    /// <param name="dismissed">Runs when the player backs out with Esc, B or the close button.</param>
    /// <param name="overOthers">True to open it in a window of its own over the page that is showing instead of taking its place.</param>
    /// <returns>A handle that closes the popup without calling <paramref name="dismissed"/> when disposed. It is also closed when the mod unloads and on the way to the main menu. You don't need to keep it.</returns>
    public IDisposable ShowPage(string title, Action<TimbnPageBuilder, Action> build, TimbnLength? width = null, TimbnLength? height = null, Action? dismissed = null, bool overOthers = false) =>
        ShowPage(new TimbnPage(title, build, dismissed) { Width = width ?? TimbnLength.Auto, Height = height ?? TimbnLength.Auto }, overOthers);

    /// <summary>
    /// Makes an IMGUI window for a debug or cheat menu. Core draws the box, a title line that names the key to
    /// close it, and a "load a save" note, so you only draw what goes inside. The window is taken down when the
    /// mod unloads. Set its <see cref="TimbnGuiWindow.Area"/> and the other properties on the returned window.
    /// </summary>
    /// <example>
    /// <code>
    /// var menu = UI.CreateDebugWindow("Timbn Cheats", DrawCheats, PluginConfig.ToggleMenu);
    /// </code>
    /// </example>
    /// <param name="title">The window's title, drawn on its first line with the key that closes it.</param>
    /// <param name="draw">Draws the contents with GUILayout calls. It runs only while the window is visible.</param>
    /// <param name="toggle">The key that opens and closes the window, or null to set <see cref="TimbnGuiWindow.IsVisible"/> from code.</param>
    public TimbnGuiWindow CreateDebugWindow(string title, Action draw, ConfigEntry<KeyboardShortcut>? toggle = null)
    {
        var window = new TimbnGuiWindow(title, draw, toggle);
        _windows.Add(window);
        _owner.Subscriptions.Add(() => _windows.Remove(window));
        DrawGui(window.Draw);
        return window;
    }

    // A popup that closes on its own takes its handle out of the session bag, so the bag does not grow with every popup shown.
    private IDisposable OwnPopup(Func<Action, IDisposable> open)
    {
        IDisposable? owned = null;
        var closedAlready = false;
        var handle = open(() =>
        {
            if (owned == null)
                closedAlready = true;
            else
                owned.Dispose();
        });

        if (ReferenceEquals(handle, TimbnUndo.None))
            return handle;

        owned = _owner.SessionSubscriptions.Add(handle);
        if (closedAlready)
            owned.Dispose();

        return owned;
    }

    internal void Tick()
    {
        foreach (var window in _windows)
            window.Tick();
    }
}
