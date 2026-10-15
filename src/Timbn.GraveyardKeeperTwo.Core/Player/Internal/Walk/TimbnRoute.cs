namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A walk's route measured along its length as seen from above. The player's steering finds how far along it they are
/// and aims a little ahead of that, and a stretch the player does not fit through is swapped for a detour.
/// </summary>
internal sealed class TimbnRoute
{
    private const float _samePoint = 0.001f;

    private readonly List<Vector3> _points = [];
    private readonly List<float> _along = [];

    internal TimbnRoute(IEnumerable<Vector3> points)
    {
        foreach (var point in points)
        {
            if (_points.Count > 0 && Flat(_points[^1], point) < _samePoint)
            {
                _points[^1] = point;
                continue;
            }

            _along.Add(_points.Count == 0 ? 0f : _along[^1] + Flat(_points[^1], point));
            _points.Add(point);
        }
    }

    internal IReadOnlyList<Vector3> Points => _points;

    internal float Length => _along.Count > 0 ? _along[^1] : 0f;

    /// <summary>The point <paramref name="distance"/> along the route, held to its two ends.</summary>
    internal Vector3 PointAt(float distance)
    {
        if (_points.Count == 0)
            return Vector3.zero;

        if (distance <= 0f)
            return _points[0];

        if (distance >= Length)
            return _points[^1];

        var segment = SegmentAt(distance);
        var span = _along[segment + 1] - _along[segment];
        return Vector3.Lerp(_points[segment], _points[segment + 1], span > 0f ? (distance - _along[segment]) / span : 0f);
    }

    /// <summary>The way the route heads <paramref name="distance"/> along it, flat and of length one, or zero for a route of one point.</summary>
    internal Vector3 HeadingAt(float distance)
    {
        if (_points.Count < 2)
            return Vector3.zero;

        var segment = SegmentAt(Mathf.Clamp(distance, 0f, Length));
        var heading = _points[segment + 1] - _points[segment];
        heading.y = 0f;
        return heading.normalized;
    }

    /// <summary>
    /// How far along the route the point nearest <paramref name="position"/> is, looking only between
    /// <paramref name="from"/> and <paramref name="window"/> past it, so a route that doubles back is not skipped ahead.
    /// </summary>
    internal float Project(Vector3 position, float from, float window)
    {
        if (_points.Count < 2)
            return 0f;

        from = Mathf.Clamp(from, 0f, Length);
        var to = Mathf.Min(Length, from + window);
        var best = from;
        var bestGap = float.MaxValue;
        var flat = new Vector2(position.x, position.z);
        for (var i = SegmentAt(from); i < _points.Count - 1 && _along[i] <= to; i++)
        {
            var start = new Vector2(_points[i].x, _points[i].z);
            var end = new Vector2(_points[i + 1].x, _points[i + 1].z);
            var span = end - start;
            var length = span.sqrMagnitude;
            var t = length > 0f ? Mathf.Clamp01(Vector2.Dot(flat - start, span) / length) : 0f;
            var distance = Mathf.Clamp(_along[i] + t * (_along[i + 1] - _along[i]), from, to);
            var nearest = PointAt(distance);
            var gap = Vector2.Distance(flat, new Vector2(nearest.x, nearest.z));
            if (gap >= bestGap)
                continue;

            bestGap = gap;
            best = distance;
        }

        return best;
    }

    /// <summary>The route from <paramref name="from"/> to <paramref name="to"/> along it, both ends included.</summary>
    internal List<Vector3> Slice(float from, float to)
    {
        from = Mathf.Clamp(from, 0f, Length);
        to = Mathf.Clamp(to, from, Length);
        List<Vector3> slice = [PointAt(from)];
        for (var i = 0; i < _points.Count; i++)
        {
            if (_along[i] > from && _along[i] < to)
                slice.Add(_points[i]);
        }

        slice.Add(PointAt(to));
        return slice;
    }

    /// <summary>
    /// This route with the stretch from <paramref name="from"/> to <paramref name="to"/> along it replaced by
    /// <paramref name="detour"/>, which starts and ends where the stretch does, or near it.
    /// </summary>
    internal TimbnRoute Splice(float from, float to, IReadOnlyList<Vector3> detour)
    {
        List<Vector3> points = [];
        if (from > 0f)
        {
            var before = Slice(0f, from);
            points.AddRange(before.Take(before.Count - 1));
        }

        points.AddRange(detour);
        if (to < Length)
            points.AddRange(Slice(to, Length).Skip(1));

        return new TimbnRoute(points);
    }

    /// <summary>
    /// This route with only the points it needs to keep its shape, dropping each one that lies within
    /// <paramref name="tolerance"/> of the straight line past it, across or in height.
    /// </summary>
    internal TimbnRoute Simplified(float tolerance)
    {
        if (_points.Count < 3)
            return this;

        var keep = new bool[_points.Count];
        keep[0] = true;
        keep[^1] = true;
        var spans = new Stack<(int From, int To)>();
        spans.Push((0, _points.Count - 1));
        while (spans.Count > 0)
        {
            (var from, var to) = spans.Pop();
            var worst = -1;
            var widest = tolerance;
            for (var i = from + 1; i < to; i++)
            {
                var gap = Gap(_points[i], _points[from], _points[to]);
                if (gap <= widest)
                    continue;

                widest = gap;
                worst = i;
            }

            if (worst < 0)
                continue;

            keep[worst] = true;
            spans.Push((from, worst));
            spans.Push((worst, to));
        }

        return new TimbnRoute(_points.Where((_, i) => keep[i]));
    }

    /// <summary>
    /// Groups the distances along a route where the player does not fit, in order, into stretches from the first to the
    /// last of each group. Two that are less than <paramref name="gap"/> apart join into one.
    /// </summary>
    internal static List<(float From, float To)> Stretches(IReadOnlyList<float> blocked, float gap)
    {
        List<(float From, float To)> stretches = [];
        foreach (var distance in blocked)
        {
            if (stretches.Count > 0 && distance - stretches[^1].To < gap)
            {
                stretches[^1] = (stretches[^1].From, distance);
                continue;
            }

            stretches.Add((distance, distance));
        }

        return stretches;
    }

    private int SegmentAt(float distance)
    {
        for (var i = 0; i < _points.Count - 2; i++)
        {
            if (distance < _along[i + 1])
                return i;
        }

        return Mathf.Max(0, _points.Count - 2);
    }

    /// <summary>How far <paramref name="point"/> is from the line from <paramref name="start"/> to <paramref name="end"/>, across seen from above or in height, whichever is more.</summary>
    private static float Gap(Vector3 point, Vector3 start, Vector3 end)
    {
        var flat = new Vector2(point.x, point.z);
        var a = new Vector2(start.x, start.z);
        var span = new Vector2(end.x, end.z) - a;
        var length = span.sqrMagnitude;
        var t = length > 0f ? Mathf.Clamp01(Vector2.Dot(flat - a, span) / length) : 0f;
        var across = Vector2.Distance(flat, a + span * t);
        var height = Mathf.Abs(point.y - Mathf.Lerp(start.y, end.y, t));
        return Mathf.Max(across, height);
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        var x = a.x - b.x;
        var z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }
}
