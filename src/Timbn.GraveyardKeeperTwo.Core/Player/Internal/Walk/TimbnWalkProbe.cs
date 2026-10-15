using Unity.Collections;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Tests the player's real body against the world for a player walk, whether it fits at a spot and where a straight
/// line first stops fitting, and names what touches it for the walk's log line. The body is the solid colliders on the
/// player's rigidbody that hit obstacles, placed at a spot without moving them and measured with PhysX's own
/// penetration test, the way the game's <c>PlayerColliderTester</c> checks a spot. The body never turns, so this is its
/// true shape whichever way the player moves.
/// </summary>
internal static class TimbnWalkProbe
{
    /// <summary>
    /// How far the body may sink into an obstacle and still count as fitting, about physics' own contact offset, since
    /// physics lets the body sink into nothing and a gap that needs more wedges the player. The grid and the check
    /// before walking allow the same, so the check never turns down a spot the grid let through.
    /// </summary>
    internal const float Tolerance = 0.01f;

    /// <summary>How far apart a route is tested for the player's body.</summary>
    internal const float SampleStep = 0.05f;

    private const float _touchMargin = 0.1f;
    private const int _maxNamed = 3;
    private const int _pathDepth = 4;
    private const int _maxHits = 16;
    private const int _cellsPerJob = 64;
    private const float _pushStep = 0.05f;
    private const float _pushReach = 0.4f;
    private const float _pushMargin = 0.05f;
    private const float _screenMargin = 0.03f;

    private static readonly List<Part> _parts = [];
    private static readonly Collider[] _nearby = new Collider[32];
    private static Rigidbody? _body;
    private static float _reach;
    private static Vector3 _middle;

    /// <summary>
    /// Whether the player's body fits at <paramref name="feet"/>, overlapping nothing on <paramref name="mask"/> by more
    /// than <paramref name="tolerance"/>.
    /// </summary>
    internal static bool Fits(Vector3 feet, int mask, float tolerance)
    {
        if (!Measure())
            return true;

        foreach (var part in _parts)
        {
            if (part.Penetrates(feet, mask, tolerance))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Closes every open cell from <paramref name="start"/> on, <paramref name="count"/> of them, where the player's
    /// body, with the feet at the cell in <paramref name="positions"/>, sinks into anything on <paramref name="mask"/>
    /// deeper than <paramref name="tolerance"/>, and returns how many it closed. The search for what is near each cell
    /// runs as one batch of physics jobs, and the penetration test only runs where that search found something.
    /// </summary>
    internal static int CloseWhereBodyDoesNotFit(Vector3[] positions, bool[] walkable, int start, int count, int mask, float tolerance, byte[] why, byte reason)
    {
        if (!Measure())
            return 0;

        List<int> cells = [];
        for (var i = start; i < start + count; i++)
        {
            if (walkable[i])
                cells.Add(i);
        }

        var closed = 0;
        Dictionary<int, Collider?> colliders = [];
        foreach (var part in _parts)
        {
            var layers = mask & part.Mask;
            if (layers == 0 || cells.Count == 0)
                continue;

            var query = new QueryParameters(layers, false, QueryTriggerInteraction.Ignore, false);
            var commands = new NativeArray<OverlapBoxCommand>(cells.Count, Allocator.TempJob);
            var hits = new NativeArray<ColliderHit>(cells.Count * _maxHits, Allocator.TempJob);
            try
            {
                for (var j = 0; j < cells.Count; j++)
                    commands[j] = new OverlapBoxCommand(positions[cells[j]] + part.BoundsOffset, part.SearchExtents, Quaternion.identity, query);

                OverlapBoxCommand.ScheduleBatch(commands, hits, _cellsPerJob, _maxHits).Complete();
                for (var j = 0; j < cells.Count; j++)
                {
                    var index = cells[j];
                    if (!walkable[index])
                        continue;

                    for (var k = 0; k < _maxHits; k++)
                    {
                        var hit = hits[j * _maxHits + k];
                        if (hit.instanceID == 0)
                            break;

                        if (!colliders.TryGetValue(hit.instanceID, out var other))
                            colliders[hit.instanceID] = other = hit.collider;

                        if (other == null || IsPlayer(other) || !part.Sinks(positions[index], other, tolerance))
                            continue;

                        walkable[index] = false;
                        why[index] = reason;
                        closed++;
                        break;
                    }
                }
            }
            finally
            {
                commands.Dispose();
                hits.Dispose();
            }
        }

        return closed;
    }

    /// <summary>
    /// The first spot on the straight line from <paramref name="from"/> to <paramref name="to"/> where the player's body
    /// does not fit, sampled every few centimetres. False when it fits all the way.
    /// </summary>
    internal static bool TryFindBlock(Vector3 from, Vector3 to, out Vector3 at)
    {
        at = default;
        if (!Measure())
            return false;

        var mask = TimbnPlayerGrid.ObstacleMask;
        var steps = Mathf.Max(1, Mathf.CeilToInt(TimbnWorld.FlatDistance(from, to) / SampleStep));
        for (var i = 1; i <= steps; i++)
        {
            var point = TimbnPlayerGrid.GroundAt(Vector3.Lerp(from, to, (float)i / steps));
            if (Stands(point, mask))
                continue;

            at = point;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Whether the player's body fits all along the straight line from <paramref name="from"/> to <paramref name="to"/>,
    /// sampled every <paramref name="step"/> past the start. Cheap enough to ask every physics step, so it takes the
    /// heights along the line as they are rather than looking up the ground.
    /// </summary>
    internal static bool IsClear(Vector3 from, Vector3 to, float step)
    {
        if (!Measure())
            return true;

        var mask = TimbnPlayerGrid.ObstacleMask;
        var steps = Mathf.Max(1, Mathf.CeilToInt(TimbnWorld.FlatDistance(from, to) / step));
        for (var i = 1; i <= steps; i++)
        {
            if (!Stands(Vector3.Lerp(from, to, (float)i / steps), mask))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the body fits with its feet on the ground at <paramref name="feet"/>, the surface nearest that height,
    /// with ground under it on every side.
    /// </summary>
    internal static bool Stands(Vector3 feet, int mask) =>
        TimbnLedge.TryFindSupport(feet, out var ground, out var onGround) && Fits(ground, mask, Tolerance) && TimbnLedge.StandsClear(ground, onGround);

    /// <summary>
    /// Whether the world at <paramref name="point"/> is loaded, so the body and ledge tests mean something there. The game
    /// only keeps the chunks the camera sees active, colliders and ground included, so this is whether the point is on
    /// screen, a little in from its edges. True when there is no camera to ask.
    /// </summary>
    internal static bool IsLoaded(Vector3 point)
    {
        var camera = CameraSystem.Instance != null && CameraSystem.Instance.MainCamera != null ? CameraSystem.Instance.MainCamera.Camera : null;
        if (camera == null)
            return true;

        var view = camera.WorldToViewportPoint(point);
        return view.z > 0f && view.x >= _screenMargin && view.x <= 1f - _screenMargin && view.y >= _screenMargin && view.y <= 1f - _screenMargin;
    }

    /// <summary>Whether the body fits at <paramref name="feet"/> with ground under it on every side, against the obstacles the player grid counts.</summary>
    internal static bool Stands(Vector3 feet) => Stands(feet, TimbnPlayerGrid.ObstacleMask);

    /// <summary>
    /// The nearest spot across <paramref name="heading"/> from <paramref name="point"/> where the player's body stands,
    /// a little further out when that fits too, so a route that runs into an obstacle or past a ledge is moved off it.
    /// <paramref name="side"/> is 0 to try both sides, and once a side is found it is kept, so a run of moved spots
    /// stays on one side of what is in the way. False when nothing within reach fits.
    /// </summary>
    internal static bool TryPush(Vector3 point, Vector3 heading, ref int side, out Vector3 pushed)
    {
        pushed = point;
        var mask = TimbnPlayerGrid.ObstacleMask;
        var across = new Vector3(-heading.z, 0f, heading.x).normalized;
        for (var shift = _pushStep; shift <= _pushReach + 0.001f; shift += _pushStep)
        {
            for (var sign = 1; sign >= -1; sign -= 2)
            {
                if (side != 0 && sign != side)
                    continue;

                var spot = TimbnPlayerGrid.GroundAt(point + across * (sign * shift));
                if (!Stands(spot, mask))
                    continue;

                var further = TimbnPlayerGrid.GroundAt(point + across * (sign * (shift + _pushMargin)));
                pushed = Stands(further, mask) ? further : spot;
                side = sign;
                return true;
            }
        }

        return false;
    }

    /// <summary>Why the player's body does not fit at <paramref name="feet"/>, what it sinks into or the ledge it stands at.</summary>
    internal static string WhyBlocked(Vector3 feet)
    {
        if (!TimbnLedge.TryFindSupport(feet, out var ground, out var onGround))
            return $"finds nothing to stand on, {TimbnLedge.DescribeSurfaces(feet)}";

        var rise = ground.y - feet.y;
        var height = $"on {(onGround ? "ground" : "a surface that is not ground")} {Mathf.Abs(rise):0.00} {(rise >= 0f ? "above" : "below")} the navmesh";
        if (!Fits(ground, TimbnPlayerGrid.ObstacleMask, Tolerance))
            return $"{height} {WhatOverlaps(ground)}";

        return TimbnLedge.StandsClear(ground, onGround)
            ? $"{height} fits on a second look"
            : $"{height} stands at a ledge, {TimbnLedge.DescribeMissing(ground)}, {TimbnLedge.DescribeSurfaces(feet)}";
    }

    /// <summary>What the player's body sinks into, and how deep, when the feet are at <paramref name="feet"/>.</summary>
    internal static string WhatOverlaps(Vector3 feet)
    {
        if (!Measure())
            return "has no shape to test";

        List<string> found = [];
        foreach (var part in _parts)
            part.Describe(feet, TimbnPlayerGrid.ObstacleMask, found);

        return found.Count > 0 ? $"sinks into {string.Join("; ", found.Take(_maxNamed))}" : "sinks into nothing on a second look";
    }

    /// <summary>
    /// Every solid collider of any layer touching the player's body, and how far above or below the player
    /// <paramref name="corner"/> is.
    /// </summary>
    internal static string WhatBlocks(Vector3 corner)
    {
        var player = MainGame.PlayerController;
        if (player == null || !Measure())
            return "no player body found";

        var from = player.transform.position;
        List<string> found = [];
        foreach (var hit in Physics.OverlapSphere(from + _middle, _reach + _touchMargin, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (IsPlayer(hit))
                continue;

            found.Add(Describe(hit));
            if (found.Count == _maxNamed)
                break;
        }

        var rise = corner.y - from.y;
        var what = found.Count > 0 ? $"touching {string.Join("; ", found)}" : "touching nothing";
        return $"{what}, next corner ({corner.x:0.00}, {corner.z:0.00}) is {Mathf.Abs(rise):0.00} {(rise >= 0f ? "above" : "below")}";
    }

    private static bool Measure()
    {
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player == null)
            return false;

        var body = player.PhysicalBody.Rb;
        if (body == _body && _body != null)
            return _parts.Count > 0;

        _body = body;
        _parts.Clear();
        var feet = player.transform.position;
        Bounds? all = null;
        List<string> described = [];
        foreach (var collider in body.GetComponentsInChildren<Collider>())
        {
            if (collider.isTrigger || !collider.enabled || collider.attachedRigidbody != body)
                continue;

            var bounds = collider.bounds;
            var mask = CollidesWith(collider.gameObject.layer);
            var hitsObstacles = (mask & TimbnPlayerGrid.ObstacleMask) != 0;
            described.Add($"{collider.GetType().Name} '{collider.name}' on {LayerMask.LayerToName(collider.gameObject.layer)}, {bounds.size.x:0.00} by {bounds.size.z:0.00}, {(hitsObstacles ? "hits" : "passes through")} obstacles");
            if (!hitsObstacles)
                continue;

            _parts.Add(new Part(collider, feet, mask));
            if (all is { } sum)
            {
                sum.Encapsulate(bounds);
                all = sum;
            }
            else
            {
                all = bounds;
            }
        }

        if (all is { } total)
        {
            _middle = total.center - feet;
            _reach = Mathf.Max(total.extents.x, total.extents.z);
        }

        TimbnCorePlugin.Logger.LogDebug($"{nameof(TimbnPluginPlayer)}.{nameof(TimbnPluginPlayer.WalkTo)}|The player's body is {string.Join("; ", described)}.");
        return _parts.Count > 0;
    }

    private static bool IsPlayer(Collider collider) => _body != null && collider.attachedRigidbody == _body;

    private static int CollidesWith(int layer)
    {
        var mask = 0;
        for (var other = 0; other < 32; other++)
        {
            if (!Physics.GetIgnoreLayerCollision(layer, other))
                mask |= 1 << other;
        }

        return mask;
    }

    private static string Describe(Collider collider)
    {
        List<string> names = [];
        for (var node = collider.transform; node != null && names.Count < _pathDepth; node = node.parent)
            names.Add(node.name);

        names.Reverse();
        var layer = LayerMask.LayerToName(collider.gameObject.layer);
        return $"'{string.Join("/", names)}' ({layer}{(collider.isTrigger ? ", trigger" : "")})";
    }

    /// <summary>One solid collider of the player's body, kept as offsets from the feet.</summary>
    private readonly struct Part
    {
        private readonly Collider _collider;
        private readonly Vector3 _offset;

        public Part(Collider collider, Vector3 feet, int mask)
        {
            var bounds = collider.bounds;
            _collider = collider;
            _offset = collider.transform.position - feet;
            BoundsOffset = bounds.center - feet;
            SearchExtents = bounds.extents;
            Mask = mask;
        }

        /// <summary>The middle of this part's bounds from the feet.</summary>
        public Vector3 BoundsOffset { get; }

        /// <summary>Half the size of this part's bounds, the box to search for what it may touch.</summary>
        public Vector3 SearchExtents { get; }

        /// <summary>The layers this part's own layer collides with.</summary>
        public int Mask { get; }

        /// <summary>Whether this part, placed with the feet at <paramref name="feet"/>, sinks into <paramref name="other"/> deeper than <paramref name="tolerance"/>.</summary>
        public bool Sinks(Vector3 feet, Collider other, float tolerance) =>
            Physics.ComputePenetration(_collider, feet + _offset, _collider.transform.rotation, other, other.transform.position, other.transform.rotation, out _, out var depth)
            && depth > tolerance;

        /// <summary>Adds what this part, placed with the feet at <paramref name="feet"/>, sinks into and how deep.</summary>
        public void Describe(Vector3 feet, int mask, List<string> found)
        {
            var layers = mask & Mask;
            var position = feet + _offset;
            var rotation = _collider.transform.rotation;
            var count = Physics.OverlapBoxNonAlloc(feet + BoundsOffset, SearchExtents, _nearby, Quaternion.identity, layers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var other = _nearby[i];
                if (!IsPlayer(other) && Physics.ComputePenetration(_collider, position, rotation, other, other.transform.position, other.transform.rotation, out _, out var depth))
                    found.Add($"{TimbnWalkProbe.Describe(other)} {depth:0.00} deep");
            }
        }

        /// <summary>Whether this part, placed with the feet at <paramref name="feet"/>, sinks into anything deeper than <paramref name="tolerance"/>.</summary>
        public bool Penetrates(Vector3 feet, int mask, float tolerance)
        {
            var layers = mask & Mask;
            if (layers == 0)
                return false;

            var position = feet + _offset;
            var rotation = _collider.transform.rotation;
            var count = Physics.OverlapBoxNonAlloc(feet + BoundsOffset, SearchExtents, _nearby, Quaternion.identity, layers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var other = _nearby[i];
                if (IsPlayer(other))
                    continue;

                if (Physics.ComputePenetration(_collider, position, rotation, other, other.transform.position, other.transform.rotation, out _, out var depth) && depth > tolerance)
                    return true;
            }

            return false;
        }
    }
}
