using System.Globalization;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's slider, a minus, the track and a plus, with the value printed after the plus. Clicking the number types an
/// exact value, up to four decimals, and the handle sits on the nearest step.
/// </summary>
internal sealed class TimbnSliderWidget : TimbnWidget
{
    private const int _maxDecimals = 4;
    private const float _numberGap = 2f;

    private readonly Slider _slider;
    private readonly TextMeshProUGUI _amount;
    private readonly Func<float> _get;
    private readonly Action<float> _set;
    private readonly float _min;
    private readonly float _max;
    private readonly float _stepSize;
    private readonly bool _wholeNumbers;
    private readonly int _decimals;
    private readonly ManualLogSource _logger;

    public TimbnSliderWidget(TimbnWidgets.SettingParts parts, float min, float max, bool wholeNumbers, int decimals, Func<float> get, Action<float> set, ManualLogSource logger)
        : base(parts.Row, parts.Name)
    {
        _logger = logger;
        _get = get;
        _set = set;
        _min = min;
        _max = max;
        _wholeNumbers = wholeNumbers;
        _stepSize = TimbnSliderSteps.StepSize(wholeNumbers, decimals, min, max);
        _decimals = TimbnSliderSteps.DecimalPlaces(_stepSize);
        // A range of one value still needs a step, or the slider's position would divide by zero.
        var steps = float.IsFinite(max - min) ? Math.Clamp(Mathf.RoundToInt(Math.Min((max - min) / _stepSize, int.MaxValue / 2f)), 1, int.MaxValue / 2) : 1;

        var minus = TimbnTheme.Minus(parts.Control);
        _slider = Track(parts.Control, steps);
        var plus = TimbnTheme.Plus(parts.Control);
        var slot = TimbnWidgets.NumberSlot(parts.Control);
        _amount = TimbnUi.Stretch(TimbnUi.Text(slot, TimbnTheme.Value, string.Empty), -_numberGap);
        // Like the game's own slider, a number wider than its slot runs on past it toward the row's edge.
        _amount.overflowMode = TextOverflowModes.Overflow;
        _amount.raycastTarget = true;
        var type = _amount.gameObject.AddComponent<Button>();
        type.targetGraphic = _amount;
        type.navigation = new Navigation { mode = Navigation.Mode.None };
        type.onClick.AddListener(TypeValue);

        var stop = GamepadStop(Nudge);

        // The game's slider script wires the minus and plus buttons, writes the number, plays the tick and calls back with
        // the slider's value times the step, which is how far the value is above the minimum.
        var game = TimbnGameScript.Add<UISlider>(
            parts.Control,
            ("slider", _slider),
            ("increaseButton", plus),
            ("decreaseButton", minus),
            ("amountLabel", _amount),
            ("gamepadNavigationItem", stop));
        game.Initialize(above =>
        {
            var value = Snap(_min + above);
            _amount.text = Format(value);
            _set(value);
        }, 0f, _stepSize);
        // Initialize sets the slider to run from 0 to 100 steps.
        _slider.maxValue = steps;
        TimbnGameScript.SilenceKeys(game, listsOfKeys: true);
        Refresh();
    }

    public override void Refresh()
    {
        var value = _get();
        _slider.SetValueWithoutNotify(Mathf.Round((value - _min) / _stepSize));
        _amount.text = Format(value);
    }

    private void Nudge(int delta) => _slider.value = Mathf.Clamp(_slider.value + delta, _slider.minValue, _slider.maxValue);

    // The game's own track, as wide as the row leaves it, stepping by whole steps from 0.
    private static Slider Track(Transform parent, int steps)
    {
        var slider = TimbnTheme.SliderTrack(parent);
        TimbnUi.Fill((RectTransform)slider.transform);
        slider.onValueChanged.RemoveAllListeners();
        slider.wholeNumbers = true;
        slider.minValue = 0f;
        slider.maxValue = steps;
        return slider;
    }

    private void TypeValue()
    {
        if (!Interactable)
            return;

        TimbnPrompts.Prompt(TitleText, Format(_get()), string.Empty, text =>
        {
            if (!float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return;

            value = Mathf.Clamp(value, _min, _max);
            // Typing is exact to four decimals, so the value can sit between steps and the handle takes the nearest one.
            _set(_wholeNumbers ? Mathf.Round(value) : (float)Math.Round(value, _maxDecimals));
            Refresh();
        }, null, _logger, wholeNumbers: _wholeNumbers);
    }

    private float Snap(float value) => (float)Math.Round(value, _decimals);

    private static string Format(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}
