namespace Timbn.GraveyardKeeperTwo.InfusionRack;

internal static class InfusionMachine
{
    public const string StationId = "timbn_infusion_machine";
    private const string _buildIcon = "infusion_machine_build";
    // A timed station that runs by itself, with its fuel taken away.
    private const string _oven = "kitchen_oven";
    private const string _buildingTalent = "talent_orange";
    // Has no output, tech points or mastery above 1.
    private const string _template = "pocket_extract_item_craft";
    private const string _blueCrystal = "crystal_goddess";
    private const int _ticks = 8;

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        var model = plugin.Models.Build("InfusionMachine", faces => faces
            .Block("infusion_machine_table", 0, 0, 1, 24)
            .Top("infusion_machine_basin", 5, 17, 5));

        plugin.Sprites.AddEmbeddedPng(_buildIcon);
        var machine = plugin.Stations.Add(StationId, station => station
            // Two cells across by two deep, with a stand's slot on each side. A stand is a cell deeper, flush with the machine's front.
            .Model(model, new Vector3(0.64f, 0.5f, 0.6f))
            .Slot(InfusionStand.SlotId, new Vector3(-0.64f, 0f, 0.15f), InfusionStand.Size)
            .Slot(InfusionStand.SlotId, new Vector3(0.64f, 0f, 0.15f), InfusionStand.Size)
            .Icon(_buildIcon)
            .BasedOn(_oven)
            // Dismantled under the building talent, not the oven's farming talent.
            .Talent(_buildingTalent)
            .NoFuel()
            .TakesZombie(false)
            .BuiltAt(TimbnGameIds.BuildDesks.Morgue)
            .BuildTab(TimbnGameIds.BuildTabs.First)
            .UnlockedBy(InfusionTech.Id)
            .Cost(("stone_plate_1", 8), ("nails_iron", 6), ("detail_1_iron", 4))
            .Limit(1)
            .Extensions(InfusionStand.StationId));

        foreach (var color in SpiritSolution.Colors)
        {
            var solution = SpiritSolution.ItemId(color);
            var lower = color.ToString().ToLowerInvariant();
            plugin.Crafts.Add($"{StationId}_{lower}", craft => craft
                .In(machine)
                .BasedOn(_template)
                .Needs((solution, 1), (_blueCrystal, 1))
                .Mastery(1)
                .Icon(solution)
                .DurationTicks(_ticks)
                .AutoFinish()
                // An injection with no zombie lying in a stand would only waste the solution and the crystal.
                .Requires(machine => InfusionStand.OccupiedStands(machine).Any())
                .OnStarted(infusion => StartOnStands(plugin, infusion.Station))
                .OnFinished(infusion => Infuse(infusion.Station, color)));
        }
    }

    private static void StartOnStands(TimbnFrameworkPlugin plugin, WgoData machine)
    {
        // Empty stands run it too, so nothing can be laid in once it has begun.
        foreach (var stand in InfusionStand.IdleStands(machine))
        {
            if (!plugin.Crafts.Start(stand, InfusionStand.InfusionId))
                Plugin.Logger.LogWarning($"Could not start the stand infusion on {stand.id}.");
        }
    }

    private static void Infuse(WgoData machine, TechColor color)
    {
        // Read from the stands, not remembered, so an infusion that was running when the game saved still pays.
        foreach (var stand in InfusionStand.InfusionStands(machine).ToList())
        {
            if (TimbnZombies.AtStation(stand) is { } zombie)
                SpiritSolution.GiveTo(zombie, machine, color);

            stand.CraftComponent.TryFinishCurCraft();

            // The craft's end lights the lines, which an empty stand must not show.
            if (TimbnZombies.AtStation(stand) == null)
                InfusionStand.ClearGlow(stand);
        }
    }
}
