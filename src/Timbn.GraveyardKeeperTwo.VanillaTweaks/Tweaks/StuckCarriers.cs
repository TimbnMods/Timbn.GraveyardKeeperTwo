namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class StuckCarriers
{
    private const float _interval = 0.5f;

    private HashSet<ZombieWgoData> _stuck = [];

    public static bool IsStuck(ZombieWgoData zombie) =>
        zombie.ZombieType == ZombieType.Caretaker
        && zombie.CaretakerState == ZombieWgoData.ZombieCaretakerState.CanNotPutItemToInventory;

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        var stuckCarriers = new StuckCarriers();
        plugin.Settings.Toggle(PluginConfig.StuckCarriers, stuckCarriers.Reset, stuckCarriers.Revert);
        plugin.Settings.While(PluginConfig.StuckCarriers, () => plugin.Events.Every(_interval, stuckCarriers.Tick));
    }

    private void Tick()
    {
        var stuck = TimbnZombies.OnScene().Where(IsStuck).ToHashSet();

        foreach (var zombie in stuck.Where(z => !_stuck.Contains(z)))
        {
            Redraw(zombie);
            WalkToStation(zombie);
            Plugin.Logger.LogInfo($"Carrier {zombie.Name} has nowhere to put {zombie.CaretakerPortableItem.Count} "
                + $"{zombie.CaretakerPortableItem.id} in {zombie.WorldZoneData?.id}.");
        }

        foreach (var zombie in _stuck.Where(z => !stuck.Contains(z)))
            Redraw(zombie);

        _stuck = stuck;
    }

    public void Reset() => _stuck.Clear();

    public void Revert()
    {
        if (TimbnGame.IsInGame)
        {
            foreach (var zombie in _stuck)
                Redraw(zombie);
        }

        _stuck.Clear();
    }

    private static void Redraw(ZombieWgoData zombie) => GameScene.GetWgoViewGlobal(zombie.UniqueId)?.DrawWidgets();

    private static void WalkToStation(ZombieWgoData zombie)
    {
        var zone = zombie.WorldZoneData;
        var station = zombie.AttachedWgoData;
        var movement = zombie.MovementComponent;
        if (zone == null || station == null || movement == null || movement.IsMoving)
            return;

        var dock = station.GetNearestDockPointData(zombie.Position, DockPointData.Availability.All);
        var target = station.GetNearestDockPointDataWorldPositionOrMyPosition(
            zombie.Position,
            DockPointData.Availability.OnlyOccupied,
            (point, _) => !point.BakedData.DisableTargetingForCaretaker);
        var graphs = NavigationGraphMaskUtils.ToGraphMask(zone.MovementGraphs, zone.navigationGraph);
        if (dock != null)
            movement.StartPath(target, dock.BakedData, graphs, zombie.WorldId);
        else
            movement.StartPath(target, graphs, zombie.WorldId);
    }
}
