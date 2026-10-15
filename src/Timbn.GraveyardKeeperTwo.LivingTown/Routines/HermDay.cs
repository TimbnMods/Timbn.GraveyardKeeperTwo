namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// Herm's daily schedule. He manages his stall starting at dawn, takes occasional short walks (except in the rain),
/// visits the tavern in the evening, and heads home at night.
/// </summary>
internal sealed class HermDay
{
    private const string _stand = "RT_herm_spawn";
    private const string _ramp = "npc_sim_village_right_side_road_34";
    private const string _road03 = "npc_sim_village_right_side_road_03";
    private const string _pond = "npc_sim_village_right_side_03";
    private const string _leave = "npc_sim_village_right_side_road_36";
    private const string _bench = "npc_sim_village_right_side_01";
    private const string _botSide = "npc_sim_village_bot_side_01";
    private const string _guard = "npc_sim_village_guard_01";
    private const string _toiletsInside = "npc_sim_village_toilets_01_inside";
    private const string _toiletsOutside = "npc_sim_village_toilets_01_outside";
    private const float _goChance = 0.75f;
    private const float _rareChance = 0.25f;
    private const float _commonChance = 0.7f;
    private const float _chainChance = 0.4f;

    // His longest trips (bot01 and the bathroom) take up to 112 in-game minutes during schedule profiling.
    private const double _quietTailMinutes = 120;

    private static readonly string[] _benches = ["npc_sim_village_right_side_05", "npc_sim_village_right_side_04"];

    private static readonly Dictionary<string, Direction> _loopFacing = new()
    {
        ["npc_sim_village_right_side_road_39"] = Direction.Left,
        ["npc_sim_village_right_side_road_02"] = Direction.Left,
        ["npc_sim_village_right_side_road_03"] = Direction.Left,
        ["npc_sim_village_right_side_road_35"] = Direction.Right,
    };

    private static readonly string[] _common = ["spot", "pond"];
    private static readonly string[] _uncommon = ["bench01", "bot01"];

    private int _rareDoneDay = -1;
    private string _lastRoute = "";

    public TimbnNpcDay Build()
    {
        return new TimbnNpcDay()
            .Block("working", VendorHours.Working, block => block
                .Origin(_stand)
                .Intro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.Herm.GoToSpawn))
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.Herm.TpToSpawn))
                .Loop(steps => steps
                    .WaitGameMinutes(10, 30)
                    .ChainTrips((herm, previous) => TimbnWorld.IsRaining ? RollRaining(previous) : Roll(herm, previous))))
            .Block("evening", VendorHours.Evening, block => block
                .Origin(_stand)
                .Intro(steps => steps
                    .WalkNavmesh(_ramp)
                    .GoToFreePoint(TimbnGameIds.Groups.VillagersDayAll, "npc_sim_village_tavern_"))
                .LateIntro(steps => steps
                    .GoToFreePoint(TimbnGameIds.Groups.VillagersDayAll, "npc_sim_village_tavern_", teleport: true)))
            .Block("night", VendorHours.Night, block => block
                .Origin(_stand)
                .Intro(steps => steps
                    .Walk("npc_sim_village_tavern_road_02")
                    .Walk(_ramp)
                    .WalkNavmesh(_stand)
                    .FireEvent(TimbnGameIds.NpcEvents.Herm.TpToSpawn)
                    .FireEvent(TimbnGameIds.NpcEvents.Herm.GoHome))
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.Herm.TpToSpot)))
            .LogSteps(() => PluginConfig.WriteDebugLog.Value);
    }

    public void Reset()
    {
        _rareDoneDay = -1;
        _lastRoute = "";
    }

    private TimbnNpcTrip? Roll(TimbnNpc herm, TimbnNpcTrip? previous)
    {
        var left = herm.MinutesLeftInBlock;
        if (left < _quietTailMinutes)
        {
            Log(previous == null ? $"Staying at the stall, the next block starts in {left:0} minutes." : $"Heading back to the stall, the next block starts in {left:0} minutes.");
            return null;
        }

        var day = TimbnClock.Day;
        var chaining = previous != null;
        var rareDue = _rareDoneDay != day ? TodaysRare() : null;
        var rare = rareDue != null && rareDue != _lastRoute && TimbnRolls.ThisMinute($"{TimbnGameIds.Npcs.Herm}/rare").NextDouble() < _rareChance ? rareDue : null;
        var random = herm.RollChance(rare != null ? 1f : chaining ? _chainChance : _goChance, out var roll, chaining ? "another trip" : "a trip");
        if (random == null)
            return null;

        var common = random.NextDouble() < _commonChance;
        List<string> order = [];
        if (rare != null)
            order.Add(rare);

        order.AddRange((common ? _common : _uncommon).OrderBy(_ => random.Next()));
        order.AddRange((common ? _uncommon : _common).OrderBy(_ => random.Next()));
        if (chaining)
            order.Remove(_lastRoute);

        List<string> taken = [];
        foreach (var route in order)
        {
            var destination = Destination(route, herm, random);
            if (herm.BlockedReason(destination) is { } why)
            {
                taken.Add($"{route} ({destination}: {why})");
                continue;
            }

            herm.Claim(destination);
            _lastRoute = route;
            Log((route == rare ? $"Rolled the {route} route (once a day)." : $"Rolled {roll:0.00}, the {route} route.")
                + (chaining ? $" Going straight on from the last trip." : "")
                + (taken.Count > 0 ? $" Passed over {string.Join(", ", taken)}." : ""));
            if (route == rare)
                _rareDoneDay = day;

            return Trip(route, destination);
        }

        Log(chaining
            ? $"Heading back to the stall, every other route had its spot taken: {string.Join(", ", taken)}."
            : $"Skipped, every route had its spot taken: {string.Join(", ", taken)}. Staying at the stall.");

        return null;
    }

    private static TimbnNpcTrip? RollRaining(TimbnNpcTrip? previous)
    {
        Log(previous == null ? "Staying at the stall, it's raining." : "Heading back to the stall, it's started raining.");
        return null;
    }

    private TimbnNpcTrip Trip(string route, string destination) =>
        route switch
        {
            "spot" => new TimbnNpcTrip()
                .Leave(steps => steps.WalkNavmesh(_ramp))
                .Body(steps => steps
                    .Walk(destination)
                    .Face(_loopFacing[destination])
                    .WaitGameMinutes(15, 30))
                .Return(steps => steps
                    .Walk(_road03)
                    .WalkNavmesh(_ramp)
                    .WalkNavmesh(_stand)),
            "pond" => Away(viaLeave: false, trip => trip.Walk(_pond).WaitGameMinutes(45)),
            "bench01" => Away(viaLeave: true, trip => trip.GoToFreePoint([_bench]).WaitGameMinutes(30)),
            "bot01" => Away(viaLeave: true, trip => trip.Walk(_botSide).WaitGameMinutes(30)),
            "guard" => Away(viaLeave: true, trip => trip.Walk(_guard).WaitGameMinutes(20)),
            _ => Away(viaLeave: true, trip => trip
                .Walk(_toiletsInside)
                .WaitGameMinutes(10)
                .Walk(_toiletsOutside)
                .Teleport(_toiletsOutside)
                .GoToFreePoint(_benches)
                .WaitGameMinutes(30)
                .Walk("npc_sim_village_right_side_road_29")
                .Walk("npc_sim_village_right_side_road_32")),
        };

    private static TimbnNpcTrip Away(bool viaLeave, System.Func<TimbnNpcSteps, TimbnNpcSteps> body) =>
        new TimbnNpcTrip()
            .Leave(steps => viaLeave ? steps.WalkNavmesh(_ramp).Walk(_leave) : steps.WalkNavmesh(_ramp))
            .Body(body)
            .Return(steps => steps.Walk(_ramp).WalkNavmesh(_stand));

    private static void Log(string line)
    {
        if (PluginConfig.WriteDebugLog.Value)
            Plugin.Logger.LogDebug($"day {TimbnClock.Day} {TimbnClock.TimeText} Herm: {line}");
    }

    private static string Destination(string route, TimbnNpc herm, System.Random random) =>
        route switch
        {
            "pond" => _pond,
            "bench01" => _bench,
            "bot01" => _botSide,
            "guard" => _guard,
            "bathroom" => _toiletsInside,
            _ => _loopFacing.Keys.OrderBy(_ => random.Next()).FirstOrDefault(herm.IsFree) ?? _loopFacing.Keys.First(),
        };

    private static string TodaysRare() => TimbnRolls.ThisDay(TimbnGameIds.Npcs.Herm).Next(2) == 0 ? "guard" : "bathroom";
}
