using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Lays children out side by side and starts a new line when the next one does not fit. Unity sets every width before it
/// asks for heights, so the width is known when the group works out how many lines it needs, the same way wrapped text
/// gets its height. The group is no wider than its widest child at the least, so a long row cannot push its parent wider.
/// </summary>
internal sealed class TimbnFlowLayoutGroup : LayoutGroup
{
    /// <summary>The space between children on a line and between lines.</summary>
    internal float Spacing { get; set; }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        var widest = 0f;
        var total = 0f;
        foreach (var child in rectChildren)
        {
            widest = Mathf.Max(widest, LayoutUtility.GetMinSize(child, 0));
            total += LayoutUtility.GetPreferredSize(child, 0) + Spacing;
        }

        total = Mathf.Max(0f, total - Spacing);
        SetLayoutInputForAxis(widest + padding.horizontal, total + padding.horizontal, 0f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        var lines = Measure(rectTransform.rect.width - padding.horizontal);
        var height = padding.vertical + (Spacing * Mathf.Max(0, lines.Count - 1));
        foreach (var line in lines)
            height += line.Height;

        SetLayoutInputForAxis(height, height, 0f, 1);
    }

    public override void SetLayoutHorizontal()
    {
        var available = rectTransform.rect.width - padding.horizontal;
        foreach (var line in Measure(available))
        {
            // A line wider than the room, from a child with a least width past it, starts at the padding instead of left of it.
            var x = padding.left + (Mathf.Max(0f, available - line.Width) * GetAlignmentOnAxis(0));
            foreach (var (child, width) in line.Children)
            {
                SetChildAlongAxis(child, 0, x, width);
                x += width + Spacing;
            }
        }
    }

    public override void SetLayoutVertical()
    {
        var y = (float)padding.top;
        foreach (var line in Measure(rectTransform.rect.width - padding.horizontal))
        {
            foreach (var (child, _) in line.Children)
            {
                var height = LayoutUtility.GetPreferredSize(child, 1);
                SetChildAlongAxis(child, 1, y + ((line.Height - height) * GetAlignmentOnAxis(1)), height);
            }

            y += line.Height + Spacing;
        }
    }

    private List<Line> Measure(float available)
    {
        var lines = new List<Line>();
        Line? line = null;
        foreach (var child in rectChildren)
        {
            var width = Mathf.Max(LayoutUtility.GetMinSize(child, 0), Mathf.Min(LayoutUtility.GetPreferredSize(child, 0), available));
            if (line == null || line.Width + Spacing + width > available)
                lines.Add(line = new Line());

            line.Add(child, width, LayoutUtility.GetPreferredSize(child, 1), Spacing);
        }

        return lines;
    }

    private sealed class Line
    {
        internal List<(RectTransform Child, float Width)> Children { get; } = [];

        internal float Width { get; private set; }

        internal float Height { get; private set; }

        internal void Add(RectTransform child, float width, float height, float spacing)
        {
            Width += (Children.Count > 0 ? spacing : 0f) + width;
            Height = Mathf.Max(Height, height);
            Children.Add((child, width));
        }
    }
}
