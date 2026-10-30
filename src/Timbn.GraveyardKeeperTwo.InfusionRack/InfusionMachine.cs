namespace Timbn.GraveyardKeeperTwo.InfusionRack;

internal static class InfusionMachine
{
    public const string StationId = "timbn_infusion_machine";
    private const string _buildIcon = "infusion_machine_build";
    private const string _skullTalent = "talent_blue";
    // Has no output, tech points or mastery above 1.
    private const string _template = "pocket_extract_item_craft";
    private const int _ticks = 8;
    private const int _mastery = 4;

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        var model = plugin.Models.Build("InfusionMachine", faces => faces
            .Block("infusion_machine_table", 0, 0, 1, 24)
            .Top("infusion_machine_basin", 5, 17, 5));

        plugin.Sprites.AddEmbedded(_buildIcon + ".png",TimbnSpriteKind.Blueprint);
        var machine = plugin.Stations.Add(StationId, station => station
            // Two cells across by two deep, with a stand's slot on each side. A stand is a cell deeper, flush with the machine's front.
            .Model(model, new Vector3(0.64f, 0.5f, 0.6f))
            .Slot(InfusionStand.SlotId, new Vector3(-0.64f, 0f, 0.15f), InfusionStand.Size)
            .Slot(InfusionStand.SlotId, new Vector3(0.64f, 0f, 0.15f), InfusionStand.Size)
            .Icon(_buildIcon)
            // A timed station that runs by itself, with its fuel taken away.
            .BasedOn(TimbnGameIds.Workstations.Placed.KitchenOven)
            // The infusions' mastery lock is read against the skull talent, which dismantling then uses too.
            .Talent(_skullTalent)
            .NoFuel()
            .TakesZombie(false)
            .CraftSlots(50)
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
                .Needs((solution, 1), (TimbnGameIds.Items.CrystalGoddess, 1))
                .Mastery(_mastery)
                .Icon(solution)
                .DurationTicks(_ticks)
                .AutoFinish()
                // An injection with no zombie lying in a stand would only waste the solution and the crystal.
                .Requires(machine => InfusionStand.OccupiedStands(machine).Any())
                .OnStarted(infusion => StartOnStands(plugin, infusion.Station))
                .OnFinished(infusion => Infuse(plugin, infusion.Station, color)));
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

    private static void Infuse(TimbnFrameworkPlugin plugin, WgoData machine, TechColor color)
    {
        // Read from the stands, not remembered, so an infusion that was running when the game saved still pays.
        foreach (var stand in InfusionStand.InfusionStands(machine).ToList())
        {
            if (TimbnZombies.AtStation(stand) is { } zombie)
                SpiritSolution.GiveTo(zombie, machine, color);

            plugin.Crafts.Finish(stand);
        }
    }
}
