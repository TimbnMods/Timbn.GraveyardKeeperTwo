using Pathfinding;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Finds things in the loaded world, such as the walkable ground of a scene or the objects near a spot. A mod that
/// uses the navigation graph needs its own reference to AstarPathfindingProject.dll from the game's Managed
/// folder.
/// </summary>
public static class TimbnWorld
{
    private static readonly DistanceMetric[] _metrics =
        [DistanceMetric.ClosestAsSeenFromAboveSoft(), DistanceMetric.ClosestAsSeenFromAbove()];

    /// <summary>
    /// Gets the navigation graph of a scene, which holds its walkable ground. People and zombies path over it, and
    /// the player can stand anywhere on it.
    /// </summary>
    /// <param name="sceneId">The scene's id. Leave it out for the scene the player is in.</param>
    /// <param name="graph">The scene's graph, or null when there is none.</param>
    /// <returns>True when the scene has a graph.</returns>
    public static bool TryGetNavGraph(string? sceneId, out NavGraph graph)
    {
        graph = null!;
        sceneId ??= TimbnPlayer.SceneId;
        if (string.IsNullOrEmpty(sceneId))
            return false;

        var indices = GraphHelper.Instance?.SceneGraphsData?.GetRecastGraphIndexByWorldId(sceneId);
        var graphs = AstarPath.active?.data?.graphs;
        if (indices == null || indices.Count == 0 || graphs == null || indices[0] < 0 || indices[0] >= graphs.Length)
            return false;

        graph = graphs[indices[0]];
        return graph != null;
    }

    /// <summary>
    /// Finds the nearest walkable ground to a spot in the player's scene, measured flat as seen from above, so a
    /// spot on a roof or under the floor still finds the ground below or above it.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnWorld.TryFindWalkable(TimbnPlayer.Position, 5f, out var ground))
    ///     TimbnPlayer.MoveTo(ground);
    /// </code>
    /// </example>
    /// <param name="point">The spot to search from.</param>
    /// <param name="range">How far to look, in world units.</param>
    /// <param name="ground">The nearest walkable point, or <paramref name="point"/> when none was found.</param>
    /// <returns>True when walkable ground was found within <paramref name="range"/>.</returns>
    public static bool TryFindWalkable(Vector3 point, float range, out Vector3 ground)
    {
        ground = point;
        if (!TimbnGame.IsInGame || !TryGetNavGraph(null, out var graph))
            return false;

        foreach (var metric in _metrics)
        {
            var constraint = NearestNodeConstraint.Walkable;
            constraint.distanceMetric = metric;
            constraint.maxDistanceSqr = range * range;
            var nearest = graph.GetNearest(point, constraint);
            if (nearest.node != null && FlatDistance(point, nearest.position) <= range)
            {
                ground = nearest.position;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Lists the world objects in the player's scene within a flat distance of a spot, nearest first. World objects
    /// are everything placed in the world, such as chests, stations, graves, NPCs, and zombies.
    /// </summary>
    /// <example>
    /// <code>
    /// var chests = TimbnWorld.Near(TimbnPlayer.Position, 5f, wgo => wgo.Inventory != null);
    /// </code>
    /// </example>
    /// <param name="position">The spot to measure from.</param>
    /// <param name="radius">How far to look, in world units, measured flat as seen from above.</param>
    /// <param name="match">Which objects to keep. Leave it out to keep all of them.</param>
    /// <returns>The matching objects, nearest first. Empty at the main menu.</returns>
    public static List<WgoData> Near(Vector3 position, float radius, Func<WgoData, bool>? match = null)
    {
        if (TimbnPlayer.Scene is not { } scene)
            return [];

        return scene.wgoDataList
            .Where(wgo => wgo != null && FlatDistance(position, wgo.Position) <= radius && (match?.Invoke(wgo) ?? true))
            .OrderBy(wgo => FlatDistance(position, wgo.Position))
            .ToList();
    }

    /// <summary>
    /// The view of a world object, the GameObject that draws it, or null when its chunk is not loaded around the
    /// player. The view is what the game's windows and effects work with, such as opening a station's window.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (var zombie in TimbnZombies.OnScene())
    ///     TimbnWorld.ViewOf(zombie)?.DrawWidgets();
    /// </code>
    /// </example>
    /// <param name="data">The world object.</param>
    /// <returns>Its view, or null when it has none right now.</returns>
    public static Wgo? ViewOf(WgoData data)
    {
        var view = GameScene.GetWgoViewGlobal(data.UniqueId);
        return view != null ? view : null;
    }

    /// <summary>The distance between two spots as seen from above, ignoring height.</summary>
    /// <param name="first">One spot.</param>
    /// <param name="second">The other spot.</param>
    /// <returns>The flat distance in world units.</returns>
    public static float FlatDistance(Vector3 first, Vector3 second) =>
        new Vector2(first.x - second.x, first.z - second.z).magnitude;
}
