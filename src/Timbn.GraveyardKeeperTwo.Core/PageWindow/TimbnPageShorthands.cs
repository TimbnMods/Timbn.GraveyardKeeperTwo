using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One call shorthands for Core's containers, each the same as putting the container on with
/// <see cref="TimbnPageBuilder.Add{T}(T)"/>. Every container Core ships has one, since pages use them all the time. A mod
/// gives its own container a shorthand the same way, with an extension method on <see cref="TimbnPageBuilder"/>.
/// </summary>
/// <example>
/// <code>
/// public static void Framed(this TimbnPageBuilder page, Action&lt;TimbnPageBuilder&gt; build) =>
///     page.Add(new Framed(new TimbnColumn(build)));
/// </code>
/// </example>
public static class TimbnPageShorthands
{
    /// <summary>Puts a <see cref="TimbnRow"/> on the page, laid out from the start. The parameters are its properties.</summary>
    /// <param name="page">The page or box to add to.</param>
    /// <param name="build">Fills the row.</param>
    /// <param name="gap">The space between what is in it.</param>
    /// <param name="fill">A share of the room the row's parent has left along its direction, by weight.</param>
    /// <param name="width">A fixed width, or null to size from what is in it.</param>
    /// <param name="height">A fixed height, or null to size from what is in it.</param>
    /// <param name="visible">Shows or hides the row, as a value or a callback that is read all the time, or null to always show it.</param>
    /// <param name="minHeight">The least height the row has.</param>
    /// <param name="stretch">Whether what is in the row is as tall as the row.</param>
    /// <param name="verticalAlign">Where what is shorter than the row sits.</param>
    /// <param name="wrap">Whether what does not fit moves to a new line under it.</param>
    public static void Row(this TimbnPageBuilder page, Action<TimbnPageBuilder> build, float gap = TimbnPageBuilder.RowGap, float? fill = null, float? width = null, float? height = null, TimbnBinding<bool>? visible = null, float? minHeight = null, bool stretch = false, TimbnAlign verticalAlign = TimbnAlign.Center, bool wrap = false) =>
        page.Row(TimbnAlign.Start, build, gap, fill, width, height, visible, minHeight, stretch, verticalAlign, wrap);

    /// <summary>Puts a <see cref="TimbnRow"/> on the page, laid out where <paramref name="align"/> says. The parameters are its properties.</summary>
    /// <param name="page">The page or box to add to.</param>
    /// <param name="align">Where what is in the row goes when it does not fill it.</param>
    /// <param name="build">Fills the row.</param>
    /// <param name="gap">The space between what is in it.</param>
    /// <param name="fill">A share of the room the row's parent has left along its direction, by weight.</param>
    /// <param name="width">A fixed width, or null to size from what is in it.</param>
    /// <param name="height">A fixed height, or null to size from what is in it.</param>
    /// <param name="visible">Shows or hides the row, as a value or a callback that is read all the time, or null to always show it.</param>
    /// <param name="minHeight">The least height the row has.</param>
    /// <param name="stretch">Whether what is in the row is as tall as the row.</param>
    /// <param name="verticalAlign">Where what is shorter than the row sits.</param>
    /// <param name="wrap">Whether what does not fit moves to a new line under it.</param>
    public static void Row(this TimbnPageBuilder page, TimbnAlign align, Action<TimbnPageBuilder> build, float gap = TimbnPageBuilder.RowGap, float? fill = null, float? width = null, float? height = null, TimbnBinding<bool>? visible = null, float? minHeight = null, bool stretch = false, TimbnAlign verticalAlign = TimbnAlign.Center, bool wrap = false) =>
        page.Add(Shown(new TimbnRow(build) { Gap = gap, Align = align, VerticalAlign = verticalAlign, MinHeight = minHeight, Stretch = stretch, Wrap = wrap, Fill = fill, Width = width, Height = height }, visible));

    /// <summary>Puts a <see cref="TimbnColumn"/> on the page. The parameters are its properties.</summary>
    /// <param name="page">The page or box to add to.</param>
    /// <param name="build">Fills the column.</param>
    /// <param name="gap">The space between what is in it, or null for the page's own.</param>
    /// <param name="fill">A share of the room the column's parent has left along its direction, by weight.</param>
    /// <param name="width">A fixed width, or null to size from what is in it.</param>
    /// <param name="height">A fixed height, or null to size from what is in it.</param>
    /// <param name="visible">Shows or hides the column, as a value or a callback that is read all the time, or null to always show it.</param>
    /// <param name="dimmed">While it is true a gray veil covers the column, or null for none.</param>
    /// <param name="brown">Whether the Inspirations page's brown stone is drawn behind the column.</param>
    /// <param name="backdrop">A flat color drawn behind the column, or null for none.</param>
    /// <param name="panel">Whether the Save and Load window's dark navy panel is drawn behind the column.</param>
    public static void Column(this TimbnPageBuilder page, Action<TimbnPageBuilder> build, float? gap = null, float? fill = null, float? width = null, float? height = null, TimbnBinding<bool>? visible = null, TimbnBinding<bool>? dimmed = null, bool brown = false, Color? backdrop = null, bool panel = false) =>
        page.Add(Shown(new TimbnColumn(build) { Gap = gap, Fill = fill, Width = width, Height = height, Dimmed = dimmed, Brown = brown, Backdrop = backdrop, Panel = panel }, visible));

    /// <summary>Puts a <see cref="TimbnCard"/> holding a column with no gap on the page.</summary>
    /// <param name="page">The page or box to add to.</param>
    /// <param name="build">Fills the card's column.</param>
    /// <param name="fill">A share of the room the card's parent has left along its direction, by weight.</param>
    /// <param name="width">A fixed width, or null to size from what is in it.</param>
    /// <param name="height">A fixed height, or null to size from what is in it.</param>
    /// <param name="visible">Shows or hides the card, as a value or a callback that is read all the time, or null to always show it.</param>
    public static void Card(this TimbnPageBuilder page, Action<TimbnPageBuilder> build, float? fill = null, float? width = null, float? height = null, TimbnBinding<bool>? visible = null) =>
        page.Add(Shown(new TimbnCard(new TimbnColumn(build) { Gap = 0f }) { Fill = fill, Width = width, Height = height }, visible));

    /// <summary>Puts a <see cref="TimbnScroll"/> on the page.</summary>
    /// <param name="page">The page or box to add to.</param>
    /// <param name="build">Fills the column that scrolls.</param>
    /// <param name="bleed">How far the scroll area reaches past the sides of what it holds.</param>
    /// <param name="visible">Shows or hides the scroll area, as a value or a callback that is read all the time, or null to always show it.</param>
    public static void Scroll(this TimbnPageBuilder page, Action<TimbnPageBuilder> build, float bleed = 0f, TimbnBinding<bool>? visible = null) =>
        page.Add(Shown(new TimbnScroll(build) { Bleed = bleed }, visible));

    /// <summary>Puts a <see cref="TimbnRegion"/> on the page and hands it back to rebuild.</summary>
    /// <param name="page">The page or box to add to.</param>
    /// <param name="build">Builds the region's content.</param>
    /// <param name="fill">A share of the room the region's parent has left along its direction, by weight.</param>
    /// <param name="width">A fixed width, or null to size from what is in it.</param>
    /// <param name="height">A fixed height, or null to size from what is in it.</param>
    /// <param name="visible">Shows or hides the region, as a value or a callback that is read all the time, or null to always show it.</param>
    /// <param name="gap">The space between what is in it, or null for the page's own.</param>
    /// <returns>The region, to rebuild when what it shows changes.</returns>
    public static TimbnRegion Region(this TimbnPageBuilder page, Action<TimbnPageBuilder> build, float? fill = null, float? width = null, float? height = null, TimbnBinding<bool>? visible = null, float? gap = null) =>
        page.Add(Shown(new TimbnRegion(build) { Gap = gap, Fill = fill, Width = width, Height = height }, visible));

    /// <summary>Puts a <see cref="TimbnSplit"/> on the page. The parameters are its own.</summary>
    /// <param name="page">The page or box to add to.</param>
    /// <param name="leftWidth">The width of the left column, not counting the strip its divider takes.</param>
    /// <param name="leftTitle">The left plate's text, already in the player's language.</param>
    /// <param name="left">Builds the left column.</param>
    /// <param name="right">Builds the right column.</param>
    /// <param name="top">Builds a line above the right column, or null for none.</param>
    /// <param name="topLeft">Builds a line above the left column, or null for none.</param>
    /// <param name="rightTitle">The right plate's text, or null when <paramref name="rightHeader"/> fills it.</param>
    /// <param name="rightHeader">Builds what goes on the right plate in place of a title.</param>
    /// <param name="leftKey">The key whose glyph shows at the left end of the left plate, or null for none.</param>
    /// <param name="rightKey">The key whose glyph shows at the right end of the left plate, or null for none.</param>
    /// <param name="rightDimmed">While it is true a gray veil covers the right column, or null for none.</param>
    /// <param name="visible">Shows or hides the split, as a value or a callback that is read all the time, or null to always show it.</param>
    public static void Split(this TimbnPageBuilder page, float leftWidth, string leftTitle, Action<TimbnPageBuilder> left, Action<TimbnPageBuilder> right, Action<TimbnPageBuilder>? top = null, Action<TimbnPageBuilder>? topLeft = null, string? rightTitle = null, Action<TimbnPageBuilder>? rightHeader = null, GameKey? leftKey = null, GameKey? rightKey = null, TimbnBinding<bool>? rightDimmed = null, TimbnBinding<bool>? visible = null) =>
        page.Add(Shown(new TimbnSplit(leftWidth, leftTitle, left, right) { Top = top, TopLeft = topLeft, RightTitle = rightTitle, RightHeader = rightHeader, LeftKey = leftKey, RightKey = rightKey, RightDimmed = rightDimmed }, visible));

    private static T Shown<T>(T component, TimbnBinding<bool>? visible)
        where T : TimbnComponent
    {
        if (visible is { } shown)
            component.Visible = shown;

        return component;
    }
}
