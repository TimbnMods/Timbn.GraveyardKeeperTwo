using System.Collections.Generic;
using System.Linq;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class StuckCarriers
{
    private const float _interval = 0.5f;

    private HashSet<ZombieWgoData> _stuck = [];
    private float _nextCheck;

    public static bool IsStuck(ZombieWgoData zombie) =>
        zombie.ZombieType == ZombieType.Caretaker
        && zombie.CaretakerState == ZombieWgoData.ZombieCaretakerState.CanNotPutItemToInventory;

    public void Tick()
    {
        if (Time.time < _nextCheck)
            return;

        _nextCheck = Time.time + _interval;
        HashSet<ZombieWgoData> stuck = [];
        foreach (var id in MainGame.ZombieSystemData.zombieOnSceneWgoIds)
        {
            if (MainGame.WorldData.GetWgoData(id) is ZombieWgoData zombie && IsStuck(zombie))
                stuck.Add(zombie);
        }

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
