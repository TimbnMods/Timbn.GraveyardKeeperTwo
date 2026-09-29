namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class StuckResourceWorkers
{
    private const string _craftStartEvent = "craft_start";

    private static readonly Station[] _stations =
    [
        new("sawmill", "sawmill_wood_crafter", "builder_sawmill"),
        new("mine", "mine_ore_coal_crafter", "builder_mine"),
        new("clay", "clay_zombie_crafter", "builder_clay_sand"),
        new("sand", "sand_zombie_crafter", "builder_clay_sand"),
    ];

    public static void Repair()
    {
        if (!PluginConfig.StuckResourceWorkers.Value)
            return;

        var pointIds = new HashSet<string>(MainGame.WorldData.gdPointsData.Points.Select(p => p.Id));
        foreach (var station in _stations)
        {
            var builder = MainGame.WorldData.GetWgoData(station.BuilderId);
            if (builder == null)
                continue;

            var workers = Workers(station).ToList();
            HashSet<string> held = [];
            foreach (var zombie in workers.Where(z => z.GameResStr.Has(station.PointKey)))
            {
                if (HasLeftSpot(zombie, station))
                    zombie.GameResStr.Remove(station.PointKey);
                else
                    held.Add(zombie.GameResStr.Get(station.PointKey));
            }

            List<string> points = [];
            while (pointIds.Contains($"{station.Prefix}_zombie_{points.Count + 1}"))
                points.Add($"{station.Prefix}_zombie_{points.Count + 1}");

            foreach (var point in points.Where(p => builder.GetGameResInt(p) == 1 && !held.Contains(p)))
            {
                builder.SetGameRes(point, 0);
                Plugin.Logger.LogInfo($"Freed work spot {point}, which no {station.Prefix} zombie was using.");
            }

            var free = points.Count(p => builder.GetGameResInt(p) == 0);
            foreach (var zombie in workers.Where(z => IsStranded(z, station)).Take(free))
            {
                Plugin.Logger.LogInfo($"Sent a stranded {station.StandId} zombie back to work.");
                zombie.AttachedWgoData.FireEvent(_craftStartEvent);
            }
        }
    }

    public static void OnDetaching(ZombieWgoData zombie)
    {
        if (!PluginConfig.StuckResourceWorkers.Value
            || Find(zombie.AttachedWgoData) is not { } station
            || !zombie.GameResStr.Has(station.PointKey))
        {
            return;
        }

        var point = zombie.GameResStr.Get(station.PointKey);
        if (IsHolding(zombie, station))
        {
            MainGame.WorldData.GetWgoData(station.BuilderId)?.SetGameRes(point, 0);
            Plugin.Logger.LogInfo($"Freed work spot {point} as its zombie left the {station.StandId}.");
        }

        zombie.GameResStr.Remove(station.PointKey);
    }

    private static bool IsHolding(ZombieWgoData zombie, Station station) =>
        zombie.AttachedWgoData?.CraftComponent?.CurrentCraftElement != null && !HasLeftSpot(zombie, station);

    private static bool HasLeftSpot(ZombieWgoData zombie, Station station)
    {
        var element = zombie.AttachedWgoData?.CraftComponent?.CurrentCraftElement;
        if (element == null)
            return false;

        var movement = zombie.MovementComponent;
        return element.ParamsData.customRes.GetInt(station.WaitFlag) == 1
            && !(movement.IsMoving && movement.OnPathCompleteEvent == station.WorkEvent);
    }

    private static bool IsStranded(ZombieWgoData zombie, Station station)
    {
        var element = zombie.AttachedWgoData.CraftComponent?.CurrentCraftElement;
        return element is { IsStarted: true }
            && element.ParamsData.customRes.GetInt(station.WaitFlag) == 1
            && !zombie.MovementComponent.IsMoving
            && !zombie.GameResStr.Has(station.PointKey);
    }

    private static Station? Find(WgoData? stand) =>
        stand == null ? null : _stations.FirstOrDefault(s => s.StandId == stand.id);

    private static IEnumerable<ZombieWgoData> Workers(Station station) =>
        MainGame.WorldData.GetWgoDataList(station.StandId).Select(stand => stand.Worker).OfType<ZombieWgoData>();

    private sealed class Station(string prefix, string standId, string builderId)
    {
        public string Prefix { get; } = prefix;

        public string StandId { get; } = standId;

        public string BuilderId { get; } = builderId;

        public string PointKey => $"{Prefix}_point";

        public string WaitFlag => $"wait_for_zombie_at_{Prefix}";

        public string WorkEvent => $"{Prefix}_craft_start";
    }
}
