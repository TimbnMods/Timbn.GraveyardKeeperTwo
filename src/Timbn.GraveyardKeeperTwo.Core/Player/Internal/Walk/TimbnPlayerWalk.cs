using LazyBearTechnology;
using Pathfinding;
using System.Collections;
using System.Runtime.CompilerServices;
using Path = Pathfinding.Path;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The player walking to a spot along the navmesh by steering their own movement, the way they would with the
/// movement keys, so their collision, ledges and speed all apply. In free play the game's own movement call is steered
/// and any movement input ends the walk. While plugins hold control, Core moves the player itself and ignores input.
/// Control the game takes, such as a talk, ends the walk. Planning runs in a coroutine a slice per frame, so a long walk
/// never stalls a frame, and a newer walk takes over once its route is found, so the player keeps moving while it plans.
/// A plan stops as soon as a newer one replaces it, and only one plan at a time lays out and uses the player grid.
/// </summary>
internal sealed class TimbnPlayerWalk : IDisposable
{
    private const double _frameBudget = 4d;
    private const int _maxRepairs = 4;
    private const float _startSlack = 0.2f;
    private const float _sameStart = 0.3f;
    private const float _arriveRadius = 0.15f;
    private const float _legEndRadius = 0.3f;
    private const float _endTolerance = 0.5f;
    private const float _reachTolerance = 0.3f;
    private const float _depthMovementScale = 0.8f;
    private const float _lookahead = 0.5f;
    private const float _shortestAim = 0.1f;
    private const float _aimStep = 0.1f;
    private const float _simplifyTolerance = 0.02f;
    private const float _searchAhead = 1f;
    private const float _legLength = 16f;
    private const float _handover = 4f;

    // A leg cut where the screen ends reaches only as far as the view, about 4 units up or down at 720p (the camera's
    // size follows the window height), so planning the next one at the usual handover would replan every few frames.
    private const float _screenHandover = 2f;
    private const float _legWait = 5f;
    private const float _joinGap = 2.5f;
    private const float _detourPad = 1f;
    private const float _boxReach = 2f;
    private const int _maxReroutes = 3;
    private const float _avoidStep = 0.25f;
    private const int _maxRecoveries = 2;
    private const float _stallRadius = 0.25f;
    private const float _progressStep = 0.02f;
    private const float _stuckSeconds = 0.6f;
    private const float _pausedSeconds = 0.25f;

    private static readonly AccessTools.FieldRef<GlobalNavigationManager, Pool>? _pathUnits = TimbnReflection.TryField<GlobalNavigationManager, Pool>("pathCalculationUnitPool");

    private static TimbnPlayerWalk? _current;
    private static TimbnPlayerWalk? _latest;
    private static bool _driving;
    private static TimbnPlayerWalk? _gridUser;
    private static int _gridGeneration;

    private readonly Vector3 _target;
    private readonly Action? _arrived;
    private readonly ManualLogSource _logger;
    private readonly TimbnFrameBudget _budget = new(_frameBudget);
    private readonly HashSet<uint> _avoided = [];
    private readonly List<Vector3> _stalls = [];
    private TimbnRoute? _route;
    private string? _noWay;
    private List<Vector3> _noWayStretch = [];
    private int _reroutes;
    private int _recoveries;
    private int _generation;
    private bool _replanning;
    private float _aimAhead;
    private float _requestedAt;
    private bool _final;
    private bool _held;
    private bool _cutShort;
    private string? _scene;
    private int _legs;
    private int _requestedFrame;
    private IDisposable? _unfrozen;
    private float _progress;
    private float _best;
    private float _lastProgress;
    private float _lastSteer;
    private float _started;
    private float _length;

    private TimbnPlayerWalk(Vector3 target, Action? arrived, ManualLogSource logger)
    {
        _target = target;
        _arrived = arrived;
        _logger = logger;
    }

    internal bool IsDone { get; private set; }

    internal Action? Ended { get; set; }

    internal bool Arrived { get; private set; }

    private string Where => $"{nameof(TimbnPluginPlayer)}.{nameof(TimbnPluginPlayer.WalkTo)}|({_target.x:0.00}, {_target.z:0.00})";

    /// <summary>
    /// True once the walk is over, replaced by a newer one, or the player has left the scene it was planned in, which
    /// also stops it.
    /// </summary>
    private bool IsAbandoned
    {
        get
        {
            if (!IsDone && TimbnGame.IsInGame && MainGame.PlayerData.currentGameSceneId != _scene)
                Stop($"{Where} The player left the scene, so the walk stops.");

            return IsDone || (_route == null ? _latest != this : _current != this);
        }
    }

    /// <summary>True once this walk is abandoned or a newer plan of it (after a stall) has replaced the one at <paramref name="generation"/>.</summary>
    private bool IsStale(int generation) => IsAbandoned || generation != _generation;

    internal static TimbnPlayerWalk Start(Vector3 position, Action? arrived, ManualLogSource logger)
    {
        var walk = new TimbnPlayerWalk(position, arrived, logger);
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player == null)
        {
            walk.IsDone = true;
            return walk;
        }

        // One still planning would otherwise just drop out on IsAbandoned and never tell whoever waits on Ended.
        if (_latest is { IsDone: false, _route: null } replaced)
            replaced.End();

        _latest = walk;
        TimbnPlayerControl.ReleaseFacing(player);

        // Any plugin hold counts, since the game may still hold control for a closing window when the walk starts.
        walk._held = TimbnPlayerControl.HasPluginHolds;
        walk._scene = MainGame.PlayerData.currentGameSceneId;
        walk._requestedAt = Time.realtimeSinceStartup;
        walk._requestedFrame = Time.frameCount;
        if (TimbnCorePlugin.Instance is not { } core)
        {
            walk.Stop($"{walk.Where} Core is not running, so the player stays put.");
            return walk;
        }

        core.StartCoroutine(walk.Plan(walk._generation));
        return walk;
    }

    /// <summary>
    /// Turns the movement input the game is about to apply in free play into a step along the current walk. Input of
    /// the player's own ends the walk instead.
    /// </summary>
    internal static void Steer(PlayerPhysicalBody body, ref Vector2 direction)
    {
        var walk = _current;
        if (walk == null || walk._route == null || _driving || TimbnPlayerControl.IsHeldByPlugins)
            return;

        var player = MainGame.PlayerController;
        if (direction != Vector2.zero || player == null || !player.IsControlsEnabled
            || Time.fixedTime - walk._lastSteer > _pausedSeconds)
        {
            walk.End();
            return;
        }

        if (player.Ssm.CurState is not FreePlayerState)
            return;

        walk._lastSteer = Time.fixedTime;
        direction = walk.NextDirection(body.transform.position);
    }

    /// <summary>
    /// Moves the player one physics step along the current walk while plugins hold control, since the game's free
    /// movement does not run then, and ends the walk when the game takes control.
    /// </summary>
    internal static void FixedTick()
    {
        var walk = _current;
        if (walk == null || walk._route == null)
            return;

        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player == null || TimbnPlayerControl.IsTakenByGame([]))
        {
            walk.End();
            return;
        }

        if (!TimbnPlayerControl.IsHeldByPlugins)
        {
            walk.Unfreeze(false);
            return;
        }

        if (!walk._held)
        {
            walk.Stop($"{walk.Where} A plugin took control, so the walk the player started stops.");
            return;
        }

        walk.Unfreeze(true);
        walk._lastSteer = Time.fixedTime;
        var body = player.PhysicalBody;
        var direction = walk.NextDirection(body.transform.position);
        _driving = true;
        try
        {
            body.MoveByDirection(direction);
        }
        finally
        {
            _driving = false;
        }
    }

    public void Dispose()
    {
        if (IsDone)
            return;

        IsDone = true;
        Forget();
    }

    /// <summary>
    /// Plans the walk a leg at a time. Each leg is the navmesh's own route, the one the game's walker follows, checked
    /// against the player's body and the game's ledge rays before the player sets off. Only the stretches the player does
    /// not fit through are replaced, each by a detour found on the player grid laid over a small box around it. A long
    /// walk plans its next leg in the background as the player nears the end of the current one. Every step waits a
    /// frame rather than running inside a route's callback, where scanning or flushing graphs breaks an update the game
    /// has in progress.
    /// </summary>
    private IEnumerator Plan(int generation)
    {
        var navigation = LazySingleton<GlobalNavigationManager>.Instance;
        if (navigation == null || !TimbnWorld.TryGetNavGraph(null, out var graph))
        {
            Fail($"{Where} No navmesh here, so the player stays put.");
            yield break;
        }

        var navmeshMask = GraphMask.FromGraphIndex(graph.graphIndex);
        while (true)
        {
            if (_route != null)
            {
                _requestedAt = Time.realtimeSinceStartup;
                _requestedFrame = Time.frameCount;
            }

            var guide = new StrongBox<Path?>();
            yield return NavmeshRoute(generation, navigation, navmeshMask, guide);
            if (IsStale(generation))
                yield break;

            var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
            if (player == null || guide.Value is not { error: false, vectorPath.Count: > 0 } navmeshRoute)
            {
                Fail($"{Where} No way there over the navmesh{(_avoided.Count > 0 ? $" round the {_avoided.Count} triangles the player does not fit through" : "")}, so the player stays put.");
                yield break;
            }

            var start = player.transform.position;
            var skip = TimbnWorld.FlatDistance(navmeshRoute.vectorPath[0], start) < _sameStart ? 1 : 0;
            var route = new TimbnRoute([start, .. navmeshRoute.vectorPath.Skip(skip), _target]);
            var leg = new StrongBox<TimbnRoute?>();
            var final = new StrongBox<bool>();
            var atScreenEdge = new StrongBox<bool>();
            yield return PlanLeg(generation, route, leg, final, atScreenEdge);
            if (IsStale(generation))
                yield break;

            if (leg.Value is not { } planned)
            {
                if (_noWay is not { } why)
                    yield break;

                _noWay = null;
                if (_reroutes >= _maxReroutes)
                {
                    Fail($"{why}, so the player stays put.");
                    yield break;
                }

                _reroutes++;
                var avoided = Avoid(graph, start);
                _logger.LogDebug($"{why}. Asking the navmesh again round {avoided} more triangles.");
                continue;
            }

            Walk(planned, final.Value);
            if (final.Value)
                yield break;

            var handover = atScreenEdge.Value ? _screenHandover : _handover;
            while (!IsStale(generation) && Remaining() > handover)
            {
                yield return null;
                _budget.Restart();
            }

            if (IsStale(generation))
                yield break;
        }
    }

    /// <summary>
    /// Shuts the navmesh triangles under the stretch no detour got round out of this walk's navmesh routes, except the
    /// ones the player and the target stand on, and returns how many it added.
    /// </summary>
    private int Avoid(NavGraph graph, Vector3 start)
    {
        var keep = new HashSet<uint>();
        foreach (var spot in new[] { start, _target })
        {
            if (graph.GetNearest(spot, NearestNodeConstraint.Walkable).node is { } node)
                keep.Add(node.NodeIndex);
        }

        var added = 0;
        var stretch = new TimbnRoute(_noWayStretch);
        for (var along = 0f; along <= stretch.Length + _avoidStep / 2f; along += _avoidStep)
        {
            if (graph.GetNearest(stretch.PointAt(along), NearestNodeConstraint.Walkable).node is { } node && !keep.Contains(node.NodeIndex) && _avoided.Add(node.NodeIndex))
                added++;
        }

        return added;
    }

    /// <summary>
    /// Records that no detour got round the stretch of <paramref name="route"/> from <paramref name="from"/> to
    /// <paramref name="to"/>, so the leg is planned again over the navmesh round it.
    /// </summary>
    private void NoWayRound(string why, TimbnRoute route, float from, float to)
    {
        _noWay = why;
        _noWayStretch = route.Slice(from, to);
    }

    /// <summary>
    /// Walks the navmesh route from where the player stands every few centimetres. A spot where the player's body does
    /// not stand is moved across the route to the nearest one where it does, on the same side as the spot before it,
    /// and a spot with none within reach is marked. The route is cut to about <see cref="_legLength"/> at a spot the
    /// player fits, or where it leaves the screen, since the game only loads the world the camera sees and nothing past
    /// it can be tested. Each marked stretch is swapped for a detour, and the result is thinned to the corners it needs.
    /// Sets <paramref name="result"/> to the leg to walk, or stops the walk and leaves it null.
    /// </summary>
    private IEnumerator PlanLeg(int generation, TimbnRoute route, StrongBox<TimbnRoute?> result, StrongBox<bool> final, StrongBox<bool> atScreenEdge)
    {
        List<Vector3> points = [route.PointAt(0f)];
        List<float> blocked = [];
        var lastBlocked = float.MinValue;
        var length = 0f;
        var side = 0;
        var pushed = 0;
        var unseen = false;
        final.Value = true;
        for (var distance = TimbnWalkProbe.SampleStep; distance < route.Length + TimbnWalkProbe.SampleStep; distance += TimbnWalkProbe.SampleStep)
        {
            var along = Mathf.Min(distance, route.Length);
            var point = TimbnPlayerGrid.GroundAt(route.PointAt(along));
            if (along > _startSlack && !TimbnWalkProbe.IsLoaded(point))
            {
                unseen = true;
                final.Value = false;
                atScreenEdge.Value = true;
                break;
            }

            var stands = along <= _startSlack || (TimbnWalkProbe.Stands(point) && !NearStall(point));
            var fits = stands;
            if (stands)
            {
                side = 0;
            }
            else if (TimbnWalkProbe.TryPush(point, route.HeadingAt(along), ref side, out var moved) && !NearStall(moved))
            {
                point = moved;
                fits = true;
                pushed++;
            }
            else
            {
                side = 0;
            }

            length += TimbnWorld.FlatDistance(points[^1], point);
            points.Add(point);
            if (!fits)
            {
                blocked.Add(length);
                lastBlocked = length;
            }

            if (stands && along >= _legLength && length - lastBlocked >= _joinGap && route.Length - along > _handover)
            {
                final.Value = false;
                break;
            }

            if (!_budget.IsSpent)
                continue;

            yield return null;
            _budget.Restart();
            if (IsStale(generation))
                yield break;
        }

        var leg = new TimbnRoute(points);
        var stretches = TimbnRoute.Stretches(blocked, _joinGap);
        if (unseen && stretches.Count > 0 && length - stretches[^1].To < _joinGap)
        {
            leg = new TimbnRoute(leg.Slice(0f, Mathf.Max(0f, stretches[^1].From - _detourPad)));
            stretches.RemoveAt(stretches.Count - 1);
        }

        for (var i = stretches.Count - 1; i >= 0; i--)
        {
            var detoured = new StrongBox<TimbnRoute?>();
            yield return Detour(generation, leg, stretches[i].From, stretches[i].To, final.Value, detoured);
            if (IsStale(generation) || detoured.Value is not { } spliced)
                yield break;

            leg = spliced;
        }

        result.Value = leg.Simplified(_simplifyTolerance);
        _logger.LogDebug($"{Where} Checked {length:0.0} units of the navmesh route every {TimbnWalkProbe.SampleStep:0.00}{(unseen ? ", up to the edge of the screen" : "")}, {pushed} spots moved off what is in the way and {stretches.Count} stretches detoured round.");
    }

    /// <summary>
    /// Replaces the stretch of <paramref name="route"/> from <paramref name="from"/> to <paramref name="to"/> along it,
    /// where the player does not fit, with a way round found on the player grid laid over a small box around it. The
    /// detour joins the route a little before and after the stretch, at spots the player fits. Sets
    /// <paramref name="result"/> to the route with the detour in it, or leaves it null and records the stretch so the
    /// navmesh is asked for another way.
    /// </summary>
    private IEnumerator Detour(int generation, TimbnRoute route, float from, float to, bool final, StrongBox<TimbnRoute?> result)
    {
        // The grid is one static scan, so another plan's Cover would swap it out between this plan's Cover and its routes.
        while (_gridUser is { } user && (user != this || _gridGeneration != generation) && !user.IsStale(_gridGeneration))
        {
            yield return null;
            _budget.Restart();
            if (IsStale(generation))
                yield break;
        }

        _gridUser = this;
        _gridGeneration = generation;
        try
        {
            yield return DetourOnGrid(generation, route, from, to, final, result);
        }
        finally
        {
            if (_gridUser == this && _gridGeneration == generation)
                _gridUser = null;
        }
    }

    private IEnumerator DetourOnGrid(int generation, TimbnRoute route, float from, float to, bool final, StrongBox<TimbnRoute?> result)
    {
        var entryAt = Mathf.Max(0f, from - _detourPad);
        var exitAt = Mathf.Min(route.Length, to + _detourPad);
        var entry = route.PointAt(entryAt);
        var exit = route.PointAt(exitAt);
        var endsWalk = final && exitAt >= route.Length;
        var tolerance = endsWalk ? _endTolerance : _reachTolerance;
        var first = TimbnPlayerGrid.GroundAt(route.PointAt(from));
        var stretch = route.Slice(entryAt, exitAt);
        var around = route.Slice(from - _boxReach, to + _boxReach);
        var covered = new StrongBox<bool>();
        yield return TimbnPlayerGrid.Cover(around, Box(around, _boxReach), _budget, () => IsStale(generation), covered);
        if (IsStale(generation))
            yield break;

        var plannedOn = TimbnPlayerGrid.LastScan;
        if (!covered.Value)
        {
            NoWayRound($"{Where} The player does not fit at ({first.x:0.00}, {first.z:0.00}), where the body {TimbnWalkProbe.WhyBlocked(first)}, and no way round could be laid, planned on {plannedOn}", route, from, to);
            yield break;
        }

        var found = new StrongBox<Path?>();
        yield return GridRoute(generation, entry, exit, found);
        if (IsStale(generation))
            yield break;

        var reached = found.Value is { error: false, vectorPath.Count: > 0 } path ? path.vectorPath[^1] : entry;
        var closest = TimbnWorld.FlatDistance(reached, exit);
        if (found.Value is not { error: false, vectorPath.Count: > 0 } || closest > tolerance)
        {
            NoWayRound($"{Where} The navmesh route does not fit the player first at ({first.x:0.00}, {first.z:0.00}), where the body {TimbnWalkProbe.WhyBlocked(first)}. "
                + $"The closest spot past it the player can reach and fit is {closest:0.00} away at ({reached.x:0.00}, {reached.z:0.00}), "
                + $"{TimbnPlayerGrid.WhyCutOff(stretch, reached)}, planned on {plannedOn}", route, from, to);
            yield break;
        }

        var detour = new StrongBox<List<Vector3>?>();
        var repairs = new StrongBox<int>();
        var stuck = new StrongBox<Vector3?>();
        yield return Check(generation, found.Value!.vectorPath, entry, exit, endsWalk, tolerance, detour, repairs, stuck);
        if (IsStale(generation))
            yield break;

        if (detour.Value is not { } corners)
        {
            var at = stuck.Value ?? first;
            NoWayRound($"{Where} No way round fits the player, the best one is blocked at ({at.x:0.00}, {at.z:0.00}), where the body {TimbnWalkProbe.WhyBlocked(at)}, planned on {plannedOn}", route, from, to);
            yield break;
        }

        _logger.LogDebug($"{Where} The navmesh route does not fit the player from {from:0.0} to {to:0.0} along it, first at ({first.x:0.00}, {first.z:0.00}) where the body {TimbnWalkProbe.WhyBlocked(first)}. "
            + $"It goes round over {corners.Count} corners, planned on {plannedOn}, {repairs.Value} tight spots closed.");
        result.Value = route.Splice(entryAt, endsWalk ? route.Length : exitAt, corners);
    }

    /// <summary>
    /// Straightens a grid route and checks it against the player's body. A stretch that does not fit closes its spot on
    /// the grid and the route is planned again, up to a few times. Sets <paramref name="result"/> to the detour from
    /// <paramref name="entry"/> to <paramref name="exit"/>, or to the nearest spot to it the player fits when the detour
    /// ends the walk, or sets <paramref name="stuck"/> to where the best one is still blocked.
    /// </summary>
    private IEnumerator Check(int generation, List<Vector3> corners, Vector3 entry, Vector3 exit, bool endsWalk, float tolerance, StrongBox<List<Vector3>?> result, StrongBox<int> repairs, StrongBox<Vector3?> stuck)
    {
        var joinsExit = !endsWalk || TimbnWalkProbe.Stands(exit, TimbnPlayerGrid.ObstacleMask);
        while (true)
        {
            List<Vector3> detour = [entry];
            yield return Straighten(generation, corners, detour);
            if (IsStale(generation))
                yield break;

            if (joinsExit)
                detour.Add(exit);

            var blocked = new StrongBox<Vector3?>();
            yield return FindBlockedLeg(generation, detour, blocked);
            if (IsStale(generation))
                yield break;

            if (blocked.Value is not { } at)
            {
                result.Value = detour;
                yield break;
            }

            if (repairs.Value >= _maxRepairs)
            {
                stuck.Value = at;
                yield break;
            }

            repairs.Value++;
            TimbnPlayerGrid.Close(at);
            var repaired = new StrongBox<Path?>();
            yield return GridRoute(generation, entry, exit, repaired);
            if (IsStale(generation))
                yield break;

            if (repaired.Value is not { error: false, vectorPath.Count: > 0 } path || TimbnWorld.FlatDistance(path.vectorPath[^1], exit) > tolerance)
            {
                stuck.Value = at;
                yield break;
            }

            corners = path.vectorPath;
        }
    }

    /// <summary>The area seen from above within <paramref name="reach"/> of every point of <paramref name="points"/>.</summary>
    private static Rect Box(List<Vector3> points, float reach)
    {
        float minX = points[0].x, minZ = points[0].z, maxX = points[0].x, maxZ = points[0].z;
        foreach (var point in points)
        {
            minX = Mathf.Min(minX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxX = Mathf.Max(maxX, point.x);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        return Rect.MinMaxRect(minX - reach, minZ - reach, maxX + reach, maxZ + reach);
    }

    /// <summary>
    /// Asks the navmesh, through the game's own seeker and its smoothing, for a route from the player to the target,
    /// keeping off the triangles this walk found the player does not fit through.
    /// </summary>
    private IEnumerator NavmeshRoute(int generation, GlobalNavigationManager navigation, GraphMask mask, StrongBox<Path?> result)
    {
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player == null)
            yield break;

        var start = player.transform.position;
        if (_avoided.Count == 0 || _pathUnits?.Invoke(navigation) is not { } units)
        {
            navigation.CalculatePath(mask, start, _target, path => result.Value = path);
        }
        else
        {
            var unit = units.GetOrCreateObject<PathCalculationUnit>();
            unit.transform.position = start;
            var path = ABPath.Construct(start, _target);
            path.traversalConstraint.traversalProvider = new Avoiding([.. _avoided]);
            path.traversalCosts.traversalProvider = path.traversalConstraint.traversalProvider;
            unit.seeker.StartPath(path, done =>
            {
                units.ReleaseObject(unit);
                result.Value = done;
            }, mask);
        }

        yield return Wait(generation, result);
    }

    private bool NearStall(Vector3 point)
    {
        foreach (var stall in _stalls)
        {
            if (TimbnWorld.FlatDistance(point, stall) < _stallRadius)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Plans the walk again from where the player is stuck, with the stretch just ahead of them kept off, the way it
    /// would after a nudge. Returns false once the walk has done so as often as it may.
    /// </summary>
    private bool Recover(TimbnRoute route)
    {
        if (_recoveries >= _maxRecoveries || TimbnCorePlugin.Instance is not { } core)
            return false;

        _recoveries++;
        for (var ahead = _aimStep; ahead <= _lookahead + 0.001f; ahead += _aimStep)
            _stalls.Add(route.PointAt(_progress + ahead));

        _replanning = true;
        _generation++;
        _requestedAt = Time.realtimeSinceStartup;
        _requestedFrame = Time.frameCount;
        core.StartCoroutine(Plan(_generation));
        return true;
    }

    /// <summary>
    /// Asks the player grid for a route from <paramref name="from"/> to <paramref name="to"/>, straight from A* with no
    /// seeker, since a seeker's smoothing suits a navmesh and on a grid rounds corners through closed cells.
    /// </summary>
    private IEnumerator GridRoute(int generation, Vector3 from, Vector3 to, StrongBox<Path?> result)
    {
        var path = ABPath.Construct(from, to, done => result.Value = done);
        path.traversalConstraint.graphMask = TimbnPlayerGrid.Mask;
        AstarPath.StartPath(path);
        yield return Wait(generation, result);
    }

    private IEnumerator Wait(int generation, StrongBox<Path?> result)
    {
        while (result.Value == null && !IsStale(generation))
        {
            yield return null;
            _budget.Restart();
        }
    }

    /// <summary>Adds to <paramref name="kept"/> only the corners the player's body cannot cut past in a straight line, read off the grid's own open cells.</summary>
    private IEnumerator Straighten(int generation, List<Vector3> corners, List<Vector3> kept)
    {
        var from = corners[0];
        kept.Add(from);
        for (var i = 1; i < corners.Count - 1; i++)
        {
            if (!TimbnPlayerGrid.IsOpenAlong(from, corners[i + 1]))
            {
                from = corners[i];
                kept.Add(from);
            }

            if (!_budget.IsSpent)
                continue;

            yield return null;
            _budget.Restart();
            if (IsStale(generation))
                yield break;
        }

        if (corners.Count > 1)
            kept.Add(corners[^1]);
    }

    /// <summary>Where the player's body first stops fitting along the route, ignoring a touch right where it starts.</summary>
    private IEnumerator FindBlockedLeg(int generation, List<Vector3> corners, StrongBox<Vector3?> blocked)
    {
        for (var i = 1; i < corners.Count; i++)
        {
            if (TimbnWalkProbe.TryFindBlock(corners[i - 1], corners[i], out var at) && (i > 1 || TimbnWorld.FlatDistance(at, corners[0]) > _startSlack))
            {
                blocked.Value = at;
                yield break;
            }

            if (!_budget.IsSpent)
                continue;

            yield return null;
            _budget.Restart();
            if (IsStale(generation))
                yield break;
        }
    }

    /// <summary>Starts walking the first leg, or carries straight on into the next one.</summary>
    private void Walk(TimbnRoute route, bool final)
    {
        var first = _route == null;
        _length += _progress;
        _route = route;
        _progress = 0f;
        _best = float.MinValue;
        _final = final;
        _replanning = false;
        _cutShort = false;

        // A leg too short to make progress on must not restart the leg wait, or a run of them never times out.
        if (first || route.Length >= _legEndRadius)
            _lastProgress = Time.fixedTime;

        _legs++;
        if (first)
        {
            _started = _lastSteer = Time.fixedTime;
            _latest = null;
            _current?.End();
            _current = this;
        }

        _logger.LogDebug($"{Where} Leg {_legs}{(final ? ", the last" : "")}, {route.Length:0.0} units over {route.Points.Count} corners, "
            + $"ready {(Time.realtimeSinceStartup - _requestedAt) * 1000f:0} ms and {Time.frameCount - _requestedFrame} frames after {(first ? "the click" : "it was asked for")}.");
    }

    /// <summary>How far the player still has to go along the current leg.</summary>
    private float Remaining() => _route is { } route ? route.Length - _progress : 0f;

    /// <summary>
    /// The way to steer, toward a point a little ahead of the player along the route, so turns curve the way the game's
    /// own walker turns. Ends, arrives or waits for the next leg at the end of the route, and stops a walk that makes no
    /// progress.
    /// </summary>
    private Vector2 NextDirection(Vector3 position)
    {
        var route = _route!;
        if (_replanning)
            return Vector2.zero;

        _progress = Mathf.Max(_progress, route.Project(position, _progress, _searchAhead));
        var toEnd = TimbnWorld.FlatDistance(position, route.Points[^1]);
        var nearEnd = route.Length - _progress <= _lookahead;
        if (nearEnd && toEnd < (_final ? _arriveRadius : _legEndRadius))
        {
            if (_final && _cutShort)
            {
                End();
            }
            else if (_final)
            {
                Arrive();
            }
            else if (Time.fixedTime - _lastProgress > _legWait)
            {
                _logger.LogWarning($"{Where} The next leg was not ready in time, so the walk stops at ({position.x:0.00}, {position.z:0.00}).");
                End();
            }

            return Vector2.zero;
        }

        var aim = Aim(route, position);
        var gone = _progress - toEnd;
        if (gone > _best + _progressStep)
        {
            _best = gone;
            _lastProgress = Time.fixedTime;
        }
        else if (Time.fixedTime - _lastProgress > _stuckSeconds && _final && !_cutShort && nearEnd && toEnd < _endTolerance)
        {
            _logger.LogDebug($"{Where} Stopped {toEnd:0.00} short of the spot, as near as the player gets.");
            Arrive();
            return Vector2.zero;
        }
        else if (Time.fixedTime - _lastProgress > _stuckSeconds)
        {
            var offRoute = TimbnWorld.FlatDistance(position, route.PointAt(_progress));
            var stuck = $"{Where} Blocked at ({position.x:0.00}, {position.z:0.00}), {_progress:0.0} of {route.Length:0.0} along leg {_legs} and {offRoute:0.00} off it, "
                + $"aiming {_aimAhead:0.0} ahead. The player {(TimbnWalkProbe.Stands(position) ? "stands clear" : "does not stand clear")} and is {TimbnWalkProbe.WhatBlocks(aim)}";
            if (Recover(route))
            {
                _logger.LogInfo($"{stuck}, so the walk is planned again from there.");
                return Vector2.zero;
            }

            _logger.LogWarning($"{stuck}, so the walk stops there.");
            End();
            return Vector2.zero;
        }

        var toAim = aim - position;
        return new Vector2(toAim.x, toAim.z / _depthMovementScale).normalized;
    }

    /// <summary>
    /// The furthest point up to <see cref="_lookahead"/> ahead along the route that the player's body reaches in a
    /// straight line, so a turn round a tree or a corner never cuts into it.
    /// </summary>
    private Vector3 Aim(TimbnRoute route, Vector3 position)
    {
        for (var ahead = _lookahead; ahead > _shortestAim + 0.001f; ahead -= _aimStep)
        {
            var aim = route.PointAt(_progress + ahead);
            if (!TimbnWalkProbe.IsClear(position, aim, _aimStep))
                continue;

            _aimAhead = ahead;
            return aim;
        }

        _aimAhead = _shortestAim;
        return route.PointAt(_progress + _shortestAim);
    }

    private void Unfreeze(bool unfrozen)
    {
        if (unfrozen)
        {
            _unfrozen ??= TimbnPlayerControl.LetMove();
            return;
        }

        _unfrozen?.Dispose();
        _unfrozen = null;
    }

    private void Arrive()
    {
        var seconds = Time.fixedTime - _started;
        var walked = _length + _progress;
        var pace = seconds > 0f ? $", {walked / seconds:0.00} units a second" : "";
        _logger.LogInfo($"{Where} Arrived, {walked:0.0} units in {seconds:0.0} seconds over {_legs} legs{pace}.");
        Arrived = true;
        End();
        if (_arrived != null)
            TimbnSafe.Run(_arrived, _logger, $"{nameof(TimbnPluginPlayer)}.{nameof(TimbnPluginPlayer.WalkTo)}|{TimbnSafe.Describe(_arrived)}");
    }

    /// <summary>
    /// Ends a walk that cannot be planned. Before the player sets off it stops at once. Once they are walking they
    /// finish the leg they are on, getting as far as they can, and then stop without arriving.
    /// </summary>
    private void Fail(string why)
    {
        if (_route == null || IsDone || _current != this || _replanning)
        {
            Stop(why);
            return;
        }

        _logger.LogInfo($"{why.Replace(", so they stay put.", ".").Replace(", so the player stays put.", ".")} The player walks to the end of the leg they are on and stops there.");
        _final = true;
        _cutShort = true;
    }

    private void Stop(string why)
    {
        _logger.LogInfo(why);
        End();
    }

    private void End()
    {
        if (IsDone)
            return;

        IsDone = true;
        Forget();
        Ended?.Invoke();
    }

    private void Forget()
    {
        Unfreeze(false);
        if (_current == this)
            _current = null;

        if (_latest == this)
            _latest = null;
    }

    /// <summary>Keeps a navmesh route off the triangles a walk found the player does not fit through.</summary>
    private sealed class Avoiding : ITraversalProvider
    {
        private readonly HashSet<uint> _nodes;

        public Avoiding(HashSet<uint> nodes)
        {
            _nodes = nodes;
        }

        public bool CanTraverse(ref TraversalConstraint traversalConstraint, GraphNode node) =>
            DefaultITraversalProvider.CanTraverse(ref traversalConstraint, node) && !_nodes.Contains(node.NodeIndex);

        public bool CanTraverse(ref TraversalConstraint traversalConstraint, GraphNode from, GraphNode to) => CanTraverse(ref traversalConstraint, to);
    }
}
