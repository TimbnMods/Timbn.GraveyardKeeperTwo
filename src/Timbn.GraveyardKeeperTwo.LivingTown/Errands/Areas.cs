namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// The village and town districts where residents live, and who in each is available to offer player errands. A
/// town district is only populated once the player clears its combat encounters and rebuilds a building there,
/// ensuring populated districts are always accessible.
/// </summary>
internal static class Areas
{
    public const string VillageKey = "timbn_living_town_where_village";
    public const string TownKey = "timbn_living_town_where_town";

    private static readonly string[] _towns =
    [
        TimbnGameIds.Groups.TownA1DayAll,
        TimbnGameIds.Groups.TownA2DayAll,
        TimbnGameIds.Groups.TownA3DayAll,
        TimbnGameIds.Groups.TownA4DayAll,
    ];

    public static List<string> FreeInVillage() => FreeIn(TimbnGameIds.Groups.VillagersDayAll);

    public static List<List<string>> FreeInTowns() => [.. _towns.Select(FreeIn).Where(people => people.Count > 0)];

    private static List<string> FreeIn(string group) =>
        [.. TimbnNpcs.GroupMemberIds(group).Distinct().Where(npc => !TimbnQuests.HasQuestRun(npc))];
}
