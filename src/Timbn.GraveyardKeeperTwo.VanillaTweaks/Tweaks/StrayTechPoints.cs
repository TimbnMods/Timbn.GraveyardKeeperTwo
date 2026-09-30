namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class StrayTechPoints
{
    private const float _interval = 1f;
    private const float _collectDuration = 1f;

    private static readonly AccessTools.FieldRef<List<TechPointDrop>> _activeDrops =
        AccessTools.StaticFieldRefAccess<List<TechPointDrop>>(AccessTools.Field(typeof(TechPointDrop), "activeDrops"));

    private static readonly AccessTools.FieldRef<TechPointDrop, bool> _isTimedCollecting =
        AccessTools.FieldRefAccess<TechPointDrop, bool>("isTimedCollecting");

    private static readonly AccessTools.FieldRef<TechPointDrop, float> _curTime =
        AccessTools.FieldRefAccess<TechPointDrop, float>("curTime");

    private static readonly AccessTools.FieldRef<TechPointDrop, float> _collectDelay =
        AccessTools.FieldRefAccess<TechPointDrop, float>("collectDelay");

    private static readonly AccessTools.FieldRef<TechPointDrop, float> _magnetRadius =
        AccessTools.FieldRefAccess<TechPointDrop, float>("magnetRadius");

    private readonly HashSet<TechPointDrop> _offMeshLastCheck = [];

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        var strayTechPoints = new StrayTechPoints();
        plugin.Settings.Toggle(PluginConfig.CollectStrayTechPoints, strayTechPoints.Reset, strayTechPoints.Reset);
        plugin.Settings.While(PluginConfig.CollectStrayTechPoints, () => plugin.Events.Every(_interval, strayTechPoints.Tick));
    }

    private void Reset() => _offMeshLastCheck.Clear();

    private void Tick()
    {
        var player = MainGame.PlayerController;
        var graph = player != null ? player.SceneRecastGraph : null;
        if (graph == null)
        {
            _offMeshLastCheck.Clear();
            return;
        }

        var playerPosition = player!.transform.position;
        List<TechPointDrop> offMesh = [];
        foreach (var drop in _activeDrops())
        {
            if (drop == null || _isTimedCollecting(drop) || _curTime(drop) < _collectDelay(drop))
                continue;

            var position = drop.transform.position;
            var inMagnetRange = (playerPosition - position).XZ().sqrMagnitude <= _magnetRadius(drop);
            if (inMagnetRange || graph.IsPointOnNavmesh(position.XZ()))
                continue;

            offMesh.Add(drop);
        }

        foreach (var drop in offMesh)
        {
            if (_offMeshLastCheck.Contains(drop))
                drop.MoveToCollectorTimed(player.transform, _collectDuration);
        }

        _offMeshLastCheck.Clear();
        _offMeshLastCheck.UnionWith(offMesh);
    }
}
