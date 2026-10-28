namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A box that lays out what is put in it side by side, each at its own size unless it asks for a share with
/// <see cref="TimbnBox.Fill"/>. <see cref="TimbnPageShorthands.Row(TimbnPageBuilder, Action{TimbnPageBuilder}, float, float?, float?, float?, TimbnBinding{bool}?, float?, bool, TimbnAlign, bool)"/>
/// puts one on a page in a single call.
/// </summary>
/// <param name="build">Fills the row.</param>
public sealed class TimbnRow(Action<TimbnPageBuilder> build) : TimbnBox
{
    /// <summary>The space between what is in it.</summary>
    public float Gap { get; set; } = TimbnPageBuilder.RowGap;

    /// <summary>Where what is in the row goes when it does not fill it, at the start by default.</summary>
    public TimbnAlign Align { get; set; } = TimbnAlign.Start;

    /// <summary>
    /// Where what is shorter than the row sits, centered by default, or at the top with <see cref="TimbnAlign.Start"/> so the
    /// first lines of columns side by side line up.
    /// </summary>
    public TimbnAlign VerticalAlign { get; set; } = TimbnAlign.Center;

    /// <summary>The least height the row has, with any room left under what is in it, or null for none.</summary>
    public float? MinHeight { get; set; }

    /// <summary>Whether what is in the row is as tall as the row, so a column in it reaches the bottom, instead of being centered at its own height.</summary>
    public bool Stretch { get; set; }

    /// <summary>
    /// Whether what does not fit on a line moves to a new line under it, so the row is never wider than the room it has
    /// and gets taller instead. <see cref="Gap"/> is then also the space between lines, <see cref="Align"/> places each
    /// line, and <see cref="Stretch"/> and <see cref="TimbnBox.Fill"/> on what is in the row have no effect. A child wider
    /// than the row still overflows it. False by default. Set it before the row is put on a page.
    /// </summary>
    public bool Wrap { get; set; }

    protected internal override RectTransform Build(Transform parent)
    {
        var anchor = TimbnPageBuilder.Anchor(Align, VerticalAlign);
        var box = Wrap ? TimbnUi.Flow(parent, "Row", Gap, anchor) : TimbnUi.Row(parent, "Row", Gap, align: anchor, stretch: Stretch);
        Place(box);
        if (MinHeight is { } least)
            TimbnUi.Element(box).minHeight = least;

        build(BuilderFor(box));
        return box;
    }
}
