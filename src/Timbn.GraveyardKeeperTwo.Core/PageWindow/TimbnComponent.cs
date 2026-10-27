using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The base of Core's components. Make one with its constructor, set what it should do, and put it on a page with
/// <see cref="TimbnPageBuilder.Add{T}(T)"/>. The properties work before the component is on a page and after, so a
/// component can be dimmed or hidden whenever the mod decides. A mod can make a component of its own by deriving from
/// it and building its objects in <see cref="Build(Transform)"/>, and it then gets the same switches and tooltip.
/// </summary>
/// <example>
/// <code>
/// public sealed class Swatch(Color color) : TimbnComponent
/// {
///     protected override RectTransform Build(Transform parent)
///     {
///         var box = new GameObject("Swatch", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
///         box.transform.SetParent(parent, false);
///         box.GetComponent&lt;Image&gt;().color = color;
///         box.GetComponent&lt;LayoutElement&gt;().preferredHeight = 16f;
///         return (RectTransform)box.transform;
///     }
/// }
///
/// page.Add(new Swatch(Color.cyan) { Disabled = new(() => !_enabled) });
/// </code>
/// </example>
public abstract class TimbnComponent
{
    private const float _disabledAlpha = 0.5f;

    private TimbnBinding<bool> _disabled;
    private TimbnBinding<bool> _visible = true;
    private bool _disabledNow;
    private bool _visibleNow = true;
    private bool _hiddenNow;
    private bool? _offApplied;
    private TimbnPageWatcher? _watcher;
    private CanvasGroup? _group;

    /// <summary>Makes the component. It builds nothing until it is put on a page.</summary>
    protected TimbnComponent()
    {
    }

    /// <summary>The component's box once it is on a page, for a mod that wants to put an object of its own next to it, or null before.</summary>
    public RectTransform? Root { get; private set; }

    /// <summary>
    /// Dims the component and stops it taking input, as a value or a callback that is read all the time. On a
    /// <see cref="TimbnField"/> or a container it covers everything inside.
    /// </summary>
    public TimbnBinding<bool> Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            RefreshState();
        }
    }

    /// <summary>Whether the component is shown, as a value or a callback that is read all the time. A hidden one takes no room.</summary>
    public TimbnBinding<bool> Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            RefreshState();
        }
    }

    /// <summary>
    /// The token of a text a mod registered with its Text API, shown in the game's tooltip while the mouse is over the
    /// component, or null for none. A gamepad shows no tooltip. Set it before the component is put on a page. It points
    /// at the component's own box, so on a <see cref="TimbnField"/> it covers the whole row. Set it on the control inside
    /// the field for a tooltip that points at the control.
    /// </summary>
    public string? TooltipToken { get; set; }

    /// <summary>
    /// Whether the gamepad's focus is on the component or on something inside it, such as a list's line or a field's
    /// control. Always false with the mouse and keyboard, and before the component is on a page.
    /// </summary>
    public bool HasFocus =>
        Root != null
        && LazyInput.IsGamepadActive
        && Root.GetComponentInParent<GamepadNavigationController>()?.FocusedItem is { } item
        && item.transform.IsChildOf(Root);

    /// <summary>
    /// With a gamepad in use, puts the focus on the component's first control, such as a switch the page just built again.
    /// With a mouse in use it sets where the gamepad's focus starts once the player picks one up. It does nothing before
    /// the component is on a page or while it is hidden.
    /// </summary>
    public void Focus()
    {
        if (Root != null && Root.gameObject.activeInHierarchy)
            TimbnPageWindow.FocusOrResume(Root.GetComponentsInChildren<GamepadNavigationItem>(false).FirstOrDefault(item => item.isActiveAndEnabled && item.Active));
    }

    internal TimbnPageWatcher? Watcher => _watcher;

    // Set by the builder it is added through, for a component whose box fills along the parent's direction.
    internal bool InColumn { get; set; }

    // A line of text, which the builder follows with a small space in a column.
    internal virtual bool LeavesLineGap => false;

    internal virtual bool ReadOnlyNow => false;

    // Whether the component's host hides it for now, such as a button that a key stands in for with a gamepad.
    internal virtual bool HiddenByHost => false;

    /// <summary>
    /// Makes the component's objects under the page's box and returns the one that holds them, which becomes
    /// <see cref="Root"/>. The box is a layout group, so give the root a <c>LayoutElement</c> with the size it wants.
    /// Core runs it once each time the page is built, and destroys what it made when the page goes. A component the
    /// player can press calls <see cref="MakePressable(RectTransform, Action)"/> on its box.
    /// </summary>
    /// <param name="parent">The page's box to build in.</param>
    protected internal abstract RectTransform Build(Transform parent);

    internal void BuildInto(Transform parent, TimbnPageWatcher? watcher)
    {
        // A component kept in a field can go on a page again after its first one closed, so nothing from that build is kept.
        _group = null;
        _offApplied = null;
        OnBuilding();
        _watcher = watcher;
        Root = Build(parent);
        if (TooltipToken != null)
            UIMouseTooltip.Attach(Root.gameObject, TooltipToken);

        // The first time the box gets the state the bindings ask for, whatever they said before it was built.
        _disabledNow = _disabled.Current;
        _visibleNow = _visible.Current;
        _hiddenNow = HiddenByHost;
        ReadOnlyChanged();
        Apply();
        OnBuilt();
        _watcher?.Watch(() => _disabled.Current, _ => RefreshState());
        _watcher?.Watch(() => _visible.Current, _ => RefreshState());
        _watcher?.Watch(() => HiddenByHost, _ => RefreshState());
    }

    /// <summary>
    /// Makes a box the player can press the way a list's card is pressed, by a click or by A with the gamepad's focus on it,
    /// with the game's yellow frame while the mouse or the focus is over it. The box becomes a stop for the gamepad. A
    /// throw in the press is logged like <see cref="Run(Action, string)"/>. Call it from <see cref="Build(Transform)"/>.
    /// </summary>
    /// <param name="box">The box to press, usually the root <see cref="Build(Transform)"/> returns.</param>
    /// <param name="onPress">Runs on each press.</param>
    protected void MakePressable(RectTransform box, Action onPress)
    {
        TimbnUi.MakeButton(box, () => Run(onPress, "press"));
        TimbnWidget.MarkSelect(box);
    }

    /// <summary>
    /// A builder that fills a box this component made, the way a container such as a <see cref="TimbnCard"/> fills itself
    /// with the components it holds. It lays out top to bottom unless the box is a row. Call it from
    /// <see cref="Build(Transform)"/>.
    /// </summary>
    /// <param name="box">A box the component made in <see cref="Build(Transform)"/>, usually a row or a column.</param>
    /// <returns>The builder, whose components go in the box and belong to the same page.</returns>
    protected TimbnPageBuilder BuilderFor(RectTransform box) =>
        TimbnPageBuilder.Inside(box, _watcher ?? throw new InvalidOperationException($"{GetType().Name} is not being built on a page."));

    /// <summary>
    /// Runs the mod's code for a click or a change and logs anything it throws to the page's logger, under the
    /// component's type, instead of letting it break the game's event. Use it in the handlers <see cref="Build(Transform)"/> hooks up.
    /// When the component is not on a page there is no logger, so it just runs the code.
    /// </summary>
    /// <param name="action">The code to run.</param>
    /// <param name="what">A few words for the log line, such as "click".</param>
    protected internal void Run(Action action, string what)
    {
        if (_watcher?.Logger is { } logger)
            TimbnSafe.Run(action, logger, $"{GetType().Name}|{what}");
        else
            action();
    }

    // Reads the bindings now and puts the box in the state they ask for, doing nothing when it has not changed.
    internal void RefreshState()
    {
        var disabled = _disabled.Current;
        var visible = _visible.Current;
        var readOnlyChanged = ReadOnlyChanged();
        var hidden = HiddenByHost;
        if (Root == null || (disabled == _disabledNow && visible == _visibleNow && hidden == _hiddenNow && !readOnlyChanged))
        {
            _disabledNow = disabled;
            _visibleNow = visible;
            _hiddenNow = hidden;
            return;
        }

        _disabledNow = disabled;
        _visibleNow = visible;
        _hiddenNow = hidden;
        Apply();
    }

    internal virtual bool ReadOnlyChanged() => false;

    internal virtual void OnBuilding()
    {
    }

    internal virtual void OnBuilt()
    {
    }

    internal virtual void OnApplied(bool off)
    {
    }

    private void Apply()
    {
        if (Root == null)
            return;

        Root.gameObject.SetActive(_visibleNow && !_hiddenNow);
        var readOnly = ReadOnlyNow;
        var off = _disabledNow || readOnly;
        _group ??= Root.GetComponent<CanvasGroup>() ?? Root.gameObject.AddComponent<CanvasGroup>();
        _group.alpha = off ? _disabledAlpha : 1f;
        _group.interactable = !off;
        _group.blocksRaycasts = !readOnly;
        // Only a change of state touches the selectables, and each takes what every group above it allows, so a container
        // coming back on does not wake a component inside it that is disabled on its own.
        if (_offApplied != off && (_offApplied != null || off))
        {
            foreach (var selectable in Root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
                selectable.interactable = GroupsAllow(selectable.transform);
        }

        _offApplied = off;
        OnApplied(off);
    }

    internal static bool GroupsAllow(Transform? transform)
    {
        for (; transform != null; transform = transform.parent)
        {
            if (transform.GetComponent<CanvasGroup>() is not { } group)
                continue;

            if (!group.interactable)
                return false;

            if (group.ignoreParentGroups)
                return true;
        }

        return true;
    }
}
