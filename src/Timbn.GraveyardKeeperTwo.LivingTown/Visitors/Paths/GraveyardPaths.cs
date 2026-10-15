using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.LivingTown;

// The graveyard's paths as straight lines read from the scene's road tiles, since neither navmesh marks them.
internal sealed class GraveyardPaths
{
    private const string _roadsPath = "World/StaticContent/CemeteryZone/Roads";
    private const float _sameLine = 0.2f;
    private const float _touching = 0.1f;
    private const float _junctionReach = 1f;
    private const float _endInset = 1f;

    // A small tile draws its dirt off its middle, north on an across tile and east on an up and down one (west when
    // mirrored), checked against screenshots. The cobbled tiles are centered.
    private const float _acrossDirt = 0.3f;
    private const float _upAndDownDirt = 0.16f;
    private const float _groundSnap = 0.1f;
    private const float _groundStep = 0.1f;
    private const int _groundTries = 20;
    private const int _spotTries = 10;

    // Walks favor the cobbled main path, a line of people to a grave favors the across line the grave is on over the
    // up and down ones, and a walk goes round people standing in a line when another way exists.
    private const float _mainPathCost = 0.5f;
    private const float _upAndDownCost = 2.5f;
    private const float _crowdedCost = 1000f;
    private const float _crowdReach = 0.5f;
    private const float _acrossReach = 2f;

    private readonly List<Segment> _segments = [];
    private readonly Dictionary<TimbnNpc, List<Vector3>> _routes = [];

    public IReadOnlyList<Segment> Segments => _segments;

    public IReadOnlyCollection<List<Vector3>> Routes => _routes.Values;

    // Who stands in a walker's way, such as a line of people, as x and z spots. A walk goes round them when it can.
    public Func<TimbnNpc, IReadOnlyList<Vector2>>? Avoid { get; set; }

    // Where the paths meet the road outside, the gate. A walk from or to a spot outside the graveyard goes through it.
    public Vector3? Entrance { get; set; }

    public void Rebuild()
    {
        _segments.Clear();
        if (GameObject.Find(_roadsPath) is not { } roads)
        {
            GraveyardVisitors.Log($"no {_roadsPath} in the scene, so visitors walk straight over the grass.");
            return;
        }

        // The game switches whole path sets on and off as the paths are upgraded, and switches each tile off while
        // the keeper is away, so the sets say what is built and the tiles are read switched off or not.
        List<Tile> tiles = [];
        foreach (Transform set in roads.transform)
        {
            if (!set.gameObject.activeSelf)
                continue;

            foreach (Transform group in set)
            {
                if (!group.gameObject.activeSelf)
                    continue;

                tiles.AddRange(group.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(tile => tile.sprite != null)
                    .Select(tile => new Tile(Regex.Replace(tile.name, @" \(\d+\)$", ""), tile.transform.position, tile.transform.lossyScale.x < 0f, WorldBounds(tile))));
            }
        }

        var height = tiles.Count > 0 ? tiles.Average(tile => tile.Position.y) : 0f;
        var across = Lines(tiles, height, horizontal: true);
        var upAndDown = Lines(tiles, height, horizontal: false);
        foreach (var segment in across)
            Trim(segment, upAndDown);

        foreach (var segment in upAndDown)
            Trim(segment, across);

        _segments.AddRange(across.Concat(upAndDown).Where(segment => segment.End - segment.Start > _touching));
        GraveyardVisitors.Log($"the graveyard paths are {across.Count} across and {upAndDown.Count} up and down, from {tiles.Count} tiles.");
    }

    // The whole route is one straight line walk through its corners, since a navmesh walk picks its own line between
    // two points and wanders off the dirt, and a stop at every corner made the walk stutter. Each corner ends a hop
    // along one path line, or the short step between the path and what the visitor came to see.
    public TimbnNpcSteps Walk(TimbnNpc npc, TimbnNpcSteps steps, Vector3 to)
    {
        if (_segments.Count == 0)
            return steps.WalkNavmesh(to.x, to.z);

        var from = npc.Position;
        List<Vector3> corners = [];
        var start = from;
        var end = to;
        if (Entrance is { } entrance)
        {
            if (!Inside(from))
            {
                Add(entrance);
                start = entrance;
            }

            if (!Inside(to))
                end = entrance;
        }

        foreach (var point in Route(start, end, Avoid?.Invoke(npc)))
            Add(point);

        Add(end);
        Add(to);
        _routes[npc] = [from, .. corners];
        return corners.Count == 0 ? steps : steps.WalkStraight(corners);

        // A corner within the game's own arrival slack of the one before (0.1, which is also how far off its point a
        // navmesh walk may end) would only make the person stop and twitch, so it is left out.
        void Add(Vector3 point)
        {
            if (TimbnWorld.FlatDistance(corners.Count == 0 ? from : corners[^1], point) >= _touching)
                corners.Add(point);
        }
    }

    private static bool Inside(Vector3 spot) => TimbnWorld.GraveyardBounds.Contains(new Vector2(spot.x, spot.z));

    public bool TryRandomSpot(Vector3 from, float shortestHop, System.Random random, out Vector3 spot)
    {
        spot = default;
        var total = _segments.Sum(segment => segment.Length);
        if (total <= 0f)
            return false;

        for (var i = 0; i < _spotTries; i++)
        {
            var pick = (float)random.NextDouble() * total;
            foreach (var segment in _segments)
            {
                if (pick > segment.Length)
                {
                    pick -= segment.Length;
                    continue;
                }

                spot = segment.At(segment.Start + pick);
                break;
            }

            if (TimbnWorld.FlatDistance(from, spot) >= shortestHop)
                return true;
        }

        return false;
    }

    public bool TryNearest(Vector3 spot, out Vector3 onPath, bool preferAcross = false)
    {
        onPath = default;
        if (_segments.Count == 0)
            return false;

        onPath = Nearest(spot, preferAcross).Point;
        return true;
    }

    // The spot a distance along the paths from the path nearest one place toward another, such as where the fourth
    // person in a line to a grave stands. Past the end of the way it is the end.
    public Vector3 PointAlong(Vector3 from, Vector3 to, float distance)
    {
        var way = Route(from, to, null, across: true);
        if (way.Count == 0)
            return _segments.Count > 0 ? Nearest(from, true).Point : from;

        for (var i = 1; i < way.Count; i++)
        {
            var length = Vector3.Distance(way[i - 1], way[i]);
            if (distance <= length)
                return Vector3.Lerp(way[i - 1], way[i], length > 0f ? distance / length : 0f);

            distance -= length;
        }

        return way[^1];
    }

    public void Forget(TimbnNpc npc) => _routes.Remove(npc);

    public void Clear() => _routes.Clear();

    private static List<Segment> Lines(List<Tile> tiles, float height, bool horizontal)
    {
        var axis = horizontal ? "_hor" : "_ver";
        var anchors = tiles
            .Where(tile => tile.Name.Contains(axis) && !tile.Name.Contains("_end"))
            .Select(tile => Anchor(tile, horizontal))
            .OrderBy(line => line)
            .ToList();
        List<Segment> segments = [];
        foreach (var line in Cluster(anchors))
        {
            // Crosses and ends are drawn off their line, so they only say where a line starts and stops.
            var spans = tiles
                .Where(tile => (tile.Name.Contains(axis) || tile.Name.Contains("cross")) && Covers(tile.Bounds, line, horizontal))
                .Select(tile => horizontal ? (Start: tile.Bounds.min.x, End: tile.Bounds.max.x, Main: tile.Name.Contains("_big_")) : (Start: tile.Bounds.min.z, End: tile.Bounds.max.z, Main: tile.Name.Contains("_big_")))
                .OrderBy(span => span.Start)
                .ToList();
            Segment? current = null;
            foreach ((var start, var end, var main) in spans)
            {
                if (current != null && start <= current.End + _touching)
                {
                    current.End = Mathf.Max(current.End, end);
                    current.Main |= main;
                    continue;
                }

                current = new Segment(horizontal, line, height, start, end) { Main = main };
                segments.Add(current);
            }
        }

        return segments;
    }

    private static IEnumerable<float> Cluster(List<float> sorted)
    {
        List<float> group = [];
        foreach (var value in sorted)
        {
            if (group.Count > 0 && value - group[^1] > _sameLine)
            {
                yield return group.Average();
                group.Clear();
            }

            group.Add(value);
        }

        if (group.Count > 0)
            yield return group.Average();
    }

    // A switched off renderer reports empty bounds, so these come from the sprite itself.
    private static Bounds WorldBounds(SpriteRenderer tile)
    {
        var local = tile.sprite.bounds;
        var bounds = new Bounds(tile.transform.TransformPoint(local.min), Vector3.zero);
        bounds.Encapsulate(tile.transform.TransformPoint(local.max));
        bounds.Encapsulate(tile.transform.TransformPoint(new Vector3(local.min.x, local.max.y, 0f)));
        bounds.Encapsulate(tile.transform.TransformPoint(new Vector3(local.max.x, local.min.y, 0f)));
        return bounds;
    }

    private static float Anchor(Tile tile, bool horizontal)
    {
        if (tile.Name.Contains("_big_"))
            return horizontal ? tile.Position.z : tile.Position.x;

        return horizontal ? tile.Position.z + _acrossDirt : tile.Position.x + (tile.Mirrored ? -_upAndDownDirt : _upAndDownDirt);
    }

    private static bool Covers(Bounds bounds, float line, bool horizontal) =>
        horizontal ? bounds.min.z <= line && line <= bounds.max.z : bounds.min.x <= line && line <= bounds.max.x;

    // A line stops on the path it meets when that is within its last tile, which cuts the stub a T crossing draws
    // past it. A line that meets nothing stops short of its end tile's edge, on walkable ground.
    private static void Trim(Segment segment, List<Segment> crossing)
    {
        segment.Start = TrimEnd(segment, segment.Start, 1f, crossing);
        segment.End = TrimEnd(segment, segment.End, -1f, crossing);
    }

    private static float TrimEnd(Segment segment, float end, float inward, List<Segment> crossing)
    {
        var junctions = crossing
            .Where(other => other.Contains(segment.Line, _touching) && (other.Line - end) * inward >= -_touching && Mathf.Abs(other.Line - end) <= _junctionReach)
            .OrderBy(other => Mathf.Abs(other.Line - end))
            .ToList();
        if (junctions.Count > 0)
            return junctions[0].Line;

        var along = end + inward * _endInset;
        for (var i = 0; i < _groundTries; i++, along += inward * _groundStep)
        {
            if (TimbnWorld.TryFindWalkable(segment.At(along), _groundSnap, out _))
                return along;
        }

        return end + inward * _endInset;
    }

    private List<Vector3> Route(Vector3 from, Vector3 to, IReadOnlyList<Vector2>? avoid = null, bool across = false)
    {
        if (_segments.Count == 0)
            return [];

        (var entrySegment, var entry) = Nearest(from, across);
        (var exitSegment, var exit) = Nearest(to);
        var links = Links(entrySegment, entry, exitSegment, exit, avoid, across);
        var path = Shortest(links, Key(entry), Key(exit));
        return Corners(path).Select(point => new Vector3(point.x, from.y, point.y)).ToList();
    }

    // With preferAcross the nearest across line wins when one lies within reach, so a grave is viewed from the line it sits on.
    private (Segment Segment, Vector3 Point) Nearest(Vector3 spot, bool preferAcross = false)
    {
        var candidates = _segments;
        if (preferAcross && _segments.Where(segment => segment.Horizontal).ToList() is { Count: > 0 } across
            && across.Any(segment => TimbnWorld.FlatDistance(spot, segment.Nearest(spot)) <= _acrossReach))
        {
            candidates = across;
        }

        var best = candidates[0];
        var bestPoint = best.Nearest(spot);
        foreach (var segment in candidates.Skip(1))
        {
            var point = segment.Nearest(spot);
            if (TimbnWorld.FlatDistance(spot, point) < TimbnWorld.FlatDistance(spot, bestPoint))
            {
                best = segment;
                bestPoint = point;
            }
        }

        return (best, bestPoint);
    }

    private Dictionary<Vector2, List<(Vector2 To, float Length)>> Links(Segment entrySegment, Vector3 entry, Segment exitSegment, Vector3 exit, IReadOnlyList<Vector2>? avoid, bool across)
    {
        Dictionary<Vector2, List<(Vector2 To, float Length)>> links = [];
        foreach (var segment in _segments)
        {
            List<float> stops = [segment.Start, segment.End];
            stops.AddRange(_segments
                .Where(other => other.Horizontal != segment.Horizontal && other.Contains(segment.Line, _touching) && segment.Contains(other.Line, _touching))
                .Select(other => other.Line));
            if (segment == entrySegment)
                stops.Add(segment.Along(entry));

            if (segment == exitSegment)
                stops.Add(segment.Along(exit));

            var points = stops.Distinct().OrderBy(along => along).Select(along => Key(segment.At(along))).Distinct().ToList();
            for (var i = 1; i < points.Count; i++)
            {
                var cost = Vector2.Distance(points[i - 1], points[i]) * (segment.Main ? _mainPathCost : across && !segment.Horizontal ? _upAndDownCost : 1f);
                if (avoid != null && avoid.Any(spot => DistanceToEdge(spot, points[i - 1], points[i]) < _crowdReach))
                    cost += _crowdedCost;

                Link(links, points[i - 1], points[i], cost);
                Link(links, points[i], points[i - 1], cost);
            }
        }

        return links;
    }

    private static float DistanceToEdge(Vector2 spot, Vector2 from, Vector2 to)
    {
        var edge = to - from;
        var along = edge.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(spot - from, edge) / edge.sqrMagnitude) : 0f;
        return Vector2.Distance(spot, from + edge * along);
    }

    private static void Link(Dictionary<Vector2, List<(Vector2 To, float Length)>> links, Vector2 from, Vector2 to, float length)
    {
        if (!links.TryGetValue(from, out var list))
        {
            list = [];
            links[from] = list;
        }

        list.Add((to, length));
    }

    private static List<Vector2> Shortest(Dictionary<Vector2, List<(Vector2 To, float Length)>> links, Vector2 start, Vector2 goal)
    {
        Dictionary<Vector2, float> distances = new() { [start] = 0f };
        Dictionary<Vector2, Vector2> previous = [];
        HashSet<Vector2> done = [];
        while (true)
        {
            var open = distances.Where(pair => !done.Contains(pair.Key)).ToList();
            if (open.Count == 0)
                return [];

            var current = open.OrderBy(pair => pair.Value).First();
            if (current.Key == goal)
                break;

            done.Add(current.Key);
            if (!links.TryGetValue(current.Key, out var next))
                continue;

            foreach ((var to, var length) in next)
            {
                var distance = current.Value + length;
                if (distances.TryGetValue(to, out var known) && known <= distance)
                    continue;

                distances[to] = distance;
                previous[to] = current.Key;
            }
        }

        List<Vector2> path = [goal];
        while (path[^1] != start)
            path.Add(previous[path[^1]]);

        path.Reverse();
        return path;
    }

    // Only the turns matter, since the walk between two points on one line stays on that line.
    private static IEnumerable<Vector2> Corners(List<Vector2> path)
    {
        for (var i = 0; i < path.Count; i++)
        {
            if (i > 0 && i < path.Count - 1 && IsStraight(path[i - 1], path[i], path[i + 1]))
                continue;

            yield return path[i];
        }
    }

    private static bool IsStraight(Vector2 before, Vector2 at, Vector2 after) =>
        (Mathf.Abs(before.x - at.x) < _touching && Mathf.Abs(at.x - after.x) < _touching)
        || (Mathf.Abs(before.y - at.y) < _touching && Mathf.Abs(at.y - after.y) < _touching);

    private static Vector2 Key(Vector3 point) => new(Mathf.Round(point.x * 100f) / 100f, Mathf.Round(point.z * 100f) / 100f);

    internal sealed class Segment
    {
        public Segment(bool horizontal, float line, float height, float start, float end)
        {
            Horizontal = horizontal;
            Line = line;
            Height = height;
            Start = start;
            End = end;
        }

        public bool Horizontal { get; }

        public bool Main { get; set; }

        public float Line { get; }

        public float Height { get; }

        public float Start { get; set; }

        public float End { get; set; }

        public float Length => End - Start;

        public Vector3 At(float along) => Horizontal ? new Vector3(along, Height, Line) : new Vector3(Line, Height, along);

        public float Along(Vector3 point) => Mathf.Clamp(Horizontal ? point.x : point.z, Start, End);

        public Vector3 Nearest(Vector3 point) => At(Along(point));

        public bool Contains(float along, float slack) => along >= Start - slack && along <= End + slack;
    }

    private sealed class Tile
    {
        public Tile(string name, Vector3 position, bool mirrored, Bounds bounds)
        {
            Name = name;
            Position = position;
            Mirrored = mirrored;
            Bounds = bounds;
        }

        public string Name { get; }

        public Vector3 Position { get; }

        public bool Mirrored { get; }

        public Bounds Bounds { get; }
    }
}
