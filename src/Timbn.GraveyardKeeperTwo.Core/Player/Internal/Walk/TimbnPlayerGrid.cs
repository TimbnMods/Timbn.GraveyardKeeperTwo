using Pathfinding;
using Pathfinding.Graphs.Grid.Rules;
using Pathfinding.Util;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Lays the game's player grid graph over a small box around a spot on a walk's route that the player does not fit
/// through, the graph the game rescans around the player for a walk to a workstation, as a layer over the scene's
/// navmesh. Each cell takes its ground from the navmesh triangle under it, a cell no triangle reaches is closed, and so
/// is every cell where the player's real body does not fit. That work runs a slice per frame, and only the last step
/// scans the grid at once with the results. The body is wider one way than the other, which a grid's own round test
/// cannot model, so its own test is shrunk to a point and its ground rays are off.
/// </summary>
internal static class TimbnPlayerGrid
{
    private const int _graphIndex = (int)LazyConsts.Navigation.Graph.PlayerGraph;
    private const float _nodeSize = LazyConsts.PLAYER_GRAPH_NODE_SIZE;
    private const float _largestNodeSize = 0.2f;
    private const float _pointWidth = 0.05f;
    private const int _obstacleLayer = LazyConsts.Layers.OBSTACLES;
    private const int _maxNodes = 240;
    private const int _bodyChunk = 512;
    private const int _ledgeChunk = 256;
    private const byte _offNavmesh = 1;
    private const byte _atLedge = 2;
    private const byte _bodyDoesNotFit = 3;
    private const byte _closedByCheck = 4;
    private const float _reuseSeconds = 1.5f;
    private const float _closeSize = 0.25f;
    private const float _closeHeight = 4f;
    private const float _heightWeight = 0.001f;
    private const float _groundReach = 0.5f;
    private const float _edgeReach = 0.5f;

    private static readonly ApplyRule _apply = new();
    private static Rect _scannedArea;
    private static float _scannedAt = float.MinValue;
    private static Vector3 _center;
    private static int _width;
    private static GraphTransform? _layout;
    private static bool[] _open = [];
    private static byte[] _why = [];
    private static int _gridWidth;
    private static int _gridDepth;
    private static float _cellSize;
    private static float _lastUsed = float.MinValue;
    private static bool _enlarged;

    internal static GraphMask Mask => GraphMask.FromGraphIndex(_graphIndex);

    /// <summary>The layers the player grid counts as obstacles, or the Obstacle layer when the scene has no player grid.</summary>
    internal static int ObstacleMask => Grid() is { } grid ? grid.collision.mask : 1 << _obstacleLayer;

    /// <summary>How the last <see cref="Cover"/> went, for the walk's log line.</summary>
    internal static string LastScan { get; private set; } = "";

    /// <summary>
    /// Lays the player grid over <paramref name="area"/> seen from above, a slice per frame within
    /// <paramref name="budget"/>, unless a scan a moment ago already covers it. Where two levels of navmesh overlap, the
    /// one nearer <paramref name="route"/>'s height wins. Sets <paramref name="covered"/> to false when the area is too
    /// big for the grid, the scene has no grid or navmesh, or <paramref name="abandoned"/> turns true on the way.
    /// </summary>
    internal static IEnumerator Cover(IReadOnlyList<Vector3> route, Rect area, TimbnFrameBudget budget, Func<bool> abandoned, StrongBox<bool> covered)
    {
        covered.Value = false;
        if (route.Count == 0 || Grid() is not { } grid || !TimbnWorld.TryGetNavGraph(null, out var graph) || graph is not NavmeshBase navmesh)
            yield break;

        if (Time.time - _scannedAt < _reuseSeconds && grid.center == _center && grid.width == _width && Contains(_scannedArea, area))
        {
            LastScan = "the player grid scanned a moment ago";
            covered.Value = true;
            yield break;
        }

        var nodeSize = Mathf.Max(_nodeSize, Mathf.Max(area.width, area.height) / _maxNodes);
        if (nodeSize > _largestNodeSize)
        {
            LastScan = $"nothing, since {area.width:0} by {area.height:0} units is too big for the player grid";
            yield break;
        }

        var started = Time.realtimeSinceStartup;
        var frames = 1;
        _lastUsed = Time.time;
        var width = Mathf.CeilToInt(area.width / nodeSize);
        var depth = Mathf.CeilToInt(area.height / nodeSize);
        var center = new Vector3(Round(area.center.x, nodeSize), route[0].y, Round(area.center.y, nodeSize));
        var layout = Layout(grid, center, width, depth, nodeSize);
        var count = width * depth;
        var positions = new Vector3[count];
        var open = new bool[count];
        var expected = new float[count];
        var distance = new float[count];
        var heights = new float[count];
        var why = new byte[count];
        for (var z = 0; z < depth; z++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = z * width + x;
                positions[index] = layout.Transform(new Vector3(x + 0.5f, 0f, z + 0.5f));
                expected[index] = HeightNear(positions[index], route);
                open[index] = true;
                distance[index] = float.MaxValue;
            }

            if (!budget.IsSpent)
                continue;

            yield return null;
            frames++;
            budget.Restart();
            if (abandoned())
                yield break;
        }

        var tiles = navmesh.GetTouchingTiles(new Bounds(new Vector3(area.center.x, 0f, area.center.y), new Vector3(area.width, 10000f, area.height)));
        for (var tileZ = tiles.ymin; tileZ <= tiles.ymax; tileZ++)
        {
            for (var tileX = tiles.xmin; tileX <= tiles.xmax; tileX++)
            {
                if (navmesh.GetTile(tileX, tileZ) is { nodes: { } triangles })
                {
                    foreach (var triangle in triangles)
                        Paint(triangle, layout, width, depth, distance, heights, expected);
                }

                if (!budget.IsSpent)
                    continue;

                yield return null;
                frames++;
                budget.Restart();
                if (abandoned())
                    yield break;
            }
        }

        var off = 0;
        for (var i = 0; i < count; i++)
        {
            if (distance[i] == float.MaxValue)
            {
                open[i] = false;
                why[i] = _offNavmesh;
                off++;
                continue;
            }

            positions[i].y = heights[i];
        }

        var ledges = 0;
        for (var start = 0; start < count; start += _ledgeChunk)
        {
            ledges += TimbnLedge.CloseOffLedges(positions, open, start, Mathf.Min(_ledgeChunk, count - start), why, _atLedge);
            if (!budget.IsSpent)
                continue;

            yield return null;
            frames++;
            budget.Restart();
            if (abandoned())
                yield break;
        }

        var mask = ObstacleMask;
        var slack = TimbnWalkProbe.Tolerance;
        var closed = 0;
        for (var start = 0; start < count; start += _bodyChunk)
        {
            closed += TimbnWalkProbe.CloseWhereBodyDoesNotFit(positions, open, start, Mathf.Min(_bodyChunk, count - start), mask, slack, why, _bodyDoesNotFit);
            if (!budget.IsSpent)
                continue;

            yield return null;
            frames++;
            budget.Restart();
            if (abandoned())
                yield break;
        }

        Scan(grid, center, width, depth, nodeSize, positions, open);
        _enlarged = true;
        _lastUsed = Time.time;
        _layout = layout;
        _open = open;
        _why = why;
        _gridWidth = width;
        _gridDepth = depth;
        _cellSize = nodeSize;
        _scannedArea = area;
        _scannedAt = Time.time;
        _center = grid.center;
        _width = grid.width;
        LastScan = $"the player grid, {width} by {depth} cells of {nodeSize:0.00}, {off} off the navmesh, {ledges} at a ledge and {closed} closed for the body, "
            + $"laid over {frames} frames in {(Time.realtimeSinceStartup - started) * 1000f:0} ms";
        covered.Value = true;
    }

    /// <summary>
    /// Whether every cell under the straight line from <paramref name="from"/> to <paramref name="to"/> is open on the
    /// last grid laid, so the player's body fits all the way along it. False off that grid.
    /// </summary>
    internal static bool IsOpenAlong(Vector3 from, Vector3 to)
    {
        _lastUsed = Time.time;
        if (_layout is not { } layout || _open.Length == 0)
            return false;

        var steps = Mathf.Max(1, Mathf.CeilToInt(TimbnWorld.FlatDistance(from, to) / (_cellSize / 4f)));
        for (var i = 0; i <= steps; i++)
        {
            if (!TryCell(layout, Vector3.Lerp(from, to, (float)i / steps), out var index) || !_open[index])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Why the route through <paramref name="route"/> is cut off on the last grid laid: the first closed cell along it
    /// past the point nearest <paramref name="reached"/>, the furthest spot a grid route got to, and what closed it.
    /// </summary>
    internal static string WhyCutOff(IReadOnlyList<Vector3> route, Vector3 reached)
    {
        if (_layout is not { } layout || _why.Length == 0 || route.Count < 2)
            return "no grid to read";

        List<Vector3> samples = [];
        for (var i = 1; i < route.Count; i++)
        {
            var steps = Mathf.Max(1, Mathf.CeilToInt(TimbnWorld.FlatDistance(route[i - 1], route[i]) / (_cellSize / 2f)));
            for (var step = 0; step < steps; step++)
                samples.Add(Vector3.Lerp(route[i - 1], route[i], (float)step / steps));
        }

        samples.Add(route[^1]);
        var nearest = 0;
        for (var i = 1; i < samples.Count; i++)
        {
            if (TimbnWorld.FlatDistance(samples[i], reached) < TimbnWorld.FlatDistance(samples[nearest], reached))
                nearest = i;
        }

        for (var i = nearest; i < samples.Count; i++)
        {
            if (!TryCell(layout, samples[i], out var index) || _open[index])
                continue;

            var reason = _why[index] switch
            {
                _offNavmesh => "off the navmesh",
                _atLedge => "at a ledge, where the game's ground rays find no ground",
                _bodyDoesNotFit => "where the body does not fit",
                _closedByCheck => "closed by the check before walking",
                _ => "closed by the grid's own test",
            };
            return $"the navmesh route is cut off at ({samples[i].x:0.00}, {samples[i].z:0.00}), {reason}";
        }

        return "the navmesh route is open all the way on the grid";
    }

    /// <summary>The navmesh surface under <paramref name="point"/>, or the point itself where there is no navmesh close by.</summary>
    internal static Vector3 GroundAt(Vector3 point)
    {
        if (!TimbnWorld.TryGetNavGraph(null, out var navmesh))
            return point;

        var nearest = navmesh.GetNearest(point, Below(_groundReach));
        return nearest.node != null ? nearest.position : point;
    }

    /// <summary>
    /// Puts the player grid back the way the game keeps it, a small patch around the player, once no walk has used the
    /// grid for a moment. Otherwise a large grid of fine cells would stay behind for the game's other searches to find.
    /// </summary>
    internal static void Tick()
    {
        if (!_enlarged || Time.time - _lastUsed < _reuseSeconds)
            return;

        Forget();
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player != null && player.PlayerLocalAreaMovement != null)
            player.PlayerLocalAreaMovement.RescanPlayerGraph();
    }

    /// <summary>Drops what Core knows of the last grid it laid, so nothing reuses it.</summary>
    internal static void Forget()
    {
        _enlarged = false;
        _layout = null;
        _open = [];
        _why = [];
        _scannedAt = float.MinValue;
    }

    /// <summary>Closes the player grid's cells around <paramref name="at"/>, a spot a route was found not to fit the body.</summary>
    internal static void Close(Vector3 at)
    {
        _lastUsed = Time.time;
        if (TimbnWorld.Pathfinding is not { } pathfinding)
            return;

        if (_layout is { } layout && _open.Length > 0)
        {
            var reach = Mathf.CeilToInt(_closeSize / 2f / _cellSize);
            if (TryCell(layout, at, out var middle))
            {
                var x = middle % _gridWidth;
                var z = middle / _gridWidth;
                for (var dz = -reach; dz <= reach; dz++)
                {
                    for (var dx = -reach; dx <= reach; dx++)
                    {
                        var cx = x + dx;
                        var cz = z + dz;
                        if (cx < 0 || cz < 0 || cx >= _gridWidth || cz >= _gridDepth)
                            continue;

                        _open[cz * _gridWidth + cx] = false;
                        _why[cz * _gridWidth + cx] = _closedByCheck;
                    }
                }
            }
        }

        pathfinding.UpdateGraphs(new GraphUpdateObject(new Bounds(at, new Vector3(_closeSize, _closeHeight, _closeSize)))
        {
            graphMask = Mask,
            updatePhysics = false,
            modifyWalkability = true,
            setWalkability = false,
        });
        pathfinding.FlushGraphUpdates();
    }

    /// <summary>
    /// The transform the grid would have at this size, read by sizing it and putting the old size straight back, so the
    /// grid never stays a size its cells do not match.
    /// </summary>
    private static GraphTransform Layout(GridGraph grid, Vector3 center, int width, int depth, float nodeSize)
    {
        var oldCenter = grid.center;
        var oldWidth = grid.width;
        var oldDepth = grid.depth;
        var oldNodeSize = grid.nodeSize;
        try
        {
            grid.center = center;
            grid.SetDimensions(width, depth, nodeSize);
            return grid.CalculateTransform();
        }
        finally
        {
            grid.center = oldCenter;
            grid.SetDimensions(oldWidth, oldDepth, oldNodeSize);
        }
    }

    private static void Scan(GridGraph grid, Vector3 center, int width, int depth, float nodeSize, Vector3[] positions, bool[] open)
    {
        var gameDiameter = grid.collision.diameter;
        var gameHeightCheck = grid.collision.heightCheck;
        grid.center = center;
        grid.collision.diameter = _pointWidth / nodeSize;
        grid.collision.heightCheck = false;
        grid.SetDimensions(width, depth, nodeSize);
        _apply.Positions = positions;
        _apply.Open = open;
        grid.rules.AddRule(_apply);
        try
        {
            grid.Scan();
        }
        finally
        {
            grid.rules.RemoveRule(_apply);
            grid.collision.diameter = gameDiameter;
            grid.collision.heightCheck = gameHeightCheck;
            _apply.Positions = null;
            _apply.Open = null;
        }
    }

    /// <summary>
    /// A navmesh lookup that matches as seen from above, with height weighed so lightly that only where two levels
    /// overlap, such as a bridge over a path, does the one nearer in height win.
    /// </summary>
    private static NearestNodeConstraint Below(float maxDistance)
    {
        var below = NearestNodeConstraint.Walkable;
        below.distanceMetric = new DistanceMetric
        {
            projectionAxis = Vector3.positiveInfinity,
            distanceScaleAlongProjectionDirection = _heightWeight,
        };
        below.maxDistance = maxDistance;
        return below;
    }

    /// <summary>
    /// Paints one navmesh triangle onto the cells under it. A cell takes the height of the triangle under its middle,
    /// or of one whose edge is within half a cell so a narrow strip of navmesh keeps its edges. Where two levels overlap,
    /// such as a rock shelf over a path, the one nearer the route's own height there wins.
    /// </summary>
    private static void Paint(TriangleMeshNode triangle, GraphTransform layout, int width, int depth, float[] distance, float[] heights, float[] expected)
    {
        var a = (Vector3)triangle.GetVertex(0);
        var b = (Vector3)triangle.GetVertex(1);
        var c = (Vector3)triangle.GetVertex(2);
        var la = layout.InverseTransform(a);
        var lb = layout.InverseTransform(b);
        var lc = layout.InverseTransform(c);
        var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(la.x, Mathf.Min(lb.x, lc.x)) - 1f));
        var maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(la.x, Mathf.Max(lb.x, lc.x))));
        var minZ = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(la.z, Mathf.Min(lb.z, lc.z)) - 1f));
        var maxZ = Mathf.Min(depth - 1, Mathf.CeilToInt(Mathf.Max(la.z, Mathf.Max(lb.z, lc.z))));
        if (minX > maxX || minZ > maxZ)
            return;

        var pa = new Vector2(la.x, la.z);
        var pb = new Vector2(lb.x, lb.z);
        var pc = new Vector2(lc.x, lc.z);
        for (var z = minZ; z <= maxZ; z++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var index = z * width + x;
                if (!TryHeight(new Vector2(x + 0.5f, z + 0.5f), pa, pb, pc, a.y, b.y, c.y, out var away, out var height))
                    continue;

                var better = away < distance[index] - 0.001f
                    || (away <= distance[index] + 0.001f && Mathf.Abs(height - expected[index]) < Mathf.Abs(heights[index] - expected[index]));
                if (!better)
                    continue;

                distance[index] = away;
                heights[index] = height;
            }
        }
    }

    /// <summary>
    /// The height of the triangle under <paramref name="cell"/>, or at its nearest edge when that is within half a
    /// cell, and how far away that is in cells. False when the triangle is further than half a cell.
    /// </summary>
    private static bool TryHeight(Vector2 cell, Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc, out float away, out float height)
    {
        away = 0f;
        height = 0f;
        var area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
        if (Mathf.Abs(area) < 1e-6f)
            return false;

        var u = ((b.x - cell.x) * (c.y - cell.y) - (c.x - cell.x) * (b.y - cell.y)) / area;
        var v = ((c.x - cell.x) * (a.y - cell.y) - (a.x - cell.x) * (c.y - cell.y)) / area;
        var w = 1f - u - v;
        if (u >= 0f && v >= 0f && w >= 0f)
        {
            height = u * ya + v * yb + w * yc;
            return true;
        }

        away = float.MaxValue;
        Closest(cell, a, b, ya, yb, ref away, ref height);
        Closest(cell, b, c, yb, yc, ref away, ref height);
        Closest(cell, c, a, yc, ya, ref away, ref height);
        return away <= _edgeReach;
    }

    private static void Closest(Vector2 cell, Vector2 start, Vector2 end, float startHeight, float endHeight, ref float away, ref float height)
    {
        var along = end - start;
        var length = along.sqrMagnitude;
        var t = length > 0f ? Mathf.Clamp01(Vector2.Dot(cell - start, along) / length) : 0f;
        var gap = Vector2.Distance(cell, start + along * t);
        if (gap >= away)
            return;

        away = gap;
        height = Mathf.Lerp(startHeight, endHeight, t);
    }

    private static bool Contains(Rect outer, Rect inner) =>
        inner.xMin >= outer.xMin && inner.yMin >= outer.yMin && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;

    /// <summary>The route's height at its point nearest <paramref name="point"/> seen from above.</summary>
    private static float HeightNear(Vector3 point, IReadOnlyList<Vector3> route)
    {
        var height = route[0].y;
        var best = float.MaxValue;
        var flat = new Vector2(point.x, point.z);
        for (var i = 1; i < route.Count; i++)
        {
            var start = new Vector2(route[i - 1].x, route[i - 1].z);
            var end = new Vector2(route[i].x, route[i].z);
            var along = end - start;
            var length = along.sqrMagnitude;
            var t = length > 0f ? Mathf.Clamp01(Vector2.Dot(flat - start, along) / length) : 0f;
            var gap = Vector2.Distance(flat, start + along * t);
            if (gap >= best)
                continue;

            best = gap;
            height = Mathf.Lerp(route[i - 1].y, route[i].y, t);
        }

        return height;
    }

    private static bool TryCell(GraphTransform layout, Vector3 point, out int index)
    {
        var local = layout.InverseTransform(point);
        var x = Mathf.FloorToInt(local.x);
        var z = Mathf.FloorToInt(local.z);
        index = z * _gridWidth + x;
        return x >= 0 && z >= 0 && x < _gridWidth && z < _gridDepth;
    }

    private static GridGraph? Grid() =>
        TimbnWorld.Pathfinding?.data?.graphs is { } graphs && graphs.Length > _graphIndex ? graphs[_graphIndex] as GridGraph : null;

    private static float Round(float value, float step) => Mathf.Round(value / step) * step;

    /// <summary>Copies cell positions and which cells are open, both worked out beforehand, into the grid as it scans.</summary>
    private sealed class ApplyRule : GridGraphRule
    {
        public Vector3[]? Positions { get; set; }

        public bool[]? Open { get; set; }

        public override void Register(GridGraphRules rules) => rules.AddMainThreadPass(Pass.BeforeCollision, Apply);

        private void Apply(GridGraphRules.Context context)
        {
            var positions = context.data.nodes.positions;
            var walkable = context.data.nodes.walkable;
            if (Positions == null || Open == null || positions.Length != Positions.Length)
                return;

            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = Positions[i];
                walkable[i] &= Open[i];
            }
        }
    }
}
