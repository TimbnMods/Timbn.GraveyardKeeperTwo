using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A small value box between two arrows that steps through colors, centered in the row like the character editor's color
/// switchers. The box shows the position as "3/10" over a swatch of the color, and no color shows no swatch.
/// </summary>
internal sealed class TimbnColorWidget : TimbnWidget
{
    private const float _swatchInset = 2f;
    private const float _boxWidth = 42f;

    private readonly IReadOnlyList<string> _colors;
    private readonly Func<string?> _get;
    private readonly Action<string?> _set;
    private readonly UISwitchButton _switch;
    private readonly Image _swatch;
    private List<string?> _choices = [];

    public TimbnColorWidget(TimbnWidgets.SettingParts parts, IReadOnlyList<string> colors, Func<string?> get, Action<string?> set)
        : base(parts.Row, parts.Name)
    {
        _colors = colors;
        _get = get;
        _set = set;
        TimbnUi.Spring(parts.Control);
        var left = TimbnTheme.Arrow(parts.Control, right: false);
        var (box, _, text) = TimbnWidgets.ValueBox(parts.Control, TextAlignmentOptions.Center);
        TimbnUi.Fixed(box, _boxWidth);
        _swatch = TimbnUi.Box(box, "Swatch").gameObject.AddComponent<Image>();
        _swatch.raycastTarget = false;
        TimbnUi.Stretch(_swatch, -_swatchInset, -_swatchInset, -_swatchInset, -_swatchInset);
        _swatch.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
        var right = TimbnTheme.Arrow(parts.Control, right: true);
        TimbnUi.Spring(parts.Control);
        TimbnWidgets.FitArrowArt(parts.Row);
        var stop = GamepadStop(delta => (delta > 0 ? right : left).onClick.Invoke());
        _switch = TimbnGameScript.Add<UISwitchButton>(
            parts.Control,
            ("increaseButton", right),
            ("decreaseButton", left),
            ("amountLabel", text),
            ("gamepadNavigationItem", stop));
        Refresh();
    }

    public override void Refresh()
    {
        var current = _get();
        _choices = TimbnColorTags.BuildChoices(_colors, current, TimbnColorTags.IsColor);
        var index = TimbnColorTags.IndexOf(_choices, current);
        // The box shows where in the list the color is, and the game's switch steps through those labels.
        _switch.Initialize(Pick, [.. Enumerable.Range(1, _choices.Count).Select(position => $"{position}/{_choices.Count}")], index);
        TimbnGameScript.SilenceKeys(_switch, listsOfKeys: false);
        var shown = ColorUtility.TryParseHtmlString(_choices[index] ?? string.Empty, out var color);
        _swatch.gameObject.SetActive(shown);
        _swatch.color = color;
    }

    private void Pick(int index)
    {
        _set(_choices[index]);
        Refresh();
    }
}
