namespace Timbn.GraveyardKeeperTwo.InfusionRack;

// One recipe per organ, and its solution takes the color of the rune only that organ carries.
internal static class SpiritRecipes
{
    // The cube's own recipes burn 20 fire.
    private const int _firePerCraft = 20;

    // Every heart, guts and brain holding white skulls in game 1.010, with its white skulls. Check it against the
    // item data after a game update.
    private static readonly (string Organ, TechColor Color, int Skulls)[] _organs =
    [
        ("heart_0_1:1", TechColor.Red, 1),
        ("heart_1_1:1", TechColor.Red, 1),
        ("heart_3_1:3", TechColor.Red, 1),
        ("heart_1_2:2", TechColor.Red, 2),
        ("heart_2_2:2", TechColor.Red, 2),
        ("heart_0_3:3", TechColor.Red, 3),
        ("guts_2_1:2", TechColor.Green, 1),
        ("guts_2_2:2", TechColor.Green, 1),
        ("guts_1_3:3", TechColor.Green, 3),
        ("brain_0_1:1", TechColor.Blue, 1),
        ("brain_2_1:2", TechColor.Blue, 1),
        ("brain_3_1:3", TechColor.Blue, 1),
        ("brain_0_2:2", TechColor.Blue, 2),
        ("brain_0_3:3", TechColor.Blue, 3),
    ];

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        foreach (var (organ, color, skulls) in _organs)
        {
            var solution = SpiritSolution.ItemId(color);
            plugin.Crafts.Add($"timbn_spirit_{organ.Replace(':', '_')}", craft => craft
                .In(TimbnGameIds.Workstations.AlchemyLab.DistillationCube)
                // The cube's fragrance, so the time, the way it runs by itself and the rest match a recipe of the cube's own.
                .BasedOn(TimbnGameIds.Items.Fragrance)
                .Needs((organ, 1), (TimbnGameIds.Items.Fire, _firePerCraft), (TimbnGameIds.Items.AlchemyBottle1, 1))
                .Makes(solution, skulls)
                .Mastery(1)
                .Icon(solution)
                .UnlockedBy(InfusionTech.Id));
        }
    }
}
