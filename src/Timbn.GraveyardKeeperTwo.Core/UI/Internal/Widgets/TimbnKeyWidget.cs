using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A key setting, the key in a value box with the Controls window's rebind button after it. The box and the button both
/// start the key capture, and the Controls window's red dot shows in the box while a key is waited for.
/// </summary>
internal sealed class TimbnKeyWidget : TimbnWidget
{
    private const float _dotInset = 6f;

    private readonly Func<string> _get;
    private readonly TextMeshProUGUI _text;
    private readonly GameObject? _recording;

    public TimbnKeyWidget(TimbnWidgets.SettingParts parts, Func<string> get, Action onClick)
        : base(parts.Row, parts.Name)
    {
        _get = get;
        (var box, var back, _text) = TimbnWidgets.ValueBox(parts.Control, TextAlignmentOptions.Center);
        TimbnWidgets.Clickable(back, onClick);
        (var rebind, _recording) = TimbnTheme.Rebind(parts.Control);
        rebind?.onClick.AddListener(() => onClick());

        if (_recording != null)
        {
            var dot = (RectTransform)_recording.transform;
            dot.SetParent(box, false);
            dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(0f, 0.5f);
            dot.anchoredPosition = new Vector2(_dotInset, 0f);
            TimbnUi.Element(dot).ignoreLayout = true;
        }

        TimbnWidgets.FitValueArt(parts.Row);
        GamepadStop(select: onClick);
        Refresh();
    }

    public override void Refresh() => Show(_get(), recording: false);

    public void Show(string text, bool recording)
    {
        _text.text = text;
        _recording?.SetActive(recording);
    }
}
