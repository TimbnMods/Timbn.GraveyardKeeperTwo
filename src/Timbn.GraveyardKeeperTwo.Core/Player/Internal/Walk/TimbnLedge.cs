using Unity.Collections;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Whether the player could stand at a spot without the game walling them off at a ledge. The game's
/// <c>PlayerInvisibleWalls</c> casts eight short rays down around the player's feet (to the sides, ahead, behind and
/// the four corners) against the ground, and raises a wall on any side whose ray finds none. The navmesh carries on
/// past many such ledges, so a walk tests the same eight rays, with the distances read from the player's own walls.
/// The navmesh's height is only roughly the ground's, so the surface under the spot is found first and the eight rays
/// start from it, as they do from the player's feet. The game only raises these walls while the player stands on Ground
/// or Step, so on any other surface they stand on, such as a bridge deck, the rays are not tested.
/// </summary>
internal static class TimbnLedge
{
    private const float _searchAbove = 1.5f;
    private const float _searchBelow = 1.5f;
    private const int _cellsPerJob = 64;
    private const int _maxGroundHits = 4;

    private static readonly AccessTools.FieldRef<PlayerPhysicalBody, PlayerInvisibleWalls>? _walls = TimbnReflection.TryField<PlayerPhysicalBody, PlayerInvisibleWalls>("invisibleWalls");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _sideX = TimbnReflection.TryField<PlayerInvisibleWalls, float>("edgeCheckDistanceX");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _ahead = TimbnReflection.TryField<PlayerInvisibleWalls, float>("edgeCheckDistanceZPlus");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _behind = TimbnReflection.TryField<PlayerInvisibleWalls, float>("edgeCheckDistanceZMinus");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _cornerX = TimbnReflection.TryField<PlayerInvisibleWalls, float>("edgeCheckDistanceDiagonalX");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _cornerAhead = TimbnReflection.TryField<PlayerInvisibleWalls, float>("edgeCheckDistanceDiagonalZPlus");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _cornerBehind = TimbnReflection.TryField<PlayerInvisibleWalls, float>("edgeCheckDistanceDiagonalZMinus");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _rayLength = TimbnReflection.TryField<PlayerInvisibleWalls, float>("edgeRaycastLength");
    private static readonly AccessTools.FieldRef<PlayerInvisibleWalls, float>? _rayLift = TimbnReflection.TryField<PlayerInvisibleWalls, float>("raycastYOffset");

    private static readonly List<Vector3> _probes = [];
    private static readonly RaycastHit[] _groundHits = new RaycastHit[_maxGroundHits];
    private static int _supportMask;
    private static PlayerInvisibleWalls? _measured;
    private static float _length;
    private static float _lift;

    private static int GroundMask => LayerMask.GetMask("Ground", "Step");

    /// <summary>
    /// Every solid layer the player stands on, which is every layer their own collides with except obstacles, water,
    /// their own walls and their own parts.
    /// </summary>
    private static int SupportMask
    {
        get
        {
            if (_supportMask != 0)
                return _supportMask;

            var player = LayerMask.NameToLayer("Player");
            var mask = 0;
            for (var layer = 0; layer < 32; layer++)
            {
                if (player >= 0 && !Physics.GetIgnoreLayerCollision(player, layer))
                    mask |= 1 << layer;
            }

            mask &= ~LayerMask.GetMask("Obstacle", "Water", "Player", "PlayerInvisibleWalls", "PlayerDrop", "Ignore Raycast");
            _supportMask = mask | GroundMask;
            return _supportMask;
        }
    }

    /// <summary>
    /// The surface the player's feet would stand on at <paramref name="near"/>, the one nearest its height, so a spot
    /// taken from the navmesh, whose height is only roughly the ground's, is tested where the feet really are.
    /// <paramref name="onGround"/> is whether that surface is Ground or Step, the only surfaces the game walls off at a
    /// ledge. False when there is nothing to stand on within reach.
    /// </summary>
    internal static bool TryFindSupport(Vector3 near, out Vector3 feet, out bool onGround)
    {
        feet = near;
        onGround = false;
        var count = Physics.RaycastNonAlloc(near + Vector3.up * _searchAbove, Vector3.down, _groundHits, _searchAbove + _searchBelow, SupportMask, QueryTriggerInteraction.Ignore);
        var best = float.MaxValue;
        for (var i = 0; i < count; i++)
        {
            var hit = _groundHits[i];
            var gap = Mathf.Abs(hit.point.y - near.y);
            if (gap >= best || IsPlayer(hit.collider))
                continue;

            best = gap;
            feet = hit.point;
            onGround = IsGround(hit.collider);
        }

        return best < float.MaxValue;
    }

    /// <summary>
    /// Whether feet at <paramref name="feet"/> stand without the game walling them off, on Ground or Step with every
    /// ledge ray finding ground, or on any other surface, where the game raises no walls.
    /// </summary>
    internal static bool StandsClear(Vector3 feet, bool onGround) => !onGround || HasGroundAround(feet);

    /// <summary>Every solid surface under <paramref name="near"/> within reach, its layer and height, for the walk's log line.</summary>
    internal static string DescribeSurfaces(Vector3 near)
    {
        var hits = Physics.RaycastAll(near + Vector3.up * _searchAbove, Vector3.down, _searchAbove + _searchBelow, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        List<string> found = [];
        foreach (var hit in hits.OrderBy(hit => hit.distance))
        {
            if (IsPlayer(hit.collider))
                continue;

            var rise = hit.point.y - near.y;
            found.Add($"'{hit.collider.name}' on {LayerMask.LayerToName(hit.collider.gameObject.layer)} {Mathf.Abs(rise):0.00} {(rise >= 0f ? "above" : "below")}");
        }

        return found.Count > 0 ? $"surfaces {string.Join(", ", found)}" : "no surface within reach";
    }

    private static bool IsGround(Collider? collider) => collider != null && (GroundMask & (1 << collider.gameObject.layer)) != 0;

    private static bool IsPlayer(Collider? collider) =>
        collider != null && TimbnGame.IsInGame && MainGame.PlayerController is { } player && collider.attachedRigidbody == player.PhysicalBody.Rb;

    /// <summary>Whether all eight of the game's ledge rays find ground around feet standing on <paramref name="ground"/>.</summary>
    internal static bool HasGroundAround(Vector3 ground) => MissingRays(ground) == 0;

    /// <summary>Which of the game's ledge rays find no ground around feet on <paramref name="ground"/>, for the walk's log line.</summary>
    internal static string DescribeMissing(Vector3 ground)
    {
        if (!Measure())
            return "no ledge rays to test";

        List<string> missing = [];
        var from = ground + Vector3.up * _lift;
        for (var i = 0; i < _probes.Count; i++)
        {
            if (!Physics.Raycast(from + _probes[i], Vector3.down, _length, GroundMask, QueryTriggerInteraction.Ignore))
                missing.Add($"({_probes[i].x:0.00}, {_probes[i].z:0.00})");
        }

        return missing.Count > 0 ? $"the ledge rays at {string.Join(", ", missing)} find no ground" : "every ledge ray finds ground";
    }

    private static int MissingRays(Vector3 ground)
    {
        if (!Measure())
            return 0;

        var missing = 0;
        var from = ground + Vector3.up * _lift;
        foreach (var probe in _probes)
        {
            if (!Physics.Raycast(from + probe, Vector3.down, _length, GroundMask, QueryTriggerInteraction.Ignore))
                missing++;
        }

        return missing;
    }

    /// <summary>
    /// Closes every open cell from <paramref name="start"/> on, <paramref name="count"/> of them, with no ground under it
    /// or where a ledge ray finds no ground, casting the rays as batches of physics jobs. Marks each one it closes with
    /// <paramref name="reason"/> in <paramref name="why"/> and returns how many it closed.
    /// </summary>
    internal static int CloseOffLedges(Vector3[] positions, bool[] open, int start, int count, byte[] why, byte reason)
    {
        if (!Measure())
            return 0;

        List<int> cells = [];
        for (var i = start; i < start + count; i++)
        {
            if (open[i])
                cells.Add(i);
        }

        if (cells.Count == 0)
            return 0;

        var query = new QueryParameters(GroundMask, false, QueryTriggerInteraction.Ignore, false);
        var support = new QueryParameters(SupportMask, false, QueryTriggerInteraction.Ignore, false);
        var closed = 0;
        var grounds = new NativeArray<RaycastCommand>(cells.Count, Allocator.TempJob);
        var groundHits = new NativeArray<RaycastHit>(cells.Count * _maxGroundHits, Allocator.TempJob);
        var probes = _probes.Count;
        var commands = new NativeArray<RaycastCommand>(cells.Count * probes, Allocator.TempJob);
        var hits = new NativeArray<RaycastHit>(cells.Count * probes, Allocator.TempJob);
        var found = new bool[cells.Count];
        var onGround = new bool[cells.Count];
        try
        {
            for (var j = 0; j < cells.Count; j++)
                grounds[j] = new RaycastCommand(positions[cells[j]] + Vector3.up * _searchAbove, Vector3.down, support, _searchAbove + _searchBelow);

            RaycastCommand.ScheduleBatch(grounds, groundHits, _cellsPerJob, _maxGroundHits).Complete();
            for (var j = 0; j < cells.Count; j++)
            {
                var near = positions[cells[j]].y;
                var best = float.MaxValue;
                for (var k = 0; k < _maxGroundHits; k++)
                {
                    var hit = groundHits[j * _maxGroundHits + k];
                    if (!hit.colliderEntityId.IsValid())
                        break;

                    if (Mathf.Abs(hit.point.y - near) >= best || IsPlayer(hit.collider))
                        continue;

                    best = Mathf.Abs(hit.point.y - near);
                    positions[cells[j]].y = hit.point.y;
                    found[j] = true;
                    onGround[j] = IsGround(hit.collider);
                }

                var from = found[j] ? positions[cells[j]] + Vector3.up * _lift : positions[cells[j]] + Vector3.up * _searchAbove * 100f;
                for (var k = 0; k < probes; k++)
                    commands[j * probes + k] = new RaycastCommand(from + _probes[k], Vector3.down, query, _length);
            }

            RaycastCommand.ScheduleBatch(commands, hits, _cellsPerJob, 1).Complete();
            for (var j = 0; j < cells.Count; j++)
            {
                var standing = found[j];
                for (var k = 0; standing && onGround[j] && k < probes; k++)
                    standing = hits[j * probes + k].colliderEntityId.IsValid();

                if (standing)
                    continue;

                open[cells[j]] = false;
                why[cells[j]] = reason;
                closed++;
            }

            return closed;
        }
        finally
        {
            grounds.Dispose();
            groundHits.Dispose();
            commands.Dispose();
            hits.Dispose();
        }
    }

    private static bool Measure()
    {
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        var walls = player != null && _walls != null ? _walls(player.PhysicalBody) : null;
        if (walls == null)
            return false;

        if (walls == _measured)
            return true;

        _measured = walls;
        _length = Read(_rayLength, walls, 0.6f);
        _lift = Read(_rayLift, walls, 0.05f);
        var sideX = Read(_sideX, walls, 0.25f);
        var ahead = Read(_ahead, walls, 0.4f);
        var behind = Read(_behind, walls, 0.1f);
        var cornerX = Read(_cornerX, walls, 0.25f);
        var cornerAhead = Read(_cornerAhead, walls, 0.4f);
        var cornerBehind = Read(_cornerBehind, walls, 0.1f);
        _probes.Clear();
        _probes.Add(new Vector3(sideX, 0f, 0f));
        _probes.Add(new Vector3(-sideX, 0f, 0f));
        _probes.Add(new Vector3(0f, 0f, ahead));
        _probes.Add(new Vector3(0f, 0f, -behind));
        _probes.Add(new Vector3(cornerX, 0f, cornerAhead));
        _probes.Add(new Vector3(-cornerX, 0f, cornerAhead));
        _probes.Add(new Vector3(cornerX, 0f, -cornerBehind));
        _probes.Add(new Vector3(-cornerX, 0f, -cornerBehind));
        return true;
    }

    private static float Read(AccessTools.FieldRef<PlayerInvisibleWalls, float>? field, PlayerInvisibleWalls walls, float fallback) =>
        field != null ? field(walls) : fallback;
}
