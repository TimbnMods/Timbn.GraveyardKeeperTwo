namespace Timbn.GraveyardKeeperTwo.InfusionRack;

// One recipe per organ, and its solution takes the color of the rune only that organ carries.
internal static class SpiritRecipes
{
    private const string _cube = "distillation_cube";
    // The cube's fragrance, so the time, the way it runs by itself and the rest match a recipe of the cube's own.
    private const string _template = "fragrance";
    // The cube's own recipes burn 20 fire.
    private const string _fire = "fire";
    private const int _firePerCraft = 20;
    // Alchemy Flasks I, the glass the alchemy lab and flask shed work with. The solution goes in it.
    private const string _flask = "alchemy_bottle_1";

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
                .In(_cube)
                .BasedOn(_template)
                .Needs((organ, 1), (_fire, _firePerCraft), (_flask, 1))
                .Makes(solution, skulls)
                .Mastery(1)
                .Icon(solution)
                .UnlockedBy(InfusionTech.Id)
                // The fragrance's own tech points, inspiration and perks don't belong to a solution.
                .Edit(def =>
                {
                    def.techRed = new LazyExpression();
                    def.techGreen = new LazyExpression();
                    def.techBlue = new LazyExpression();
                    def.linkedPerks = [];
                    def.onCraftEndExpressions = [];
                }));
        }
    }
}
