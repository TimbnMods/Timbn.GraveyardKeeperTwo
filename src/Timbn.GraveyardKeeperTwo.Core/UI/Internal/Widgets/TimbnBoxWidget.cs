using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// A value nobody can change, drawn as plain text where a setting's value box would be, so it does not look like a box to type in.
internal sealed class TimbnBoxWidget : TimbnWidget
{
    private readonly Func<string> _get;
    private readonly TextMeshProUGUI _text;

    public TimbnBoxWidget(TimbnWidgets.SettingParts parts, Func<string> get)
        : base(parts.Row, parts.Name)
    {
        _get = get;
        (_, var back, _text) = TimbnWidgets.ValueBox(parts.Control, TextAlignmentOptions.MidlineLeft);
        back.enabled = false;
        // With no box drawn the text starts where the box would, like any other line in the column.
        TimbnUi.Stretch(_text);
        TimbnWidgets.FitValueArt(parts.Row);
        Refresh();
    }

    public override void Refresh()
    {
        _text.text = _get();
        TimbnTextFit.AlignLeftEdges([_text]);
    }
}
