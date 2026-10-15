namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The colliders, standing spot and baked data a world object made from a mod model gets, worked out from its size
/// alone so the numbers can be tested without Unity. A station is a box with a footprint, a solid body, an interaction
/// area and one dock point in front. An extension is the same box with a buff area around it and nothing to interact
/// with. An extra part has only the solid box it was given, if any.
/// </summary>
internal sealed class TimbnPartShape
{
    internal const int InteractLayer = 6;
    internal const int DockPointLayer = 7;
    internal const int ObstacleLayer = 8;
    internal const int BuildAreaLayer = 19;
    internal const int BuffAreaLayer = 28;

    // The body is narrower than the footprint so the player can stand right against the build grid cell.
    private const float _bodyScale = 0.8f;
    private const float _footprintHeight = 0.2f;
    private const float _interactMargin = 0.3f;
    private const float _dockDistance = 0.45f;
    private const float _dockRadius = 0.15f;
    private const float _chunkPadding = 0.5f;

    private TimbnPartShape(Vector3? footprint, List<TimbnPartBox> boxes, Vector3? dockPoint)
    {
        Footprint = footprint;
        Boxes = boxes;
        DockPoint = dockPoint;
    }

    internal Vector3? Footprint { get; }

    internal IReadOnlyList<TimbnPartBox> Boxes { get; }

    internal Vector3? DockPoint { get; }

    internal float DockRadius => _dockRadius;

    internal Rect? FootprintRect =>
        Footprint is { } size ? new Rect(-size.x / 2f, -size.z / 2f, size.x, size.z) : null;

    internal static TimbnPartShape Station(Vector3 size, float? dockDistance = null, TimbnFacing facing = TimbnFacing.Down)
    {
        var boxes = FootprintBoxes(size);
        var center = new Vector3(0f, size.y / 2f, 0f);
        boxes.Add(new TimbnPartBox("Interact", InteractLayer, center, size + new Vector3(_interactMargin, 0f, _interactMargin), true));
        return new TimbnPartShape(size, boxes, DockOffset(size, dockDistance ?? _dockDistance, facing));
    }

    // The keeper works a construction site from its dock spot, which has to clear the site's solid body to be reachable,
    // so a site keeps the default distance even when the finished station asks for a closer one.
    internal static float SiteDockDistance(float? stationDistance) => Math.Max(stationDistance ?? _dockDistance, _dockDistance);

    // The distance is measured from the footprint's edge on that side.
    internal static Vector3 DockOffset(Vector3 size, float distance, TimbnFacing facing) => facing switch
    {
        TimbnFacing.Down => new Vector3(0f, 0f, -(size.z / 2f + distance)),
        TimbnFacing.Up => new Vector3(0f, 0f, size.z / 2f + distance),
        TimbnFacing.Left => new Vector3(-(size.x / 2f + distance), 0f, 0f),
        _ => new Vector3(size.x / 2f + distance, 0f, 0f),
    };

    internal static TimbnPartShape Extension(Vector3 size, float reach)
    {
        var boxes = FootprintBoxes(size);
        var footprint = new Vector3(0f, _footprintHeight / 2f, 0f);
        boxes.Add(new TimbnPartBox("Buff Area", BuffAreaLayer, footprint, new Vector3(size.x + reach * 2f, _footprintHeight, size.z + reach * 2f), true));
        return new TimbnPartShape(size, boxes, null);
    }

    internal static TimbnPartShape Part(Bounds? solid)
    {
        List<TimbnPartBox> boxes = [];
        if (solid is { } box)
            boxes.Add(new TimbnPartBox("Obstacle", ObstacleLayer, box.center, box.size, false));

        return new TimbnPartShape(null, boxes, null);
    }

    // The chunk loader culls by these, so they are padded past the body. An extra part has no footprint and goes by the model.
    internal Bounds ChunkBounds(Bounds modelBounds)
    {
        if (Footprint is { } size)
            return new Bounds(new Vector3(0f, size.y / 2f, 0f), size + Vector3.one * _chunkPadding);

        var bounds = modelBounds;
        bounds.Expand(_chunkPadding);
        return bounds;
    }

    private static List<TimbnPartBox> FootprintBoxes(Vector3 size)
    {
        var center = new Vector3(0f, size.y / 2f, 0f);
        var footprint = new Vector3(0f, _footprintHeight / 2f, 0f);
        return
        [
            new TimbnPartBox("Obstacle", ObstacleLayer, center, new Vector3(size.x * _bodyScale, size.y, size.z * _bodyScale), false),
            new TimbnPartBox("Build Collider", BuildAreaLayer, footprint, new Vector3(size.x, _footprintHeight, size.z), true),
        ];
    }
}

internal sealed class TimbnPartBox
{
    internal TimbnPartBox(string name, int layer, Vector3 center, Vector3 size, bool isTrigger)
    {
        Name = name;
        Layer = layer;
        Center = center;
        Size = size;
        IsTrigger = isTrigger;
    }

    internal string Name { get; }

    internal int Layer { get; }

    internal Vector3 Center { get; }

    internal Vector3 Size { get; }

    internal bool IsTrigger { get; }
}
