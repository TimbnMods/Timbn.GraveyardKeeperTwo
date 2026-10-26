using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Two columns side by side under a header the way the Character window's Inspirations page has them, a copy of the game's
/// own rails with a plate over each column and the silver divider between them running down to the frame. Put it on an
/// <see cref="TimbnPage.EdgeToEdge"/> page, where the rails reach the frame. Each column sits on the dark panel of the
/// Save and Load window, and the left one is a fixed width with the divider beside it.
/// <see cref="TimbnPageShorthands.Split"/> puts one on a page in a single call.
/// </summary>
/// <param name="leftWidth">The width of the left column, not counting the strip its divider takes.</param>
/// <param name="leftTitle">The left plate's text, already in the player's language.</param>
/// <param name="left">Builds the left column.</param>
/// <param name="right">Builds the right column.</param>
public sealed class TimbnSplit(float leftWidth, string leftTitle, Action<TimbnPageBuilder> left, Action<TimbnPageBuilder> right) : TimbnComponent
{
    /// <summary>Builds a line above the right column, such as the name of what the columns show, or null for none.</summary>
    public Action<TimbnPageBuilder>? Top { get; set; }

    /// <summary>Builds a line above the left column, level with <see cref="Top"/>, or null for none.</summary>
    public Action<TimbnPageBuilder>? TopLeft { get; set; }

    /// <summary>The right plate's text, or null when <see cref="RightHeader"/> fills it.</summary>
    public string? RightTitle { get; set; }

    /// <summary>Builds what goes on the right plate, such as a <see cref="TimbnTabs"/> bar, in place of a title.</summary>
    public Action<TimbnPageBuilder>? RightHeader { get; set; }

    /// <summary>The key whose glyph shows at the left end of the left plate with a gamepad, or null for none.</summary>
    public GameKey? LeftKey { get; set; }

    /// <summary>The key whose glyph shows at the right end of the left plate with a gamepad, or null for none.</summary>
    public GameKey? RightKey { get; set; }

    /// <summary>A value or a callback that is read all the time, and while it is true a gray veil covers the right column, or null for none.</summary>
    public TimbnBinding<bool>? RightDimmed { get; set; }

    protected internal override RectTransform Build(Transform parent)
    {
        // Everything stacks with no gap, so the page's own gap between lines does not come between the header and its columns.
        // It takes the room a page of a set height has left, and is as tall as its columns inside a scroll.
        var box = TimbnUi.Column(parent, "Split", 0f);
        TimbnUi.Element(box).flexibleHeight = 1f;
        var inside = BuilderFor(box);
        if (Top != null || TopLeft != null)
        {
            inside.Row(
                row =>
                {
                    if (TopLeft != null)
                        LeftColumn(row, new TimbnColumn(TopLeft) { Gap = 0f });
                    else
                        row.Add(new TimbnSpace(width: leftWidth + TimbnWidgets.LineStrip));

                    if (Top != null)
                        row.Column(Top, gap: 0f, fill: 1f);
                },
                gap: 0f);
            inside.Add(new TimbnSpace(height: TimbnWidgets.RailsAbove));
        }

        var header = TimbnWidgets.SplitHeader(box, leftTitle, RightTitle, leftWidth, RightHeader != null);
        if (LeftKey is not null || RightKey is not null)
            TimbnWidgets.SectionKeys(header.LeftPlate, LeftKey, RightKey, Watcher!);

        if (header.RightSlot != null)
            RightHeader!(BuilderFor(header.RightSlot));

        var columns = inside.Add(new TimbnRow(row =>
        {
            // The d-pad and stick keep to one column, so the player moves between them with A and B instead.
            LeftColumn(row, new TimbnColumn(left) { Gap = 0f, Panel = true }).gameObject.AddComponent<TimbnNavigationZone>();
            row.Add(new TimbnColumn(right) { Gap = 0f, Fill = 1f, Panel = true, Dimmed = RightDimmed });
        })
        {
            Gap = 0f,
            Stretch = true,
        });
        TimbnUi.Element(columns.Root!).flexibleHeight = 1f;
        header.Divider.Columns = columns.Root;
        return box;
    }

    // The left column keeps the divider's strip clear on its right.
    private RectTransform LeftColumn(TimbnPageBuilder row, TimbnColumn column)
    {
        column.Width = leftWidth + TimbnWidgets.LineStrip;
        var box = row.Add(column).Root!;
        box.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding.right = (int)TimbnWidgets.LineStrip;
        return box;
    }
}
