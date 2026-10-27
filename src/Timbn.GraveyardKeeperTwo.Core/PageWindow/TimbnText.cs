using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A line of text in one of the game's styles. One line ends in "..." when it runs out of room, and wrapped text grows to
/// as many lines as it needs. In a column it keeps a small space under itself, so lines of text stacked in a column do not
/// touch. Its first letter starts on the box's left edge, like the art of every other component in the column.
/// </summary>
/// <example>
/// <code>
/// page.Add(new TimbnText("Pick a color for the chest's name.") { Wrap = true });
/// page.Row(row =>
/// {
///     row.Add(new TimbnText("Chest Labels", TimbnTextKind.Name));
///     row.Add(new TimbnText("1.1.0", TimbnTextKind.Detail));
/// });
/// </code>
/// </example>
/// <param name="text">The text, already in the player's language.</param>
/// <param name="kind">How the text looks. It is a label by default.</param>
public sealed class TimbnText(string text, TimbnTextKind kind = TimbnTextKind.Label) : TimbnComponent
{
    /// <summary>Whether a long line wraps onto more lines. Set it before the text is put on a page.</summary>
    public bool Wrap { get; set; }

    /// <summary>Where the text sits across its line, which shows on wrapped text and on a line given a width. Set it before the text is put on a page.</summary>
    public TimbnAlign Align { get; set; } = TimbnAlign.Start;

    /// <summary>How big the line is, or null for the style's own size. Set it before the text is put on a page.</summary>
    public TimbnTextSize? Size { get; set; }

    /// <summary>
    /// A share of the room the box's parent has left along its direction, by weight, or null for none. Its own width
    /// counts for nothing, so long text cannot take more than its share. Set it before the text is put on a page.
    /// </summary>
    public float? Fill { get; set; }

    /// <summary>A fixed width, or null to size from the text. Set it before the text is put on a page.</summary>
    public float? Width { get; set; }

    internal override bool LeavesLineGap => true;

    protected internal override RectTransform Build(Transform parent)
    {
        var line = TimbnUi.Text(parent, Style(kind), text, Wrap, Size is { } step ? (float)step : null);

        // Centered on the font's own line and not on the letters, so a line with a descender (the p in Keeper) sits where one
        // without does, and lines side by side share a baseline.
        line.alignment = Align switch
        {
            TimbnAlign.Center => TextAlignmentOptions.Center,
            TimbnAlign.End => TextAlignmentOptions.Right,
            _ => TextAlignmentOptions.Left,
        };
        var box = line.rectTransform;
        if (Width is { } fixedWidth)
            TimbnUi.Fixed(box, fixedWidth);

        TimbnBox.Share(box, Fill, InColumn);

        if (Align == TimbnAlign.Start)
            TimbnTextFit.AlignLeftEdges([line]);

        return box;
    }

    private static TimbnTextStyle Style(TimbnTextKind kind) => kind switch
    {
        TimbnTextKind.Name => TimbnTheme.Name,
        TimbnTextKind.Detail => TimbnTheme.Detail,
        TimbnTextKind.Value => TimbnTheme.Value,
        TimbnTextKind.Title => TimbnTheme.Title,
        _ => TimbnTheme.Label,
    };
}
