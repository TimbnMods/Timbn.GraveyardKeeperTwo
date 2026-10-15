namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// The village and the town districts people live in, and who in each is free to ask the player for something. A
/// town district only has people once the player has rebuilt a house or plot there, which needs its fights won
/// first, so a district with people is one the player can reach.
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
