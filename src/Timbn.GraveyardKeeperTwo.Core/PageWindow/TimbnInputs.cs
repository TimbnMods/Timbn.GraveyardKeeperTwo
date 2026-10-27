using LazyBearTechnology;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>An On and Off switch between two arrows.</summary>
/// <param name="get">Reads whether it is on.</param>
/// <param name="set">Called with the new state.</param>
public sealed class TimbnToggle(Func<bool> get, Action<bool> set) : TimbnControl
{
    internal override TimbnWidget BuildWidget(Transform parent) =>
        new TimbnChoiceWidget(TimbnWidgets.InputRow(parent), [LLBase.L(TimbnGameIds.Texts.UiOff), LLBase.L(TimbnGameIds.Texts.UiOn)], () => get() ? 1 : 0, Guarded<int>(index => set(index == 1)));
}

/// <summary>Named options between two arrows that step through them, looping at the ends.</summary>
/// <param name="options">The options, already in the player's language.</param>
/// <param name="get">Reads the index of the current option.</param>
/// <param name="set">Called with the index of the new option.</param>
public sealed class TimbnChoice(IReadOnlyList<string> options, Func<int> get, Action<int> set) : TimbnControl
{
    internal override TimbnWidget BuildWidget(Transform parent) => new TimbnChoiceWidget(TimbnWidgets.InputRow(parent), options, get, Guarded(set));
}

/// <summary>
/// The values of an enum in the order they are declared, between two arrows. Each shows the text of a lang line keyed by
/// the enum's full type name and the value, so a mod translates them by adding lines such as
/// <c>MyMod.Size.ExtraLarge = Extra Large</c> to its lang files, and a value with no line shows its own name. It is not for
/// <c>[Flags]</c> enums, since a combination is not one option.
/// </summary>
/// <param name="get">Reads the current value.</param>
/// <param name="set">Called with the new value.</param>
public sealed class TimbnChoice<T>(Func<T> get, Action<T> set) : TimbnControl
    where T : struct, Enum
{
    // Declaration order, since Enum.GetValues puts a negative value after the positive ones.
    private readonly FieldInfo[] _fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static);

    internal override TimbnWidget BuildWidget(Transform parent)
    {
        var values = _fields.Select(field => (T)field.GetValue(null)!).ToArray();
        var names = _fields.Select(field => TimbnEnumNames.For(typeof(T), field.Name)).ToArray();
        return new TimbnChoiceWidget(TimbnWidgets.InputRow(parent), names, () => Array.IndexOf(values, get()), Guarded<int>(index => set(values[index])));
    }
}

/// <summary>
/// A color between two arrows, drawn like the character editor's color switchers. It lists no color first, then the
/// colors passed in, then Unity's basic colors (white, black, red and the rest). The color is a tag value, such as
/// <c>#FF8800</c> or <c>orange</c>.
/// </summary>
/// <param name="colors">The colors to list before Unity's.</param>
/// <param name="get">Reads the current color, or null for none.</param>
/// <param name="set">Called with the new color, or null for none.</param>
public sealed class TimbnColorPicker(IReadOnlyList<string> colors, Func<string?> get, Action<string?> set) : TimbnControl
{
    internal override TimbnWidget BuildWidget(Transform parent) => new TimbnColorWidget(TimbnWidgets.InputRow(parent), colors, get, Guarded(set));
}

/// <summary>The game's slider with a minus and a plus, and the value printed after them. Clicking the value types an exact one.</summary>
/// <param name="min">The least the slider goes to.</param>
/// <param name="max">The most the slider goes to.</param>
/// <param name="get">Reads the current value.</param>
/// <param name="set">Called with the new value.</param>
/// <param name="wholeNumbers">Whether the slider steps by whole numbers.</param>
public sealed class TimbnSlider(float min, float max, Func<float> get, Action<float> set, bool wholeNumbers = false) : TimbnControl
{
    /// <summary>
    /// How many decimal places one step covers, so 1 steps by 0.1. It is worked out from the range when left unset, with
    /// whole numbers stepping by 1. Set it before the control is put on a page.
    /// </summary>
    public int? Decimals { get; set; }

    internal override TimbnWidget BuildWidget(Transform parent) =>
        new TimbnSliderWidget(TimbnWidgets.InputRow(parent), min, max, wholeNumbers ? 0 : Decimals ?? TimbnSliderSteps.Decimals(false, min, max), get, Guarded(set), Watcher!.Logger);
}

/// <summary>
/// Text in a box to type a new value into, set when the player finishes typing. A gamepad or text too long for the box
/// opens the game's text window instead. Esc while typing works like B. It never keeps what was typed. On a page that
/// takes Back it backs out of the page. On one that doesn't it only leaves the box as it was.
/// </summary>
/// <param name="get">Reads the current text.</param>
/// <param name="set">Called with the new text.</param>
/// <param name="wholeNumbers">True to accept only whole numbers, false to accept only decimal numbers, and null for any text.</param>
public sealed class TimbnTextBox(Func<string> get, Action<string> set, bool? wholeNumbers = null) : TimbnControl
{
    /// <summary>
    /// A gray hint shown while the box is empty, already in the player's language. Set it before the control is put on a page.
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Whether the box calls its set with every letter as it is typed, and always types where it is, even with a gamepad,
    /// where it otherwise opens the game's text window. For a search box that filters as the player types, or the box
    /// in a small prompt. Set it before the control is put on a page.
    /// </summary>
    public bool SetsWhileTyping { get; set; }

    /// <summary>
    /// Whether the box shows the save slot's yellow frame around it while it has the caret, true by default. Set it to false
    /// for a box that sits between other parts and has no room for the frame, such as a search box.
    /// Set it before the control is put on a page.
    /// </summary>
    public bool ShowsFocusFrame { get; set; } = true;

    /// <summary>
    /// Whether the box has the caret as soon as the page is on screen, so the player types straight away, the way
    /// <see cref="TimbnPluginUI.ShowPrompt"/> does. A has nothing left to pick then, so the page's Select hint goes. Set it
    /// on one box of a page, before it is put on the page.
    /// </summary>
    public bool FocusWhenShown { get; set; }

    internal override TimbnWidget BuildWidget(Transform parent) =>
        new TimbnTextBoxWidget(TimbnWidgets.InputRow(parent), get, Guarded(set), wholeNumbers, Placeholder, SetsWhileTyping, FocusWhenShown, InField, ShowsFocusFrame, Watcher!.Logger);
}

/// <summary>
/// A key in a box with the Controls window's rebind button. Clicking either waits for the next key, with any held Ctrl,
/// Shift or Alt kept with it, Backspace clears it and Esc leaves it.
/// </summary>
/// <param name="get">Reads the current key.</param>
/// <param name="set">Called with the new key.</param>
public sealed class TimbnKey(Func<KeyboardShortcut> get, Action<KeyboardShortcut> set) : TimbnControl
{
    internal override TimbnWidget BuildWidget(Transform parent)
    {
        TimbnKeyWidget? widget = null;
        widget = new TimbnKeyWidget(
            TimbnWidgets.InputRow(parent),
            () => TimbnKeyCapture.KeyText(get()),
            () => TimbnKeyCapture.Instance.Begin(Guarded(set), widget!));
        return widget;
    }
}

/// <summary>Text in a box that cannot be changed, for showing a value.</summary>
/// <param name="get">Reads the text to show.</param>
public sealed class TimbnValue(Func<string> get) : TimbnControl
{
    internal override TimbnWidget BuildWidget(Transform parent) => new TimbnBoxWidget(TimbnWidgets.InputRow(parent), get);
}

/// <summary>The game's brown tab bar, with exactly one tab chosen, paging with arrows when they do not all fit.</summary>
/// <param name="titles">The tabs, already in the player's language.</param>
/// <param name="get">Reads the index of the chosen tab.</param>
/// <param name="set">Called with the index of the tab the player picked.</param>
public sealed class TimbnTabs(IReadOnlyList<string> titles, Func<int> get, Action<int> set) : TimbnControl
{
    /// <summary>
    /// Whether the bar draws the bumpers' glyphs at its ends while a gamepad is in use, where they step it, so the page's
    /// hint bar leaves them out. LB and RB step the bar either way.
    /// </summary>
    public bool ShowsBumpers { get; set; }

    internal override TimbnWidget BuildWidget(Transform parent) => TimbnWidgets.Tabs(parent, titles, get, Guarded(set), onPage: true, ShowsBumpers);
}
