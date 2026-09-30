using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class ConveyorLoopCrash
{
    private const float _half = 0.3f;
    private const string _alertKey = "timbn_conveyor_loop_stuck";
    private const float _expireAfter = 10f;
    private const float _tickInterval = 0.5f;

    private static readonly List<ConveyorComponent> _path = [];
    private static readonly HashSet<ConveyorComponent> _walked = [];
    private static readonly Dictionary<SGuid, GameObject> _marked = [];
    private static readonly HashSet<SGuid> _reported = [];
    private static readonly Dictionary<SGuid, float> _lastCaught = [];
    private static readonly Color _red = new(1f, 0.15f, 0.1f, 1f);
    private static readonly Color _yellow = new(1f, 0.85f, 0.2f, 1f);
    private static Material? _material;

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        plugin.Settings.Toggle(PluginConfig.ConveyorLoopCrash, Restore, Restore);
        plugin.Settings.While(PluginConfig.ConveyorLoopCrash, () => plugin.Events.Every(_tickInterval, Tick));
        plugin.Events.SaveClosed(Restore);
    }

    public static bool Enter(ConveyorComponent component, ConveyorComponent cycleStart, out bool entered)
    {
        entered = false;
        if (!PluginConfig.ConveyorLoopCrash.Value)
            return true;

        if (!_walked.Add(component))
        {
            Highlight(component, cycleStart);
            return false;
        }

        _path.Add(component);
        entered = true;
        return true;
    }

    public static void Exit(bool entered)
    {
        if (!entered)
            return;

        _path.RemoveAt(_path.Count - 1);
        if (_path.Count == 0)
            _walked.Clear();
    }

    private static void Tick()
    {
        if (!TimbnGame.IsInGame)
            return;

        foreach (var id in _lastCaught.Keys.ToList())
        {
            if (Time.time - _lastCaught[id] < _expireAfter && MainGame.WorldData.GetWgoData(id) != null)
                continue;

            _lastCaught.Remove(id);
            _reported.Remove(id);
            if (_marked.Remove(id, out var marker) && marker != null)
                UnityEngine.Object.Destroy(marker);
        }
    }

    private static void Restore()
    {
        ClearMarkers();
        _reported.Clear();
        _lastCaught.Clear();
        _path.Clear();
        _walked.Clear();
    }

    private static void ClearMarkers()
    {
        foreach (var marker in _marked.Values)
        {
            if (marker != null)
                UnityEngine.Object.Destroy(marker);
        }

        _marked.Clear();
    }

    private static void Highlight(ConveyorComponent repeated, ConveyorComponent cycleStart)
    {
        var start = _path.IndexOf(repeated);
        if (start < 0 || repeated.wasPerformedItemTransfer || repeated.ParentsData.Values.Contains(cycleStart.WgoData))
            return;

        var loop = _path.Skip(start).ToList();
        var ring = Enumerable.Reverse(loop).Select(component => component.WgoData?.Position ?? Vector3.zero).ToList();
        var pick = loop.FirstOrDefault(component => component is ConveyorSplitterComponent) ?? loop[0];
        var data = pick.WgoData;
        if (data == null)
            return;

        _lastCaught[data.UniqueId] = Time.time;
        if (_reported.Add(data.UniqueId))
        {
            Plugin.Logger.LogWarning($"Conveyor loop of {loop.Count} pieces would never end. Removing the {data.id} at "
                + $"({data.Position.x:0.0}, {data.Position.z:0.0}) breaks it.");
            LazySingleton<UINotificator>.Instance?.ShowSimpleTextNotificationOneTime(_alertKey);
        }

        if (!PluginConfig.HighlightConveyorLoops.Value)
            ClearMarkers();
        else if (!_marked.ContainsKey(data.UniqueId))
            _marked[data.UniqueId] = Mark(data.Position, ring);
    }

    private static GameObject Mark(Vector3 position, List<Vector3> ring)
    {
        var marker = new GameObject("Timbn conveyor loop marker");
        var y = position.y + 0.05f;
        AddLine(
            marker,
            true,
            _red,
            0.07f,
            new Vector3(position.x - _half, y, position.z - _half),
            new Vector3(position.x - _half, y, position.z + _half),
            new Vector3(position.x + _half, y, position.z + _half),
            new Vector3(position.x + _half, y, position.z - _half));

        for (var i = 0; i < ring.Count; i++)
        {
            var from = ring[i] + Vector3.up * 0.05f;
            var to = ring[(i + 1) % ring.Count] + Vector3.up * 0.05f;
            AddLine(marker, false, _yellow, 0.05f, from, to);
            var forward = (to - from).normalized;
            var side = Vector3.Cross(forward, Vector3.up) * 0.1f;
            var tip = (from + to) / 2f + forward * 0.12f;
            AddLine(marker, false, _yellow, 0.05f, tip - forward * 0.15f + side, tip, tip - forward * 0.15f - side);
        }

        return marker;
    }

    private static void AddLine(GameObject parent, bool loop, Color color, float width, params Vector3[] points)
    {
        var line = new GameObject("line").AddComponent<LineRenderer>();
        line.transform.SetParent(parent.transform, false);
        line.sharedMaterial = _material ??= CreateMaterial();
        line.useWorldSpace = true;
        line.loop = loop;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    private static Material CreateMaterial()
    {
        var material = new Material(Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Sprites/Default"));
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Cull", 0);
        material.renderQueue = 4000;
        return material;
    }
}
