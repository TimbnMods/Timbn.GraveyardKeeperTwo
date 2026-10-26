using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Picks where the gamepad goes from a control, by the boxes the controls take on screen rather than their middles, so a
/// page laid out in columns and cards moves the way it reads. Up and down go to the nearest line of controls above or
/// below, and in that line to the leftmost one sharing some of the current control's width, else the one nearest
/// across. Left and right only move along the same line.
/// </summary>
internal static class TimbnNavigationPicker
{
    // Boxes that touch or overlap by a hair still count as above or below each other.
    private const float _touch = 2f;

    internal static int Pick(Rect from, IReadOnlyList<Rect> boxes, GUIDirection direction) => direction switch
    {
        GUIDirection.Down => Vertical(from, boxes, below: true),
        GUIDirection.Up => Vertical(from, boxes, below: false),
        GUIDirection.Left => Sideways(from, boxes, right: false),
        GUIDirection.Right => Sideways(from, boxes, right: true),
        _ => -1,
    };

    private static int Vertical(Rect from, IReadOnlyList<Rect> boxes, bool below)
    {
        var candidates = new List<int>();
        for (var i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            if (below ? box.yMax <= from.yMin + _touch : box.yMin >= from.yMax - _touch)
                candidates.Add(i);
        }

        if (candidates.Count == 0)
            return -1;

        // The nearest line is every control that reaches into the closest one's height, so columns of different heights
        // side by side count as one line, even when they only touch at an edge. One that touches the edge of a control
        // directly above or below it is the next line instead, such as a button right under a tall cell.
        var nearest = boxes[candidates.OrderBy(i => Gap(from, boxes[i], below)).First()];
        var line = candidates.Where(i => SameLine(nearest, boxes[i], below));

        // In that line, the leftmost control sharing some of the current one's width, the nearest first when two start
        // together, else the one nearest across.
        return line
            .OrderBy(i => Overlap(from, boxes[i]) > _touch ? 0 : 1)
            .ThenBy(i => Overlap(from, boxes[i]) > _touch ? boxes[i].xMin : Mathf.Abs(boxes[i].center.x - from.center.x))
            .ThenBy(i => Gap(from, boxes[i], below))
            .First();
    }

    private static bool SameLine(Rect nearest, Rect box, bool below)
    {
        var reaches = below ? box.yMax > nearest.yMin + _touch : box.yMin < nearest.yMax - _touch;
        var touches = below ? box.yMax >= nearest.yMin - _touch : box.yMin <= nearest.yMax + _touch;
        return reaches || touches && Overlap(nearest, box) <= _touch;
    }

    private static float Overlap(Rect from, Rect box) => Mathf.Min(box.xMax, from.xMax) - Mathf.Max(box.xMin, from.xMin);

    private static float Gap(Rect from, Rect box, bool below) => below ? from.yMin - box.yMax : box.yMin - from.yMax;

    private static int Sideways(Rect from, IReadOnlyList<Rect> boxes, bool right)
    {
        var best = -1;
        var bestGap = float.MaxValue;
        for (var i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            var overlaps = Mathf.Min(box.yMax, from.yMax) - Mathf.Max(box.yMin, from.yMin) > _touch;
            var gap = right ? box.xMin - from.xMax : from.xMin - box.xMax;
            if (!overlaps || gap < -_touch || gap >= bestGap)
                continue;

            best = i;
            bestGap = gap;
        }

        return best;
    }
}
