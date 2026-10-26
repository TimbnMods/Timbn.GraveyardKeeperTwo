using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Makes the controls the Mods window and custom settings pages are built from, out of the game's own art and text
/// styles. A setting is one row, its name on the left and its control on the right. A value control takes a get and a
/// set, so it can reread its value after something else changed it, such as the Defaults button. Each control's own
/// widget class is in its own file.
/// </summary>
internal static class TimbnWidgets
{
    /// <summary>The pieces of a split header the page builder needs, the box it sits in, its left plate, the slot that takes the right plate's place, and the follower that stretches its divider.</summary>
    internal sealed class SplitHeaderParts(RectTransform box, TimbnTextWidget leftPlate, RectTransform? rightSlot, TimbnDividerFollow divider)
    {
        public RectTransform Box { get; } = box;

        public TimbnTextWidget LeftPlate { get; } = leftPlate;

        public RectTransform? RightSlot { get; } = rightSlot;

        public TimbnDividerFollow Divider { get; } = divider;
    }

    internal const float RowHeight = 26f;

    private const float _numberWidth = 19f;
    internal const float BoxHeight = 17f;
    internal const float TypeBoxHeight = 22f;
    private const float _frameReach = 3f;
    private const int _caretWidth = 2;
    private const float _doubleClickSeconds = 0.3f;

    private static readonly AccessTools.FieldRef<TMP_InputField, float> _doubleClickDelay =
        AccessTools.FieldRefAccess<TMP_InputField, float>("m_DoubleClickDelay");

    // The value font's line reaches further below the baseline than its letters do, so centering the line leaves the
    // letters high. The game nudges its own value text down for the same reason.
    private const float _letterDrop = 3f;
    // A baseline aligned line hangs near the top of its box, so it moves down this far to put the capitals in the middle
    // (measured in game on boxes 17 and 22 tall).
    private const float _baselineDrop = 6.5f;
    private const float _textInset = 6f;

    // The art of an arrow, minus or plus stands this far from the box beside it, as the game's settings rows draw them.
    private const float _artGap = 1.5f;

    // A slider's number keeps this much room after the plus.
    private const float _numberSlotWidth = _numberWidth + 2.5f;

    // How far the game's rails reach out of the page, toward the frame's silver bar.
    private const float _railReach = 12f;

    // The game's Inspirations page ends its left content 4 units before the divider's middle and starts its right content 3 after it.
    private const float _dividerLeft = 4f;
    private const float _dividerRight = 3f;

    /// <summary>The strip the left column keeps free for the divider between the two columns.</summary>
    internal const float LineStrip = _dividerLeft + _dividerRight;

    // The game draws the rails this tall, and the plates sit a little below the middle of them.
    private const float _railHeight = 71f;
    private const float _railLift = 1.5f;
    private const float _plateReach = 2f;

    // The divider's art is this wide, and the plates tuck under its edges by these amounts.
    private const float _dividerWidth = 16f;
    private const float _leftTuck = 5f;
    private const float _rightTuck = 4f;

    // The divider starts this far above the middle of the rails, as the game's does, and its foot hangs past the columns onto the frame's bar.
    private const float _dividerTop = 15f;
    private const float _dividerFoot = 7f;

    // The plate sprite's bottom row is clear, one unit high.
    private const float _plateClearRow = 1f;

    // The rails sprite is clear for its first 18 rows, so its top bar starts this far above the header's band.
    private const float _railClearTop = 18f;

    /// <summary>How far the rails' top bar reaches above a split header's band, which what sits above it has to leave room for.</summary>
    internal const float RailsAbove = _railHeight / 2f + _railLift - (RowHeight + 2f * BarMargin) / 2f - _railClearTop;

    // A header on rails is one box, as tall as its plate and the two rails bars around it, so what comes next is spaced from the bars.
    internal const float BarMargin = 2f;

    internal static RectTransform Band(Transform parent) => TimbnUi.Fixed(TimbnUi.Box(parent, "Header"), height: RowHeight + 2f * BarMargin);

    /// <summary>
    /// A copy of the Inspirations page's splitter, which holds the rails, both header plates and the divider between them as
    /// one object. The plates and the divider are moved to where the columns below them are, and the returned follower
    /// makes the divider reach down to the bottom of the columns once it is given them.
    /// </summary>
    internal static SplitHeaderParts SplitHeader(Transform parent, string leftTitle, string? rightTitle, float leftWidth, bool rightIsSlot)
    {
        var band = Band(parent);
        var splitter = (RectTransform)TimbnTheme.Splitter(band).transform;
        splitter.anchorMin = new Vector2(0f, 0.5f);
        splitter.anchorMax = new Vector2(1f, 0.5f);
        splitter.pivot = new Vector2(0.5f, 0.5f);
        splitter.offsetMin = new Vector2(-_railReach, -_railHeight / 2f + _railLift);
        splitter.offsetMax = new Vector2(_railReach, _railHeight / 2f + _railLift);
        splitter.GetComponent<Image>().raycastTarget = false;

        var center = _railReach + leftWidth + _dividerLeft;
        var left = (RectTransform)splitter.Find("HeaderInspirations");
        var right = (RectTransform)splitter.Find("HeaderPerks");
        var divider = (RectTransform)splitter.Find("Vertical");
        var low = left.offsetMin.y;
        var high = left.offsetMax.y;

        left.anchorMin = left.anchorMax = new Vector2(0f, 0.5f);
        left.offsetMin = new Vector2(_railReach - _plateReach, low);
        left.offsetMax = new Vector2(center - _leftTuck, high);
        var leftLabel = left.Find("Header").GetComponent<TextMeshProUGUI>();
        leftLabel.text = leftTitle;

        right.anchorMin = new Vector2(0f, 0.5f);
        right.anchorMax = new Vector2(1f, 0.5f);
        right.offsetMin = new Vector2(center + _rightTuck, low);
        right.offsetMax = new Vector2(-(_railReach - _plateReach), high);

        RectTransform? slot = null;
        if (rightIsSlot)
        {
            UnityEngine.Object.Destroy(right.gameObject);
            slot = TimbnUi.Column(splitter, "RightHeader");
            slot.anchorMin = right.anchorMin;
            slot.anchorMax = right.anchorMax;
            slot.offsetMin = new Vector2(right.offsetMin.x, -_railLift - RowHeight / 2f);
            slot.offsetMax = new Vector2(right.offsetMax.x, -_railLift + RowHeight / 2f);
        }
        else if (rightTitle != null)
            right.Find("Header").GetComponent<TMP_Text>().text = rightTitle;

        divider.anchorMin = divider.anchorMax = new Vector2(0f, 0.5f);
        divider.pivot = new Vector2(0.5f, 1f);
        divider.anchoredPosition = new Vector2(center, _dividerTop);
        divider.sizeDelta = new Vector2(_dividerWidth, divider.sizeDelta.y);
        divider.GetComponent<Image>().raycastTarget = false;

        // A canvas of its own, sorted just above the window's, draws the divider over the columns below it, which come after it in the page.
        var follow = divider.gameObject.AddComponent<TimbnDividerFollow>();
        follow.Below = _dividerFoot;
        follow.Sorting = divider.gameObject.AddComponent<Canvas>();
        return new SplitHeaderParts(band, new TimbnTextWidget(left, leftLabel), slot, follow);
    }

    // The bumper or trigger glyph at an end of a plate, shown only while a gamepad is in use.
    internal static void SectionKeys(TimbnTextWidget section, GameKey? leftKey, GameKey? rightKey, TimbnPageWatcher watcher)
    {
        var left = leftKey is null ? null : TimbnTabsWidget.KeyIcon(section.Root, 0f);
        var right = rightKey is null ? null : TimbnTabsWidget.KeyIcon(section.Root, 1f);
        watcher.WatchValue(() => LazyInput.IsGamepadActive, on =>
        {
            if (left != null)
            {
                left.gameObject.SetActive(on);
                left.text = LazyGameKeyTip.Get(leftKey!, "", translate: false);
            }

            if (right != null)
            {
                right.gameObject.SetActive(on);
                right.text = LazyGameKeyTip.Get(rightKey!, "", translate: false);
            }
        });
    }

    internal static TimbnTextWidget Section(Transform parent, string text)
    {
        var plate = TimbnUi.Fixed(TimbnUi.Box(parent, "Section"), height: RowHeight);
        TimbnUi.Skin(plate, TimbnTheme.SectionPlate);
        TimbnTheme.SectionEnds(plate);
        var label = TimbnUi.Stretch(TimbnUi.Text(plate, TimbnTheme.SectionText, text));
        label.alignment = TextAlignmentOptions.Center;
        return new TimbnTextWidget(plate, label);
    }

    // The plate sprite ends in a clear row one unit high, which would show as a gap under it. Its box is that much shorter and the
    // plate hangs over the bottom, so whatever comes next sits against the plate's outline.
    internal static TimbnTextWidget HangingSection(Transform parent, string text)
    {
        var box = TimbnUi.Fixed(TimbnUi.Box(parent, "Header"), height: RowHeight - _plateClearRow);
        var section = Section(box, text);
        var plate = section.Root;
        plate.anchorMin = new Vector2(0f, 1f);
        plate.anchorMax = Vector2.one;
        plate.pivot = new Vector2(0.5f, 1f);
        plate.offsetMin = new Vector2(0f, -RowHeight);
        plate.offsetMax = Vector2.zero;
        TimbnUi.Element(plate).ignoreLayout = true;
        return section;
    }

    internal static TimbnButtonWidget Button(Transform parent, string label, Action onClick) =>
        new(TimbnTheme.RedButton(parent), label, onClick);

    internal static TimbnTabsWidget Tabs(Transform parent, IReadOnlyList<string> titles, Func<int> get, Action<int> set, bool onPage = false, bool showsBumpers = false) =>
        new(TimbnTheme.TabBar(parent), titles, get, set, onPage, showsBumpers);

    /// <summary>
    /// A box to type in, on the game's value box art, with gray hint text while it is empty. It reports every change as
    /// the player types, and the game's own keys are off while it has the caret.
    /// </summary>
    internal static TMP_InputField TypeBox(Transform parent, string hint, Action<string> changed, float height = TypeBoxHeight, bool showsFrame = true)
    {
        var box = TimbnUi.Fixed(TimbnUi.Fill(TimbnUi.Box(parent, "TypeBox")), height: height);
        var back = TimbnUi.Skin(box, TimbnTheme.ValueBox, TimbnTheme.ValueBoxEdge, 0f, TimbnTheme.ValueBoxEdge, 0f);
        back.raycastTarget = true;
        var area = TimbnUi.Stretch(TimbnUi.Box(box, "Text Area"), -_textInset, 0f, -_textInset, 0f);
        // A pixel font's first letter can start a little left of the text, so the clip reaches past it.
        area.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-_textInset / 2f, 0f, 0f, 0f);
        var placeholder = TimbnUi.Stretch(TimbnUi.Text(area, TimbnTheme.Detail, hint));
        var text = TimbnUi.Stretch(TimbnUi.Text(area, TimbnTheme.Value, string.Empty));
        text.overflowMode = TextOverflowModes.Overflow;
        // A Windows path holds a backslash and r (as in repos), which TextMesh Pro would otherwise draw as a return over the
        // start of the line.
        text.parseCtrlCharacters = false;
        // A line that is typed into must not move with the letters in it. Both centering on the glyphs and the font's line
        // height change with a tall letter, but the baseline stays put.
        text.alignment = TextAlignmentOptions.BaselineLeft;
        placeholder.alignment = TextAlignmentOptions.BaselineLeft;
        foreach (var line in new[] { text, placeholder })
        {
            line.rectTransform.offsetMin = new Vector2(line.rectTransform.offsetMin.x, -_baselineDrop);
            line.rectTransform.offsetMax = new Vector2(line.rectTransform.offsetMax.x, -_baselineDrop);
        }

        var field = box.gameObject.AddComponent<TMP_InputField>();
        field.targetGraphic = back;
        field.textViewport = area;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.lineType = TMP_InputField.LineType.SingleLine;
        // Typed text such as <size=1> must stay as typed, since rich text would hide it and break the caret.
        field.richText = false;
        field.navigation = TimbnUi.NoNavigation;
        field.caretColor = text.color;
        field.customCaretColor = true;
        field.caretWidth = _caretWidth;

        // The click that focuses the box puts the caret where it lands, instead of selecting everything.
        field.onFocusSelectAll = false;
        // TextMesh Pro counts any two clicks within half a second as a double click, which catches two slow ones.
        _doubleClickDelay(field) = _doubleClickSeconds;

        // The field makes its blinking caret in OnEnable, and only when its text is already set, which it was not when the
        // component was added.
        field.enabled = false;
        field.enabled = true;
        field.onValueChanged.AddListener(value => changed(value));

        // While the box has the caret it wears the save slot's yellow frame, so it is clear typing goes there.
        if (showsFrame)
        {
            var frame = TimbnUi.Overlay(box, TimbnTheme.CardHighlight, _frameReach, _frameReach, _frameReach, _frameReach);
            frame.gameObject.SetActive(false);
            field.onSelect.AddListener(_ => frame.gameObject.SetActive(true));
            field.onDeselect.AddListener(_ => frame.gameObject.SetActive(false));
        }

        box.gameObject.AddComponent<TimbnTypeBoxInput>().Field = field;
        return field;
    }

    // The control alone, as wide as the room it is given, for a component that a field puts a title beside.
    internal static SettingParts InputRow(Transform parent)
    {
        var row = TimbnUi.Fill(TimbnUi.Row(parent, "Input"));
        var control = TimbnUi.Fill(TimbnUi.Row(row, "Control", _artGap));
        return new SettingParts(row, null, control);
    }

    // The dark box a setting shows its value in, as wide as its row leaves it.
    internal static (RectTransform Box, Image Back, TextMeshProUGUI Text) ValueBox(Transform parent, TextAlignmentOptions align)
    {
        var box = TimbnUi.Fixed(TimbnUi.Fill(TimbnUi.Box(parent, "Box")), height: BoxHeight);
        var back = TimbnUi.Skin(box, TimbnTheme.ValueBox, TimbnTheme.ValueBoxEdge, 0f, TimbnTheme.ValueBoxEdge, 0f);
        var text = TimbnUi.Stretch(TimbnUi.Text(box, TimbnTheme.Value, string.Empty), -_textInset, 0f, -_textInset, 0f);
        text.alignment = align;
        CenterInBox(text);
        return (box, back, text);
    }

    private static void CenterInBox(TextMeshProUGUI text)
    {
        var margin = text.margin;
        text.margin = new Vector4(margin.x, _letterDrop, margin.z, margin.w);
    }

    // The slider's number, after its plus.
    internal static RectTransform NumberSlot(Transform parent) => TimbnUi.Space(parent, _numberSlotWidth, RowHeight);

    // A control is as tall as the art it draws and not the box the art sits in, so the gap between two parts of a column is
    // the column's gap whatever they hold. The art keeps its own size and is centered on the box, reaching a little past
    // it. The heights are what each control draws in game units, measured by hiding every other row and differencing
    // screenshots (2 px a unit), so outlines count.
    private const float _valueArt = 17f;
    private const float _arrowArt = 17f;

    internal const float ButtonArt = 22f;

    internal static void FitValueArt(Transform row) => TimbnUi.Fixed((RectTransform)row, height: _valueArt);

    internal static void FitArrowArt(Transform row) => TimbnUi.Fixed((RectTransform)row, height: _arrowArt);

    // Makes a box clickable, with the game's plain tint while hovered.
    internal static Button Clickable(Image back, Action onClick)
    {
        back.raycastTarget = true;
        var button = back.gameObject.AddComponent<Button>();
        button.targetGraphic = back;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => onClick());
        return button;
    }

    internal readonly struct SettingParts
    {
        public SettingParts(RectTransform row, TextMeshProUGUI? name, RectTransform control)
        {
            Row = row;
            Name = name;
            Control = control;
        }

        public RectTransform Row { get; }

        public TextMeshProUGUI? Name { get; }

        public RectTransform Control { get; }
    }
}
