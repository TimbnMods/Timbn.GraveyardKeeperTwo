namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's scroll area with its scrollbar, filling the box it is in. What is added grows a column that scrolls once it
/// is taller than the area, so a popup that holds one needs a height of its own.
/// <see cref="TimbnPageShorthands.Scroll"/> puts one on a page in a single call.
/// </summary>
/// <param name="build">Fills the column that scrolls.</param>
public sealed class TimbnScroll(Action<TimbnPageBuilder> build) : TimbnComponent
{
    /// <summary>
    /// How far the scroll area reaches past the sides of what it holds, so something that hangs out of the content, such as
    /// the end posts of a header's rails, is not cut off at the edge. The content keeps its width.
    /// </summary>
    public float Bleed { get; set; }

    protected internal override RectTransform Build(Transform parent)
    {
        var content = TimbnUi.Scroll(parent, out var scroll);
        TimbnUi.FillHeight(scroll);
        if (Bleed > 0f && scroll.viewport is { } viewport)
        {
            var reach = Bleed + TimbnUi.MaskClip;
            viewport.offsetMin = new Vector2(-reach, viewport.offsetMin.y);
            viewport.offsetMax = new Vector2(reach, viewport.offsetMax.y);
            var padding = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding;
            padding.left = padding.right = (int)reach;
        }

        build(BuilderFor(content));
        return (RectTransform)scroll.transform;
    }
}
