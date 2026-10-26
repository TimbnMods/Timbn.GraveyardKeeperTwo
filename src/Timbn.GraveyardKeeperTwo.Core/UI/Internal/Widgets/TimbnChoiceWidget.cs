using LazyBearTechnology;
using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A value box between a left and a right arrow that step through named options, looping at the ends. The game's own
/// UISwitchButton runs it, on the arrows and box Core lays out.
/// </summary>
internal sealed class TimbnChoiceWidget : TimbnWidget
{
    private readonly IReadOnlyList<string> _options;
    private readonly Func<int> _get;
    private readonly UISwitchButton? _switch;
    private readonly LazyButton _left;
    private readonly LazyButton _right;
    private readonly TextMeshProUGUI _value;

    public TimbnChoiceWidget(TimbnWidgets.SettingParts parts, IReadOnlyList<string> options, Func<int> get, Action<int> set)
        : base(parts.Row, parts.Name)
    {
        _options = options;
        _get = get;
        _left = TimbnTheme.Arrow(parts.Control, right: false);
        _value = TimbnWidgets.ValueBox(parts.Control, TextAlignmentOptions.Center).Text;
        _right = TimbnTheme.Arrow(parts.Control, right: true);
        TimbnWidgets.FitArrowArt(parts.Row);
        var stop = GamepadStop(delta => (delta > 0 ? _right : _left).onClick.Invoke());
        if (options.Count > 0)
        {
            _switch = TimbnGameScript.Add<UISwitchButton>(
                parts.Control,
                ("increaseButton", _right),
                ("decreaseButton", _left),
                ("amountLabel", _value),
                ("gamepadNavigationItem", stop));
            _switch.Initialize(set, [.. options], Index);
            TimbnGameScript.SilenceKeys(_switch, listsOfKeys: false);
        }

        Refresh();
    }

    public override void Refresh()
    {
        if (_switch != null)
            _switch.UpdateField(Index, fireCallback: false);
    }

    private int Index => Mathf.Clamp(_get(), 0, Math.Max(0, _options.Count - 1));
}
