namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The base of the components that hold a value, such as a toggle or a text box. They are the control alone with no name,
/// so a <see cref="TimbnField"/> puts a title and description beside one. Besides what every component has they can be
/// <see cref="ReadOnly"/>.
/// </summary>
public abstract class TimbnControl : TimbnComponent
{
    private TimbnBinding<bool> _readOnly;
    private bool _readOnlyNow;

    internal TimbnControl()
    {
    }

    /// <summary>
    /// Shows the value grayed out and stops it being changed, as a value or a callback that is read all the time. The control
    /// stays the control it is, so a read only switch still looks like a switch, and on a <see cref="TimbnField"/> only the
    /// control grays out, not the title beside it.
    /// </summary>
    public TimbnBinding<bool> ReadOnly
    {
        get => _readOnly;
        set
        {
            _readOnly = value;
            RefreshState();
        }
    }

    /// <summary>
    /// The name a typing prompt opened from this control carries as its title. A <see cref="TimbnField"/> fills it in
    /// from its own title when this is empty.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>Reads the value again from its get, for when something other than this control changed it.</summary>
    public void Refresh() => Widget?.Refresh();

    internal TimbnWidget? Widget { get; private set; }

    // Set by the field it sits in, beside a title, where a control lines up with the controls of the rows around it.
    internal bool InField { get; set; }

    internal override bool ReadOnlyNow => _readOnlyNow;

    internal abstract TimbnWidget BuildWidget(Transform parent);

    // The control's set is the mod's code, so a throw in it is logged with the mod's name instead of breaking the game's event.
    internal Action<T> Guarded<T>(Action<T> set) => value => Run(() => set(value), "The set handler");

    protected internal override RectTransform Build(Transform parent)
    {
        Widget = BuildWidget(parent);
        Widget.PromptTitle = Title;
        Widget.ManagedByComponent = true;
        Watcher?.Widgets.Add(Widget);
        _readOnlyNow = _readOnly.Current;
        Watcher?.Watch(() => _readOnly.Current, _ => RefreshState());
        return Widget.Root;
    }

    internal override bool ReadOnlyChanged()
    {
        var now = _readOnly.Current;
        var changed = now != _readOnlyNow;
        _readOnlyNow = now;
        return changed;
    }

    internal override void OnApplied(bool off)
    {
        if (Widget != null)
            Widget.Interactable = !off;
    }
}
