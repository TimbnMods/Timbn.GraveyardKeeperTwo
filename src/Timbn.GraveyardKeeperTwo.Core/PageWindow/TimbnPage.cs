namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A page of the game's UI that a mod fills with a <see cref="TimbnPageBuilder"/>, and that Core shows as a popup window
/// with <see cref="TimbnPluginUI.ShowPage(TimbnPage, bool)"/> or as a tab in the Mods window with
/// <see cref="TimbnPluginUI.AddSettingsPage(TimbnPage, bool)"/>. A simple page is a title and a lambda, and
/// <see cref="TimbnPluginUI.ShowPage(string, Action{TimbnPageBuilder, Action}, TimbnLength?, TimbnLength?, Action?, bool)"/> makes
/// one without a class. A page that keeps state can be a class that derives from this and overrides <see cref="Build"/>.
/// Core runs the build every time the page is shown, with an empty page, and destroys the page and everything in it when
/// it closes, so keep what the page remembers in fields and locals and not in the controls. A popup is the game's own
/// window frame with the gold title bar, so it gets Esc, the close button, B and gamepad navigation from the game, and
/// it knows nothing about the Mods window.
/// </summary>
/// <example>
/// <code>
/// var popup = new TimbnPage("Rename", (page, close) =>
/// {
///     page.Add(new TimbnTextBox(() => _name, value => _name = value));
///     page.ButtonFooter(footer => footer.AddPrimarySpecial("Save", () => { Save(_name); close(); }));
/// });
/// popup.Width = TimbnLength.Pixels(180);
/// UI.ShowPage(popup);
/// </code>
/// </example>
public class TimbnPage
{
    private readonly string _title = "";
    private readonly Action<TimbnPageBuilder, Action>? _build;
    private readonly Action? _dismissed;
    private Action? _close;

    /// <summary>Makes a page from a title and a lambda that builds it.</summary>
    /// <param name="title">The page's name, drawn in the title bar of a popup and on the tab in the Mods window, already in the player's language.</param>
    /// <param name="build">Fills the page, and gets an action that closes the popup it is showing in. In a tab the action does nothing.</param>
    /// <param name="dismissed">Runs when the player backs out of a popup with Esc, B or the close button, so it is the place for what Cancel would do.</param>
    public TimbnPage(string title, Action<TimbnPageBuilder, Action> build, Action? dismissed = null)
    {
        _title = title;
        _build = build;
        _dismissed = dismissed;
    }

    /// <summary>For a page that is a class of its own, which overrides <see cref="Title"/> and <see cref="Build"/>.</summary>
    protected TimbnPage()
    {
    }

    /// <summary>The page's name, drawn in the title bar of a popup and on the tab in the Mods window, already in the player's language.</summary>
    public virtual string Title => _title;

    /// <summary>
    /// How wide a popup is, in the game's UI units for the room inside its frame or as a share of the screen. It is kept
    /// at least a small width. Auto, the default, is half the screen. A tab is as wide as the Mods window leaves it.
    /// </summary>
    public TimbnLength Width { get; set; }

    /// <summary>
    /// How tall a popup is, in the game's UI units for the room inside its frame or as a share of the screen. Auto, the
    /// default, makes it as tall as its content with the game's own spacing around it, the way the pause menu is. A
    /// page with a scroll area has no height of its own to fill, so it asks for one. A tab is as tall as the Mods window leaves it.
    /// </summary>
    public TimbnLength Height { get; set; }

    /// <summary>
    /// Whether a popup on a gamepad changes its switches, choices and sliders with the triggers (LT and RT) and not the
    /// d-pad, for a page that puts them beside other controls, since left and right then have to move between those. Null,
    /// the default, picks triggers when a stepper has another control beside it when the page opens, which misses ones in a
    /// card that is hidden then, so a page with those sets it. A tab is never affected.
    /// </summary>
    public bool? StepWithTriggers { get; set; }

    /// <summary>
    /// Whether the page reaches the window's frame on the left and right, with none of the game's side padding, for a page
    /// whose header plates join the border with rails (<see cref="TimbnSection"/>). False by default.
    /// </summary>
    public bool EdgeToEdge { get; set; }

    /// <summary>
    /// A flat color for the whole inside of the window, for seeing where a layout's boxes really are. Null, the default, keeps
    /// the game's own background. It needs <see cref="EdgeToEdge"/>.
    /// </summary>
    public Color? Backdrop { get; set; }

    /// <summary>
    /// Whether B, Esc and the close button back out of a popup. They do by default, and run the page's dismissed callback or
    /// its <see cref="TimbnFooterBuilder.AddSecondary"/> button. Set it to false for a question the player must answer, which
    /// then has no close button and no Back hint. A tab is never affected.
    /// </summary>
    public bool AcceptsBackButton { get; set; } = true;

    /// <summary>Whether a popup freezes the game clock while it is open, like the Mods window does. A tab is never affected.</summary>
    public bool PauseGame { get; set; } = true;

    /// <summary>Fills the page. Core runs it every time the page is shown. A throw is logged and leaves whatever was built before it.</summary>
    /// <param name="page">The builder for the empty page.</param>
    protected virtual void Build(TimbnPageBuilder page) => _build?.Invoke(page, Close);

    /// <summary>
    /// Runs when the player backs out of a popup with Esc, B or the close button. It does not run when the page closes
    /// itself with <see cref="Close"/> or when the handle from <see cref="TimbnPluginUI.ShowPage(TimbnPage, bool)"/> is
    /// disposed, so it is the place for what Cancel would do.
    /// </summary>
    protected virtual void OnDismissed() => _dismissed?.Invoke();

    /// <summary>
    /// Runs when the player backs out of a popup with Esc, B or the close button, before it closes. Call
    /// <paramref name="dismiss"/> to let it close, which then runs <see cref="OnDismissed"/>, or leave it uncalled to keep
    /// the popup open, such as while asking whether to throw away changes and calling it only on an answer. It does not run
    /// for <see cref="Close"/> or when the handle is disposed.
    /// </summary>
    /// <param name="dismiss">Closes the popup the way backing out does.</param>
    protected virtual void OnDismissRequested(Action dismiss) => dismiss();

    internal void RequestDismiss(Action dismiss) => OnDismissRequested(dismiss);

    /// <summary>
    /// Runs when the popup goes away, however that happens, backing out, <see cref="Close"/>, the handle being disposed or
    /// another page taking its place.
    /// </summary>
    protected virtual void OnClosed()
    {
    }

    /// <summary>Runs every frame while the popup is showing, for a page that watches something outside it, such as a setting that changes its size.</summary>
    protected virtual void OnTick()
    {
    }

    internal void Closed() => OnClosed();

    internal void Tick() => OnTick();

    /// <summary>Closes the popup this page is showing in, such as from an OK button. It does nothing in a tab, and the page is not told it was dismissed.</summary>
    protected void Close() => _close?.Invoke();

    // The area is a column that already fills the page, so a tab, which is handed a plain box, wraps one first.
    internal void Mount(RectTransform column, Action? close, ManualLogSource logger)
    {
        _close = close;
        Build(TimbnPageBuilder.Create(column, logger));
    }

    internal void Dismissed() => OnDismissed();
}
