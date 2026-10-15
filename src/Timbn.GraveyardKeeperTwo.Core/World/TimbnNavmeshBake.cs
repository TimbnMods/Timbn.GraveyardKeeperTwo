using Pathfinding;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts world objects into the scene's navmesh the way the game's offline bake puts in the objects built into the
/// scene, so people and zombies walk around fences, trees and rocks and over bridge decks. The game bakes its navmesh
/// before world objects spawn, so once a view shows, Core rebuilds the navmesh tiles under any object the navmesh
/// leaves out, a solid part standing on walkable navmesh or a walkable surface over a hole in it. A rebuild reads only
/// readable meshes and correctly scaled shapes on the layers the bake uses, so for the rebuild Core adds a stand in for
/// each unreadable mesh collider (sampled with raycasts), each mirrored box, and each solid part of the object on
/// another layer, switches off the colliders they replace for that moment, then removes the stand ins again. A
/// tabletop standing on the floor is not made walkable, and an object is rebuilt again when its look changes and once
/// more when it leaves the world.
/// </summary>
internal static class TimbnNavmeshBake
{
    private const float _sampleStep = 0.05f;
    private const float _margin = 0.2f;
    private const float _coveredHeight = 0.2f;
    private const float _flatMatch = 0.05f;
    private const float _flatHeight = 0.1f;
    private const float _walkUnderHeight = 1f;

    private static readonly int _floorLayers = LayerMask.GetMask("Ground", "Step");
    private static readonly int _solidLayers = LayerMask.GetMask("Default", "Interactable", "Obstacle");
    private static readonly int _bakedLayers = LayerMask.GetMask("Ground", "Step", "Obstacle");
    private static readonly int _obstacleLayer = LayerMask.NameToLayer("Obstacle");
    private static readonly Action<AstarPath, GraphUpdateObject>? _updateGraphs =
        TimbnReflection.TryDelegate<Action<AstarPath, GraphUpdateObject>>(() => AccessTools.Method(typeof(AstarPath), "UpdateGraphs", [typeof(GraphUpdateObject)]), "AstarPath.UpdateGraphs");
    private static readonly Action<AstarPath>? _flushGraphUpdates =
        TimbnReflection.TryDelegate<Action<AstarPath>>(() => AccessTools.Method(typeof(AstarPath), "FlushGraphUpdates"), "AstarPath.FlushGraphUpdates");

    private static readonly Dictionary<SGuid, Bounds> _baked = [];
    private static readonly Dictionary<SGuid, Wgo> _waiting = [];
    private static readonly List<(Bounds Area, int Frame)> _cleared = [];
    private static GameObject? _root;

    internal static void Refresh(Wgo? view)
    {
        var data = view ? view!.Data : null;
        if (data == null)
            return;

        if (IsWorldObject(data))
        {
            _ = Root;
            _waiting[data.UniqueId] = view!;
            return;
        }

        _waiting.Remove(data.UniqueId);
        if (_baked.Remove(data.UniqueId, out var old))
            _cleared.Add((old, Time.frameCount));
    }

    internal static void Remove(WgoData data)
    {
        _waiting.Remove(data.UniqueId);
        if (_baked.Remove(data.UniqueId, out var area))
            _cleared.Add((area, Time.frameCount));
    }

    internal static void OnLeftGame()
    {
        _baked.Clear();
        _waiting.Clear();
        _cleared.Clear();
    }

    private static Transform Root
    {
        get
        {
            if (_root == null)
            {
                _root = new GameObject("TimbnNavmeshBake") { hideFlags = HideFlags.HideAndDontSave };
                _root.AddComponent<Runner>();
                UnityEngine.Object.DontDestroyOnLoad(_root);
            }

            return _root.transform;
        }
    }

    private static void Tick()
    {
        if (_waiting.Count == 0 && _cleared.Count == 0)
            return;

        List<Bounds> updates = [];
        List<Wgo> sources = [];
        if (_waiting.Count > 0 && TimbnWorld.TryGetNavGraph(null, out var graph))
        {
            foreach (var pair in _waiting.ToList())
            {
                var view = pair.Value;
                if (!view || view.Data == null)
                {
                    _waiting.Remove(pair.Key);
                    continue;
                }

                if (!view.gameObject.activeInHierarchy)
                    continue;

                _waiting.Remove(pair.Key);
                if (!IsWorldObject(view.Data))
                    continue;

                var had = _baked.TryGetValue(pair.Key, out var old);
                if (BakeArea(view) is not { } area)
                {
                    if (had && _baked.Remove(pair.Key))
                        updates.Add(old);

                    continue;
                }

                if (had && old == area)
                    continue;

                if (!had && !NeedsBake(view, graph))
                    continue;

                var update = area;
                if (had)
                    update.Encapsulate(old);

                _baked[pair.Key] = area;
                updates.Add(update);
                sources.Add(view);
            }
        }

        for (var index = _cleared.Count - 1; index >= 0; index--)
        {
            if (Time.frameCount <= _cleared[index].Frame + 1)
                continue;

            updates.Add(_cleared[index].Area);
            _cleared.RemoveAt(index);
        }

        if (updates.Count > 0)
            Rebuild(updates, sources);
    }

    private static bool IsWorldObject(WgoData data)
    {
        var definition = data.Definition;
        if (!TimbnGame.IsInGame || definition == null || definition.isMovable || data is ZombieWgoData)
            return false;

        return MainGame.WorldData.GetWgoData(data.UniqueId) == data && !GraphCustomNavMeshCutUnit.HasBakedCustomNavMeshCuts(data);
    }

    private static Bounds? BakeArea(Wgo view)
    {
        Bounds? area = null;
        foreach (var collider in Floors(view).Concat(Solids(view)))
        {
            var bounds = collider.bounds;
            if (area is { } current)
            {
                current.Encapsulate(bounds);
                area = current;
            }
            else
            {
                area = bounds;
            }
        }

        return area;
    }

    private static bool NeedsBake(Wgo view, NavGraph graph)
    {
        foreach (var collider in Solids(view))
        {
            if (StandsOnNavmesh(graph, collider.bounds))
                return true;
        }

        foreach (var collider in Floors(view))
        {
            var bounds = collider.bounds;
            var top = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            if (Physics.Raycast(top + Vector3.up, Vector3.down, out var hit, bounds.size.y + 2f, 1 << collider.gameObject.layer) && hit.collider == collider)
                top = hit.point;

            if (!Covered(graph, top) && !OverNavmesh(graph, top))
                return true;
        }

        return false;
    }

    private static bool StandsOnNavmesh(NavGraph graph, Bounds bounds)
    {
        var point = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        var constraint = NearestNodeConstraint.Walkable;
        constraint.distanceMetric = DistanceMetric.ClosestAsSeenFromAboveSoft();
        constraint.maxDistanceSqr = 1f;
        var nearest = graph.GetNearest(point, constraint);
        if (nearest.node == null || TimbnWorld.FlatDistance(point, nearest.position) > _flatMatch)
            return false;

        return bounds.min.y - nearest.position.y <= _walkUnderHeight && bounds.max.y >= nearest.position.y;
    }

    private static bool Covered(NavGraph graph, Vector3 point)
    {
        var constraint = NearestNodeConstraint.Walkable;
        constraint.distanceMetric = DistanceMetric.ClosestAsSeenFromAboveSoft();
        constraint.maxDistanceSqr = 1f;
        var nearest = graph.GetNearest(point, constraint);
        return nearest.node != null && TimbnWorld.FlatDistance(point, nearest.position) <= _flatMatch && Mathf.Abs(point.y - nearest.position.y) <= _coveredHeight;
    }

    private static bool OverNavmesh(NavGraph graph, Vector3 point)
    {
        var constraint = NearestNodeConstraint.Walkable;
        constraint.distanceMetric = DistanceMetric.ClosestAsSeenFromAbove();
        constraint.maxDistanceSqr = _flatMatch * _flatMatch;
        var nearest = graph.GetNearest(point, constraint);
        return nearest.node != null && nearest.position.y < point.y;
    }

    private static void Rebuild(List<Bounds> updates, List<Wgo> sources)
    {
        if (TimbnWorld.Pathfinding is not { } pathfinding || _updateGraphs == null || !TimbnWorld.TryGetNavGraph(null, out var graph))
            return;

        var tile = graph is RecastGraph recast ? recast.cellSize * Mathf.Max(recast.tileSizeX, recast.tileSizeZ) + _margin : 0f;
        Physics.SyncTransforms();
        List<GameObject> standIns = [];
        List<Collider> replaced = [];
        HashSet<Collider> handled = [];
        try
        {
            foreach (var update in updates)
            {
                var sampled = update;
                sampled.Expand(new Vector3(tile * 2f + _margin * 2f, _margin * 2f, tile * 2f + _margin * 2f));
                foreach (var collider in Physics.OverlapBox(sampled.center, sampled.extents, Quaternion.identity, _bakedLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!handled.Add(collider))
                        continue;

                    if (collider is MeshCollider { sharedMesh: { isReadable: false } } mesh)
                    {
                        if (SampledStandIn(mesh, sampled) is { } sampledStandIn)
                            standIns.Add(sampledStandIn);

                        replaced.Add(collider);
                    }
                    else if (collider is not MeshCollider && IsMirrored(collider.transform))
                    {
                        standIns.Add(BoxStandIn(collider, collider.gameObject.layer));
                        replaced.Add(collider);
                    }
                }
            }

            foreach (var view in sources)
            {
                foreach (var collider in Solids(view))
                {
                    if (collider.gameObject.layer != _obstacleLayer && handled.Add(collider))
                        standIns.Add(BoxStandIn(collider, _obstacleLayer));
                }
            }

            foreach (var collider in replaced)
                collider.enabled = false;

            Physics.SyncTransforms();
            var mask = GraphMask.FromGraphIndex(graph.graphIndex);
            foreach (var update in updates)
            {
                var area = update;
                area.Expand(_margin);
                _updateGraphs(pathfinding, new GraphUpdateObject(area) { updatePhysics = true, graphMask = mask });
            }

            _flushGraphUpdates?.Invoke(pathfinding);
        }
        finally
        {
            foreach (var collider in replaced)
            {
                if (collider)
                    collider.enabled = true;
            }

            foreach (var standIn in standIns)
                UnityEngine.Object.Destroy(standIn);
        }
    }

    private static bool IsMirrored(Transform transform)
    {
        var scale = transform.lossyScale;
        return scale.x < 0f || scale.y < 0f || scale.z < 0f;
    }

    private static GameObject BoxStandIn(Collider source, int layer)
    {
        var bounds = source.bounds;
        var standIn = new GameObject($"{source.name} navmesh stand in") { layer = layer };
        standIn.transform.SetParent(Root, false);
        standIn.transform.position = bounds.center;
        standIn.AddComponent<BoxCollider>().size = bounds.size;
        return standIn;
    }

    private static GameObject? SampledStandIn(MeshCollider source, Bounds within)
    {
        var whole = source.bounds;
        var min = Vector3.Max(whole.min, new Vector3(within.min.x, whole.min.y, within.min.z));
        var max = Vector3.Min(whole.max, new Vector3(within.max.x, whole.max.y, within.max.z));
        if (min.x > max.x || min.z > max.z)
            return null;

        var bounds = new Bounds((min + max) / 2f, max - min);
        var columns = Mathf.CeilToInt(bounds.size.x / _sampleStep) + 1;
        var rows = Mathf.CeilToInt(bounds.size.z / _sampleStep) + 1;
        var heights = new float?[columns, rows];
        var start = bounds.max.y + 0.5f;
        var any = false;
        for (var x = 0; x < columns; x++)
        {
            for (var z = 0; z < rows; z++)
            {
                var origin = new Vector3(bounds.min.x + x * _sampleStep, start, bounds.min.z + z * _sampleStep);
                if (source.Raycast(new Ray(origin, Vector3.down), out var hit, bounds.size.y + 1f))
                {
                    heights[x, z] = hit.point.y;
                    any = true;
                }
            }
        }

        if (!any)
            return null;

        List<Vector3> vertices = [];
        List<int> triangles = [];
        AddSheet(vertices, triangles, heights, bounds, null);
        if (source.gameObject.layer == _obstacleLayer)
            AddSheet(vertices, triangles, heights, bounds, bounds.min.y);

        var mesh = new Mesh { name = $"{source.sharedMesh.name} stand in", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        var standIn = new GameObject($"{source.name} navmesh stand in") { layer = source.gameObject.layer };
        standIn.transform.SetParent(Root, false);
        standIn.AddComponent<MeshCollider>().sharedMesh = mesh;
        return standIn;
    }

    private static void AddSheet(List<Vector3> vertices, List<int> triangles, float?[,] heights, Bounds bounds, float? flatAt)
    {
        var columns = heights.GetLength(0);
        var rows = heights.GetLength(1);
        for (var x = 0; x < columns - 1; x++)
        {
            for (var z = 0; z < rows - 1; z++)
            {
                if (heights[x, z] is not { } a || heights[x + 1, z] is not { } b || heights[x, z + 1] is not { } c || heights[x + 1, z + 1] is not { } d)
                    continue;

                var first = vertices.Count;
                vertices.Add(Point(bounds, x, z, flatAt ?? a));
                vertices.Add(Point(bounds, x + 1, z, flatAt ?? b));
                vertices.Add(Point(bounds, x, z + 1, flatAt ?? c));
                vertices.Add(Point(bounds, x + 1, z + 1, flatAt ?? d));
                triangles.AddRange([first, first + 2, first + 1, first + 1, first + 2, first + 3]);
            }
        }
    }

    private static Vector3 Point(Bounds bounds, int x, int z, float y) =>
        new(bounds.min.x + x * _sampleStep, y, bounds.min.z + z * _sampleStep);

    private static IEnumerable<Collider> Floors(Wgo view) =>
        Shown(view, _floorLayers);

    private static IEnumerable<Collider> Solids(Wgo view) =>
        Shown(view, _solidLayers).Where(collider => collider.attachedRigidbody is not { isKinematic: false } && collider.bounds.size.y >= _flatHeight);

    private static IEnumerable<Collider> Shown(Wgo view, int layers)
    {
        foreach (var collider in view.GetComponentsInChildren<Collider>())
        {
            if (!collider.isTrigger && collider.enabled && (layers & (1 << collider.gameObject.layer)) != 0)
                yield return collider;
        }
    }

    private sealed class Runner : MonoBehaviour
    {
        private void LateUpdate() => TimbnSafe.Run(Tick, TimbnCorePlugin.Logger, "Navmesh bake");
    }
}
