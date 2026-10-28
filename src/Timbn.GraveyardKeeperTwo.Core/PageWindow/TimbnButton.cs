using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One of the game's buttons, with text on it or one of the game's icons. A text button is the red button as wide as
/// its text. It can be dimmed with <see cref="TimbnComponent.Disabled"/> like any other component.
/// </summary>
/// <example>
/// <code>
/// page.Add(new TimbnButton("Save", () => Save()));
/// var reset = page.Add(new TimbnButton(TimbnIcon.Reset, () => Reset()));
/// reset.Disabled = new(() => _value == _default);
/// </code>
/// </example>
public sealed class TimbnButton : TimbnComponent
{
    /// <summary>How wide the game's reset button shows, so a row that ends with one lines up with the others.</summary>
    public const float ResetWidth = 24f;

    private readonly string? _text;
    private readonly TimbnIcon? _icon;
    private readonly Action _onClick;
    private const float _hintPadding = 8f;

    private GameKey? _key;
    private TimbnGamepadPlacement _placement;
    private TimbnPageWatcher? _keyWatcher;
    private bool _looking;

    /// <summary>A button with text on it.</summary>
    /// <param name="text">The text, already in the player's language.</param>
    /// <param name="onClick">Called when the player presses it.</param>
    public TimbnButton(string text, Action onClick)
    {
        _text = text;
        _onClick = onClick;
    }

    /// <summary>
    /// A button with one of the game's icons on it. It is for the mouse only, since a gamepad cannot focus it, so give a
    /// gamepad player another way to do the same thing, such as a key or a text button.
    /// </summary>
    /// <param name="icon">Which of the game's icons it shows.</param>
    /// <param name="onClick">Called when the player presses it.</param>
    public TimbnButton(TimbnIcon icon, Action onClick)
    {
        _icon = icon;
        _onClick = onClick;
    }

    internal string Label => _text ?? string.Empty;

    internal override bool HiddenByHost => _key is not null && _placement == TimbnGamepadPlacement.Bottom && LazyInput.IsGamepadActive;

    // Whether the player can see the button, or the hint a gamepad shows under the window in its place. A button in a box
    // that is hidden is not, so its key does nothing and its hint goes.
    internal bool Reachable =>
        Root != null && Visible.Current && Root.parent != null && Root.parent.gameObject.activeInHierarchy && (HiddenByHost || Root.gameObject.activeSelf);

    internal bool Pressable => Reachable && !Disabled.Current && GroupsAllow(Root!.parent);

    // A key a closed page tied to the button goes with that page, so the button is plain again on the next one.
    internal override void OnBuilding()
    {
        _looking = false;
        if (_key is not null && _keyWatcher == null)
            _key = null;
    }

    protected internal override RectTransform Build(Transform parent)
    {
        var root = BuildButton(parent);
        if (_key is not null)
            TimbnWidget.UnmarkSelect(root);

        return root;
    }

    // Presses the button the way a click does, for the key a page tied to it.
    internal void Press()
    {
        if (Pressable)
            Run(_onClick, "key");
    }

    // The key runs the button. With a gamepad the button either goes and the window hints the key under the panel, or
    // stays where it is as the hint.
    internal void BindKey(GameKey key, TimbnGamepadPlacement placement, TimbnPageWatcher watcher)
    {
        _key = key;
        _placement = placement;
        _keyWatcher = watcher;
        if (Root == null)
            return;

        TimbnWidget.UnmarkSelect(Root);
        RefreshState();
        WatchLook();
    }

    internal void UnbindKey()
    {
        _key = null;
        if (Root == null)
            return;

        // A text button takes A again once its key is gone, as it did before the page tied one to it.
        if (_icon is null && !TimbnWidget.TakesSelect(Root))
            TimbnWidget.MarkSelect(Root);

        RefreshState();
    }

    internal override void OnBuilt() => WatchLook();

    // Inline, the art goes with a gamepad and the label starts with the key's glyph, as the game's own dialog buttons do.
    private void WatchLook()
    {
        if (_looking || Root == null || _text == null || Watcher == null)
            return;

        _looking = true;
        var label = Root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        if (label == null)
            return;

        label.textWrappingMode = TextWrappingModes.NoWrap;
        var plain = TimbnUi.Element(Root).preferredWidth;
        void Show() => ShowLook(label, plain);
        Watcher.Watch(() => _key is not null && _placement == TimbnGamepadPlacement.Inline && LazyInput.IsGamepadActive, _ => Show());
        Watcher.Watch(() => Disabled.Current, _ => Show());
    }

    private void ShowLook(TextMeshProUGUI label, float plain)
    {
        var hint = _key is not null && _placement == TimbnGamepadPlacement.Inline && LazyInput.IsGamepadActive;
        foreach (var art in Root!.GetComponentsInChildren<Image>(true))
            art.enabled = !hint;

        // The hint is not something to focus, so the gamepad's list of stops is read again without it. A disabled stop is
        // still found by the controller, so the list is handed over with only the enabled ones.
        if (Root.GetComponent<GamepadNavigationItem>() is { } stop && stop.enabled == hint)
        {
            if (hint)
                stop.Unfocus();

            stop.enabled = !hint;
            stop.Active = !hint;
            if (Root.GetComponentInParent<GamepadNavigationController>() is { } controller)
                controller.ReinitItems(hint, [.. controller.GetComponentsInChildren<GamepadNavigationItem>().Where(item => item.enabled)]);
        }

        var text = hint ? new LazyGameKeyTip(_key!, _text!, !Disabled.Current, translate: false).ToString() : _text!;
        label.text = text;
        // With no art around it the hint needs only its own width.
        TimbnUi.Fixed(Root, hint ? label.GetPreferredValues(text).x + _hintPadding : plain);
    }

    private RectTransform BuildButton(Transform parent)
    {
        if (_icon is not { } icon)
            return TimbnWidgets.Button(parent, _text!, () => Run(_onClick, "click")).Root;

        var button = icon switch
        {
            TimbnIcon.Reset => TimbnTheme.Reset(parent),
            TimbnIcon.Left => TimbnTheme.Arrow(parent, right: false),
            TimbnIcon.Right => TimbnTheme.Arrow(parent, right: true),
            TimbnIcon.Minus => TimbnTheme.Minus(parent),
            _ => TimbnTheme.Plus(parent),
        };
        if (icon == TimbnIcon.Reset)
            TimbnUi.Fixed(button, ResetWidth);

        button.onClick.AddListener(() => Run(_onClick, "click"));
        return (RectTransform)button.transform;
    }
}
