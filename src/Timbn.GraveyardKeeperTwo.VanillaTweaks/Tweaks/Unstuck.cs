using Pathfinding;
using System;
using System.Collections.Generic;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class Unstuck
{
    private const float _step = 0.5f;
    private const float _clearance = 0.3f;
    private const int _blockingLayers = 257;
    private const float _fallbackDistance = 1f;
    private const int _worldZoneLayers = 131072;
    private static readonly RaycastHit[] _zoneHits = new RaycastHit[15];

    public static bool Run(float range)
    {
        if (!TimbnGame.IsInGame || !TryGetRecastGraph(MainGame.PlayerData.currentGameSceneId, out var graph))
        {
            Plugin.Logger.LogWarning("No recast graph in this scene.");
            return false;
        }

        var areaSizes = new Dictionary<uint, int>();
        graph.GetNodes(node =>
        {
            if (node.Walkable)
                areaSizes[node.Area] = areaSizes.TryGetValue(node.Area, out var count) ? count + 1 : 1;
        });

        var from = MainGame.PlayerData.position.Value;
        var here = graph.GetNearest(from).node;
        var hereSize = here != null && areaSizes.TryGetValue(here.Area, out var size) ? size : 0;
        var overlapping = IsBlocked(from);
        var zones = WorldZonesAt(from);

        var target = FindOpenGround(graph, from, 0f, range, zones, node =>
            overlapping || (areaSizes.TryGetValue(node.Area, out var count) ? count : 0) > hereSize);
        target ??= FindOpenGround(graph, from, _fallbackDistance, range, zones, _ => true);
        if (target == null)
        {
            Plugin.Logger.LogWarning($"No open ground within {range:0} m.");
            return false;
        }

        MainGame.PlayerController.SetPosition(target.Value);
        Plugin.Logger.LogMessage($"Unstuck, moved {Vector3.Distance(from, target.Value):0.0} m.");
        return true;
    }

    private static Vector3? FindOpenGround(NavGraph graph, Vector3 from, float minDistance, float range, HashSet<string> zones, Func<GraphNode, bool> accept)
    {
        for (var radius = Mathf.Max(_step, minDistance); radius <= range; radius += _step)
        {
            var directions = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * radius / _step));
            for (var i = 0; i < directions; i++)
            {
                var angle = 2f * Mathf.PI * i / directions;
                var candidate = from + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                var nearest = graph.GetNearest(candidate);
                if (nearest.node == null || !nearest.node.Walkable || Vector3.Distance(candidate, nearest.position) > _clearance)
                    continue;

                if (accept(nearest.node) && !IsBlocked(nearest.position) && SharesZone(zones, nearest.position))
                    return nearest.position;
            }
        }

        return null;
    }

    private static bool SharesZone(HashSet<string> zones, Vector3 position) =>
        zones.Count == 0 || zones.Overlaps(WorldZonesAt(position));

    private static HashSet<string> WorldZonesAt(Vector3 position)
    {
        var ray = new Ray(position + Vector3.up * 50f, Vector3.down);
        var count = Physics.RaycastNonAlloc(ray, _zoneHits, 100f, _worldZoneLayers, QueryTriggerInteraction.Collide);
        HashSet<string> zones = [];
        for (var i = 0; i < count; i++)
        {
            var collider = _zoneHits[i].collider;
            var zone = collider.GetComponent<WorldZone>();
            if (zone == null && collider.transform.parent != null)
                zone = collider.transform.parent.GetComponent<WorldZone>();

            if (zone != null)
                zones.Add(zone.Id);
        }

        return zones;
    }

    private static bool IsBlocked(Vector3 position)
    {
        var player = MainGame.PlayerController.transform;
        foreach (var collider in Physics.OverlapSphere(position, _clearance, _blockingLayers))
        {
            if (!collider.isTrigger && !collider.transform.IsChildOf(player))
                return true;
        }

        return false;
    }

    private static bool TryGetRecastGraph(string worldId, out NavGraph graph)
    {
        graph = null!;
        var indices = GraphHelper.Instance?.SceneGraphsData?.GetRecastGraphIndexByWorldId(worldId);
        var graphs = AstarPath.active?.data?.graphs;
        if (indices == null || indices.Count == 0 || graphs == null || indices[0] < 0 || indices[0] >= graphs.Length)
            return false;

        graph = graphs[indices[0]];
        return graph != null;
    }
}
