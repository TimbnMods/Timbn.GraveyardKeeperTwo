namespace Timbn.GraveyardKeeperTwo.InfusionRack;

internal enum TechColor
{
    Red,
    Green,
    Blue
}

/// <summary>
/// The solutions the distillation cube makes, which infuse zombies with tech points. They are copied from the game's embalm item for its
/// look and stacking but are not embalms, so the embalming table never offers them.
/// </summary>
internal static class SpiritSolution
{
    private const string _itemTemplate = "embalm_white:1";

    public static IEnumerable<TechColor> Colors => Enum.GetValues(typeof(TechColor)).Cast<TechColor>();

    public static string ItemId(TechColor color) => $"timbn_spirit_solution_{color.ToString().ToLowerInvariant()}";

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        foreach (var color in Colors)
        {
            var id = ItemId(color);
            plugin.Sprites.AddEmbeddedPng(id);
            plugin.Balance.AddItem(_itemTemplate, id, "", "", id, item =>
            {
                item.type = ItemType.None;
                item.whiteSkulls = 0;
                item.redSkulls = 0;
                // The embalm it is copied from is a bronze star item, and a solution has no quality.
                item.quality = 0;
                item.qualityType = ItemDef.QualityType.None;
            });
        }
    }

    public static void GiveTo(ZombieWgoData zombie, WgoData table, TechColor color)
    {
        var points = PluginConfig.PointsPerSolution.Value;
        zombie.DoTechPointsReward(table, color == TechColor.Red ? points : 0, color == TechColor.Green ? points : 0, color == TechColor.Blue ? points : 0);
    }
}
