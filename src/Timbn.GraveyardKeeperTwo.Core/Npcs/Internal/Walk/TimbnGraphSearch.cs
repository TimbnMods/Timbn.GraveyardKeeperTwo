namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnGraphSearch
{
    internal static List<TPoint>? Route<TPoint>(TPoint start, TPoint goal, Func<TPoint, IEnumerable<TPoint>> next, Func<TPoint, TPoint, float> cost, out float length)
        where TPoint : class
    {
        length = 0f;
        var best = new Dictionary<TPoint, float> { [start] = 0f };
        var previous = new Dictionary<TPoint, TPoint>();
        var done = new HashSet<TPoint>();
        var open = new List<TPoint> { start };
        while (open.Count > 0)
        {
            var current = open[0];
            foreach (var point in open)
            {
                if (best[point] < best[current])
                    current = point;
            }

            open.Remove(current);
            if (!done.Add(current))
                continue;

            if (ReferenceEquals(current, goal))
                break;

            foreach (var neighbor in next(current))
            {
                if (neighbor is null || done.Contains(neighbor))
                    continue;

                var step = best[current] + cost(current, neighbor);
                if (best.TryGetValue(neighbor, out var known) && known <= step)
                    continue;

                best[neighbor] = step;
                previous[neighbor] = current;
                open.Add(neighbor);
            }
        }

        if (!ReferenceEquals(start, goal) && !previous.ContainsKey(goal))
            return null;

        List<TPoint> route = [goal];
        while (previous.TryGetValue(route[^1], out var before))
            route.Add(before);

        route.Reverse();
        length = best[goal];
        return route;
    }
}
