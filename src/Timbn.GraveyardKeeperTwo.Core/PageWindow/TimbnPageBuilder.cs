namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Builds a page in the game's own look. A page is a column of components, each made with its constructor and put on
/// with <see cref="Add{T}(T)"/>, such as a <see cref="TimbnText"/>, a <see cref="TimbnSection"/> or a control. A
/// container is a component that holds components of its own, such as a <see cref="TimbnRow"/>, a
/// <see cref="TimbnColumn"/>, a <see cref="TimbnCard"/>, a <see cref="TimbnScroll"/>, a <see cref="TimbnRegion"/> or a
/// <see cref="TimbnSplit"/>, and each has a one call shorthand in <see cref="TimbnPageShorthands"/>, such as
/// <c>page.Row(row => ...)</c>. <see cref="ButtonFooter"/> is the one container that is a call of the builder, since it
/// only takes buttons tied to a key. A control takes callbacks instead of values. One is a get that reads it and one is
/// a set that stores it. Its <see cref="TimbnComponent.Disabled"/> is read all the time to gray it out. A <see cref="TimbnField"/> puts a name on the left and components on the right, with an optional description in
/// small gray text under the name. Nothing is saved, so a mod that wants a Save button keeps its own pending values.
/// </summary>
/// <example>
/// <code>
/// UI.ShowPage("Rename", (page, close) =>
/// {
///     page.Add(new TimbnTextBox(() => _name, value => _name = value));
///     page.ButtonFooter(footer => footer.AddPrimarySpecial("Save", () => { Save(_name); close(); }, new(() => _name.Length == 0)));
/// });
/// </code>
/// </example>
public sealed class TimbnPageBuilder
{
    internal const float RowGap = 6f;

    // The space between the parts of a column, a page's own included, unless the column asks for another.
    internal const float PartGap = 6f;
    private const float _lineGap = 2f;
    private const int _footerGap = 5;
    private const string _textGapName = "TextGap";

    private readonly bool _vertical;
    private readonly TimbnPageWatcher _watcher;
    private readonly ManualLogSource _logger;

    // The space the last line of text left under itself, which a footer right after it takes back.
    private RectTransform? _lineGapBox;

    private TimbnPageBuilder(RectTransform root, bool vertical, TimbnPageWatcher watcher, ManualLogSource logger)
    {
        Root = root;
        _vertical = vertical;
        _watcher = watcher;
        _logger = logger;
    }

    /// <summary>
    /// The box this builder adds to, for a mod that wants to put an object of its own in it. It is a layout group, a row
    /// or a column, so it sizes itself from what it holds.
    /// </summary>
    public RectTransform Root { get; }

    /// <summary>
    /// Makes a builder that fills any box with a column of its own, for UI that is not a page, in the game's look. Whatever
    /// is built goes when the box is destroyed. Nothing reads keys outside a page window. <see cref="ButtonFooter"/>
    /// buttons only work with the mouse there. <see cref="AddKeyAction"/> and the calls that tie a button to a key do
    /// nothing.
    /// </summary>
    /// <param name="parent">The box to fill.</param>
    /// <param name="logger">The mod's logger. A control's problems are logged to it.</param>
    public static TimbnPageBuilder On(RectTransform parent, ManualLogSource logger) =>
        Create(TimbnUi.Stretch(TimbnUi.Column(parent, "Column")), logger);

    internal static TimbnPageBuilder Create(RectTransform column, ManualLogSource logger)
    {
        var watcher = column.gameObject.GetComponent<TimbnPageWatcher>() ?? column.gameObject.AddComponent<TimbnPageWatcher>();
        watcher.Logger = logger;
        return new TimbnPageBuilder(column, true, watcher, logger);
    }

    /// <summary>
    /// Calls changed with the value that read returns. It calls once straight away, then again each time the value differs
    /// from the last one. It checks every frame while the page is open. Use it for a part of the page that is not a control
    /// and has to follow the mod's state, like an object the mod put in <see cref="Root"/>. It stops when the page goes.
    /// </summary>
    /// <param name="read">Reads the value. Keep it cheap, since it runs every frame.</param>
    /// <param name="changed">Gets the new value.</param>
    public void Watch<T>(Func<T> read, Action<T> changed) => _watcher.WatchValue(read, changed);

    /// <summary>Reads every control's value again from its get, for when something other than the control changed it.</summary>
    public void Refresh()
    {
        foreach (var widget in _watcher.Widgets)
            widget.Refresh();
    }

    /// <summary>
    /// Puts a component on the page, built in the box this builder adds to. This is the one way to add a control, such as
    /// a <see cref="TimbnToggle"/> or a <see cref="TimbnButton"/>, or a <see cref="TimbnField"/> that puts a name and
    /// description beside one.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <returns>The same component, now built.</returns>
    public T Add<T>(T component)
        where T : TimbnComponent
    {
        component.InColumn = _vertical;
        component.BuildInto(Root, _watcher);
        if (component.LeavesLineGap)
            LineGap();

        return component;
    }

    // A line of text is exactly as tall as its font, so one in a column is followed by a small space.
    private void LineGap()
    {
        if (!_vertical)
            return;

        _lineGapBox = TimbnUi.Space(Root, height: _lineGap);
        _lineGapBox.name = _textGapName;
    }

    /// <summary>
    /// A row of buttons at this point of the page, centered, with the game's gap above it. Its lambda gets a
    /// <see cref="TimbnFooterBuilder"/> whose calls add each button and tie it to a key of a gamepad's face. It is the
    /// only place Core adds a button of its own for a key, so a page lays out its dialog buttons with one call. Put it
    /// last on a popup page, after any scroll area, to keep it under the content.
    /// </summary>
    /// <param name="build">Adds the buttons.</param>
    public void ButtonFooter(Action<TimbnFooterBuilder> build)
    {
        // The game's dialogs keep the same room between a message or a box and their buttons, so the small space that follows
        // a text line gives way to the footer's own.
        if (_lineGapBox != null && Root.childCount > 0 && Root.GetChild(Root.childCount - 1) == _lineGapBox)
            UnityEngine.Object.DestroyImmediate(_lineGapBox.gameObject);

        var box = TimbnUi.Row(Root, "ButtonFooter", RowGap, align: TextAnchor.MiddleCenter);
        box.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().padding.top = _footerGap;
        build(new TimbnFooterBuilder(new TimbnPageBuilder(box, false, _watcher, _logger)));
    }

    /// <summary>
    /// Ties a button the page made itself to A, for a button that sits in a layout of its own. It adds nothing, so put the
    /// button on the page with <see cref="Add{T}(T)"/> as well. The key presses the button, and it does nothing while the
    /// button is dimmed, hidden or in a box that is hidden. With <see cref="TimbnGamepadPlacement.Bottom"/> the button
    /// goes while a gamepad is in use and its hint under the window stands in for it, so the key still works. A page with
    /// other buttons or text boxes still presses the one that has the focus on A, and this button when nothing else has. See
    /// <see cref="TimbnFooterBuilder.AddPrimary"/>.
    /// </summary>
    /// <param name="button">A button that is on the page or goes on it.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void BindPrimary(TimbnButton button, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        Bind(TimbnFaceButton.Primary, button, gamepad);

    /// <summary>Ties a button the page made itself to B and Esc. See <see cref="TimbnFooterBuilder.AddSecondary"/> and <see cref="BindPrimary(TimbnButton, TimbnGamepadPlacement)"/>.</summary>
    /// <param name="button">A button that is on the page or goes on it.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void BindSecondary(TimbnButton button, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        Bind(TimbnFaceButton.Secondary, button, gamepad);

    /// <summary>Ties a button the page made itself to X (F on the keyboard). See <see cref="BindPrimary(TimbnButton, TimbnGamepadPlacement)"/>.</summary>
    /// <param name="button">A button that is on the page or goes on it.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void BindPrimarySpecial(TimbnButton button, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        Bind(TimbnFaceButton.PrimarySpecial, button, gamepad);

    /// <summary>Ties a button the page made itself to Y. See <see cref="BindPrimary(TimbnButton, TimbnGamepadPlacement)"/>.</summary>
    /// <param name="button">A button that is on the page or goes on it.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void BindSecondarySpecial(TimbnButton button, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        Bind(TimbnFaceButton.SecondarySpecial, button, gamepad);

    internal void AddAction(TimbnFaceButton slot, string label, Action onPress, TimbnBinding<bool> disabled, TimbnGamepadPlacement gamepad)
    {
        var button = new TimbnButton(label, onPress) { Disabled = disabled };
        Add(button);
        Bind(slot, button, gamepad);
    }

    /// <summary>
    /// Ties an action to a button of a gamepad's face with no button of its own, so the key alone runs it and the hint under
    /// the window names it, such as a Y that resets the setting in focus. With <paramref name="onHold"/> a quick press runs
    /// <paramref name="onPress"/> when the key is let go, and holding it for a moment runs <paramref name="onHold"/> instead,
    /// and the hint reads like "Reset Setting (Hold to Reset All)". Core does not choose keys for the page, so the page
    /// says which button, and a second call for the same button replaces the first. It works wherever the builder sits in a
    /// page window, a popup or a tab of the Mods window, and does nothing on a builder from <see cref="On"/> outside one.
    /// </summary>
    /// <param name="button">The button of the gamepad's face.</param>
    /// <param name="label">The hint's text, already in the player's language.</param>
    /// <param name="onPress">Called when the key is pressed, or let go when there is a hold action.</param>
    /// <param name="disabled">Dims the hint and stops the key, as a value or a callback that is read all the time.</param>
    /// <param name="onHold">Called when the key is held for a moment, or null for none.</param>
    /// <param name="holdLabel">The hint's words for the hold, such as "Hold to Reset All".</param>
    public void AddKeyAction(TimbnFaceButton button, string label, Action onPress, TimbnBinding<bool> disabled = default, Action? onHold = null, string holdLabel = "")
    {
        Unbind(button);
        _watcher.Actions.Add(new TimbnPageAction(button, label, onPress, disabled, onHold, holdLabel));
    }

    private void Unbind(TimbnFaceButton slot)
    {
        foreach (var old in _watcher.Actions.Where(action => action.Face == slot).ToList())
        {
            old.Button?.UnbindKey();
            _watcher.Actions.Remove(old);
        }
    }

    private void Bind(TimbnFaceButton slot, TimbnButton button, TimbnGamepadPlacement gamepad)
    {
        Unbind(slot);

        // Outside a page window nothing reads the keys, so the button stays a plain button.
        if (Root.GetComponentInParent<TimbnPageWindow>(true) == null)
            return;

        var action = new TimbnPageAction(slot, button, gamepad);
        _watcher.Actions.Add(action);
        button.BindKey(action.Key, gamepad, _watcher);
    }

    // A builder for a box a component made on the same page, laying out top to bottom unless the box is a row.
    internal static TimbnPageBuilder Inside(RectTransform box, TimbnPageWatcher watcher) =>
        new(box, box.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() == null, watcher, watcher.Logger);

    // A column keeps the page's gap unless it asks for another.
    internal static float ColumnGap(float? gap) => gap ?? PartGap;

    internal static TextAnchor Anchor(TimbnAlign across, TimbnAlign vertical) => (vertical, across) switch
    {
        (TimbnAlign.Start, TimbnAlign.Center) => TextAnchor.UpperCenter,
        (TimbnAlign.Start, TimbnAlign.End) => TextAnchor.UpperRight,
        (TimbnAlign.Start, _) => TextAnchor.UpperLeft,
        (TimbnAlign.End, TimbnAlign.Center) => TextAnchor.LowerCenter,
        (TimbnAlign.End, TimbnAlign.End) => TextAnchor.LowerRight,
        (TimbnAlign.End, _) => TextAnchor.LowerLeft,
        (_, TimbnAlign.Center) => TextAnchor.MiddleCenter,
        (_, TimbnAlign.End) => TextAnchor.MiddleRight,
        _ => TextAnchor.MiddleLeft,
    };
}
