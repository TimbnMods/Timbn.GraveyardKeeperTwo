using Pathfinding;
using Path = Pathfinding.Path;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Hands the game's walker a route Core planned, in place of the game's own search. For a paths walk that is a route
/// of points, since the game's search keeps to the one way links and so can send a person all the way round a loop to
/// a point beside them. For a straight walk through corners it is the corners themselves as a waypoint path, which the
/// game follows in one movement, turning at each without stopping. When a game update renames what this needs, walks
/// use the game's own search again.
/// </summary>
internal static class TimbnPlannedRoute
{
    private static readonly AccessTools.FieldRef<MovementComponent, ABPath>? _wholePath =
        TimbnReflection.TryField<MovementComponent, ABPath>("wholePath");

    private static readonly AccessTools.FieldRef<MovementComponent, bool>? _useVectorPath =
        TimbnReflection.TryField<MovementComponent, bool>("useVectorPathAsSource");

    private static readonly Action<MovementComponent, Path>? _onPathCalculated = TimbnReflection.TryDelegate<Action<MovementComponent, Path>>(
        () => AccessTools.Method(typeof(MovementComponent), "OnPathCalculated", [typeof(Path)]),
        "MovementComponent.OnPathCalculated");

    private static readonly Action<Path, PathCompleteState>? _setCompleteState = TimbnReflection.TryDelegate<Action<Path, PathCompleteState>>(
        () => AccessTools.PropertySetter(typeof(Path), nameof(Path.CompleteState)),
        "Path.CompleteState setter");

    private static readonly bool _isAvailable =
        TimbnWalkState.Status != null && _wholePath != null && _useVectorPath != null && _onPathCalculated != null && _setCompleteState != null
        && AccessTools.Method(typeof(MovementComponent), "FindPathGDGraph") != null
        && AccessTools.Method(typeof(MovementComponent), "FindPathRecastGraph") != null;

    private static MovementComponent? _movement;
    private static List<GDPointData>? _route;
    private static List<Vector3>? _corners;
    private static bool _used;

    internal static bool IsAvailable => _isAvailable;

    internal static T Walk<T>(MovementComponent movement, List<GDPointData> route, Func<T> start, out bool used)
    {
        _movement = movement;
        _route = route;
        _used = false;
        try
        {
            var result = start();
            used = _used;
            return result;
        }
        finally
        {
            Forget(movement);
        }
    }

    internal static T WalkCorners<T>(MovementComponent movement, IReadOnlyList<Vector3> corners, Func<T> start, out bool used)
    {
        _movement = movement;
        _corners = [.. corners];
        _used = false;
        try
        {
            var result = start();
            used = _used;
            return result;
        }
        finally
        {
            Forget(movement);
        }
    }

    internal static void Forget(MovementComponent movement)
    {
        if (_movement != movement)
            return;

        _movement = null;
        _route = null;
        _corners = null;
    }

    // The game's waypoint mover reads the corners as the calculated path's vector path, with the start added by the
    // game and the last corner as the destination it checks arrival at.
    internal static bool TryUseCorners(MovementComponent movement)
    {
        if (_movement != movement || _corners is not { Count: > 0 } corners || !_isAvailable)
            return false;

        Forget(movement);
        TimbnWalkState.Status!(movement) = MovementComponent.Status.PathBuilding;
        _useVectorPath!(movement) = true;
        _wholePath!(movement) = new ABPath { path = [], vectorPath = [] };
        var path = new ABPath { path = [], vectorPath = corners };
        _setCompleteState!(path, PathCompleteState.Complete);
        _used = true;
        _onPathCalculated!(movement, path);
        return true;
    }

    internal static bool TryUse(MovementComponent movement)
    {
        if (_movement != movement || _route is not { Count: > 0 } route || !_isAvailable)
            return false;

        Forget(movement);
        if (route.Any(point => point.Node == null))
            return false;

        List<GDPointData> nodes = [.. route];
        if (StartsThroughItsTransit(route))
            nodes.Insert(0, route[0]);

        TimbnWalkState.Status!(movement) = MovementComponent.Status.PathBuilding;
        _useVectorPath!(movement) = false;
        _wholePath!(movement) = new ABPath { path = [], vectorPath = [] };
        var path = new ABPath
        {
            path = [.. nodes.Select(point => (GraphNode)point.Node)],
            vectorPath = [.. nodes.Select(point => (Vector3)point.Node.position)],
        };
        _setCompleteState!(path, PathCompleteState.Complete);
        _used = true;
        _onPathCalculated!(movement, path);
        return true;
    }

    private static bool StartsThroughItsTransit(List<GDPointData> route) =>
        route.Count > 1
        && (route[0].IsTransitPoint || !string.IsNullOrEmpty(route[0].GameSceneDataIdToTransit))
        && route[0].TransitToGdPointId == route[1].Id;
}
