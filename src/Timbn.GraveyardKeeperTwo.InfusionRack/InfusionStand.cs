namespace Timbn.GraveyardKeeperTwo.InfusionRack;

internal static class InfusionStand
{
    public const string StationId = "timbn_infusion_stand";
    public const string InfusionId = "timbn_infusion_stand_infusion";
    // The id of the slots on the machine, where a stand is built.
    public const string SlotId = "timbn_infusion_slot";
    public static readonly Vector3 Size = new(0.64f, 0.5f, 0.9f);
    private const string _buildIcon = "infusion_stand_build";
    private const string _infusedFlag = "timbn_infusion_done";
    private const string _resurrectionTable = "resurrection_table_1";
    private const string _buildingTalent = "talent_orange";
    // Has no needs and no output.
    private const string _template = "pocket_extract_item_craft";
    private const int _infusionTicks = 600;

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        var open = plugin.Models.Build("InfusionStandOpen", faces => faces
            .Block("infusion_stand_box", 0, 0, 1, 36)
            .Top("infusion_stand_floor", 4, 6, 5)
            .Front("infusion_stand_backwall", 4, 6, 33));

        var lid = plugin.Models.Build("InfusionStandLid", faces => faces
            .Block("infusion_stand_lying_lid", 2, 16, 4, 30));

        var glow = plugin.Models.Build("InfusionStandGlow", faces => faces
            .Front("infusion_stand_working_gemglow", 8, 7, 0)
            .Top("infusion_stand_working_lidlines", 2, 20, 4)
            .Front("infusion_stand_working_lidedge", 2, 17, 3));

        plugin.Sprites.AddEmbeddedPng(_buildIcon);
        var stand = plugin.Stations.Add(StationId, station => station
            .Model(open, Size)
            .Icon(_buildIcon)
            .BasedOn(_resurrectionTable)
            .HoldsZombie()
            // Dismantled under the building talent, not the resurrection table's.
            .Talent(_buildingTalent)
            .ShowWhen(TimbnLook.HoldsGroup("zombie"), lid)
            .ShowWhen(TimbnLook.Resource(_infusedFlag, TimbnComparison.GreaterOrEqual, 1), glow)
            .Edit(def =>
            {
                // Taking the zombie out puts the lines out, appended to the game's own take.
                def.customInteraction2.execution.Add(SetFlag(0));
                // An empty stand runs the infusion too, which is what keeps a zombie from being put in halfway.
                def.customInteraction.condition = TimbnBalance.Expression("PlayerHasOvrhdAndCanInsert() && IsNotCraftingNow()");
            })
            .BuildsInSlot(SlotId)
            .BuiltAt(TimbnGameIds.BuildDesks.Morgue)
            .BuildTab(TimbnGameIds.BuildTabs.First)
            .UnlockedBy(InfusionTech.Id)
            .Cost(("stone_plate_1", 6), ("nails_iron", 4), ("detail_1_iron", 2)));

        plugin.Events.SaveReady(RepairEmptyStands);

        plugin.Crafts.Add(InfusionId, craft => craft
            .In(stand)
            .BasedOn(_template)
            .Mastery(1)
            .DurationTicks(_infusionTicks)
            .AutoFinish()
            // The game draws a progress cell per tick over a crafting station unless the craft is hidden.
            .Edit(def =>
            {
                def.isHidden = true;
                def.onCraftStartExpressions.Add(SetFlag(0));
                def.onCraftEndExpressions.Add(SetFlag(1));
            }));
    }

    public static void ClearGlow(WgoData stand) => stand.SetGameRes(_infusedFlag, 0);

    // A save that lost its zombie while the mod was away can hold a lit stand with nothing in it.
    // Core already unlocks the stand and frees its slot.
    private static void RepairEmptyStands()
    {
        foreach (var stand in TimbnWorld.FindAll(StationId))
        {
            if (TimbnZombies.AtStation(stand) == null && stand.CraftComponent.CurrentCraftElement?.Def?.id != InfusionId)
                ClearGlow(stand);
        }
    }

    private static LazyExpression SetFlag(int value) =>
        TimbnBalance.Expression($"SetWGOPar(\"{_infusedFlag}\", {value})");

    public static IEnumerable<WgoData> StandsOf(WgoData machine) =>
        TimbnWorld.Extensions(machine).Where(stand => stand.id == StationId);

    public static IEnumerable<WgoData> InfusionStands(WgoData machine) =>
        StandsOf(machine).Where(stand => stand.CraftComponent.CurrentCraftElement?.Def?.id == InfusionId);

    public static IEnumerable<WgoData> OccupiedStands(WgoData machine) =>
        StandsOf(machine).Where(stand => TimbnZombies.AtStation(stand) != null && stand.CraftComponent.CurrentCraftElement?.Def?.id != InfusionId);

    public static IEnumerable<WgoData> IdleStands(WgoData machine) =>
        StandsOf(machine).Where(stand => stand.CraftComponent.CurrentCraftElement?.Def?.id != InfusionId);
}
