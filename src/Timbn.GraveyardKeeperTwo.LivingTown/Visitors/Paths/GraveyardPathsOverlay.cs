using HarmonyLib;

namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class GraveyardPathsOverlay : IDisposable
{
    private const float _refreshSeconds = 1f;
    private const float _spotSeconds = 5f;
    private const float _lift = 0.05f;
    private const float _pathWidth = 0.08f;
    private const float _routeWidth = 0.05f;
    private const float _markerSize = 0.25f;
    private const float _spotSize = 0.2f;
    private const float _lookWidth = 0.02f;
    private const float _groundRange = 1f;
    private const int _alwaysOnTop = 8;
    private const int _overlayQueue = 5000;

    private static readonly string[] _shaders = ["Hidden/Internal-Colored", "Sprites/Default"];
    private static readonly Color _pathColor = new(1f, 0.55f, 0.1f);
    private static readonly Color _stopColor = Color.yellow;
    private static readonly Color _routeColor = new(0.2f, 0.9f, 1f);
    private static readonly Color _standColor = new(0.3f, 1f, 0.3f);
    private static readonly Color _seatColor = new(1f, 0.3f, 1f);
    private static readonly Color _walkColor = new(1f, 0.15f, 0.15f);

    private readonly Plugin _plugin;
    private readonly GraveyardVisitors _visitors;
    private readonly GraveyardPaths _paths;
    private IDisposable? _updates;
    private GameObject? _root;
    private Material? _material;
    private float _nextRefresh;
    private float _nextSpots;
    private List<(Vector3 Spot, Vector3 Target, bool Seat)> _spots = [];
    private bool _built;

    public GraveyardPathsOverlay(Plugin plugin, GraveyardVisitors visitors)
    {
        _plugin = plugin;
        _visitors = visitors;
        _paths = visitors.Paths;
    }

    public IDisposable Show()
    {
        _nextRefresh = 0f;
        _nextSpots = 0f;
        _built = false;
        _updates = _plugin.Events.Update(Refresh);
        return this;
    }

    public void Dispose()
    {
        _updates?.Dispose();
        _updates = null;
        Clear();
    }

    private void Refresh()
    {
        if (!TimbnGame.IsInGame)
        {
            _built = false;
            Clear();
            return;
        }

        if (Time.unscaledTime < _nextRefresh)
            return;

        _nextRefresh = Time.unscaledTime + _refreshSeconds;

        // Visitors rebuild the paths each morning, so the overlay only builds them when it is on before that.
        if (!_built && _paths.Segments.Count == 0)
            _paths.Rebuild();

        _built = true;
        Clear();
        foreach (var segment in _paths.Segments)
        {
            var start = Ground(segment.At(segment.Start));
            var end = Ground(segment.At(segment.End));
            Line([start, end], _pathColor, _pathWidth);
            Marker(start);
            Marker(end);
        }

        foreach (var route in _paths.Routes)
            Line(route.Select(Ground).ToList(), _routeColor, _routeWidth);

        foreach (var npc in _visitors.People.Where(npc => !npc.IsGone))
        {
            if (Walking(npc) is { } walk)
                Line(walk, _walkColor, _routeWidth);
        }

        // Finding the spots walks every world object, so they are only worked out every few seconds.
        if (Time.unscaledTime >= _nextSpots)
        {
            _spots = _visitors.Spots().ToList();
            _nextSpots = Time.unscaledTime + _spotSeconds;
        }

        foreach ((var spot, var target, var seat) in _spots)
        {
            var at = Ground(spot);
            var color = seat ? _seatColor : _standColor;
            Square(at, color);
            if (!seat)
                Line([at, Ground(target)], color, _lookWidth);
        }
    }

    // What the game is actually walking right now, its navmesh path when it has one, otherwise straight to the end.
    private static List<Vector3>? Walking(TimbnNpc npc)
    {
        if (npc.Data.MovementComponent is not { IsMoving: true } movement)
            return null;

        var fields = Traverse.Create(movement);
        var corners = fields.Field("wholePath").Field("vectorPath").GetValue<List<Vector3>>();
        if (corners is { Count: >= 2 })
            return [npc.Position, .. corners.SkipWhile(corner => TimbnWorld.FlatDistance(corner, npc.Position) < 0.05f)];

        return [npc.Position, fields.Field("endPos").GetValue<Vector3>()];
    }

    private void Square(Vector3 at, Color color)
    {
        var half = _spotSize / 2f;
        Line(
            [
                at + new Vector3(-half, 0f, -half),
                at + new Vector3(half, 0f, -half),
                at + new Vector3(half, 0f, half),
                at + new Vector3(-half, 0f, half),
                at + new Vector3(-half, 0f, -half),
            ],
            color,
            _routeWidth);
    }

    private static Vector3 Ground(Vector3 point) =>
        TimbnWorld.TryFindWalkable(point, _groundRange, out var ground) ? new Vector3(point.x, ground.y, point.z) : new Vector3(point.x, TimbnPlayer.Position.y, point.z);

    private void Marker(Vector3 at)
    {
        var half = _markerSize / 2f;
        Line([at + new Vector3(-half, 0f, -half), at + new Vector3(half, 0f, half)], _stopColor, _routeWidth);
        Line([at + new Vector3(-half, 0f, half), at + new Vector3(half, 0f, -half)], _stopColor, _routeWidth);
    }

    private void Line(IReadOnlyList<Vector3> points, Color color, float width)
    {
        if (points.Count < 2 || Material() is not { } material)
            return;

        _root ??= new GameObject("LivingTownGraveyardPaths");
        var line = new GameObject("Line");
        line.transform.SetParent(_root.transform, false);
        var renderer = line.AddComponent<LineRenderer>();
        renderer.useWorldSpace = true;
        renderer.positionCount = points.Count;
        for (var i = 0; i < points.Count; i++)
            renderer.SetPosition(i, points[i] + Vector3.up * _lift);

        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.numCapVertices = 2;
        renderer.sharedMaterial = material;
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.sortingOrder = short.MaxValue;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void Clear()
    {
        if (_root != null)
            UnityEngine.Object.Destroy(_root);

        _root = null;
    }

    private Material? Material()
    {
        if (_material != null)
            return _material;

        foreach (var name in _shaders)
        {
            if (Shader.Find(name) is not { } shader)
                continue;

            // Drawn over everything, so the lines show through benches, graves and the keeper.
            _material = new Material(shader) { renderQueue = _overlayQueue };
            if (_material.HasProperty("_ZTest"))
                _material.SetInt("_ZTest", _alwaysOnTop);

            if (_material.HasProperty("_ZWrite"))
                _material.SetInt("_ZWrite", 0);

            return _material;
        }

        return null;
    }
}
