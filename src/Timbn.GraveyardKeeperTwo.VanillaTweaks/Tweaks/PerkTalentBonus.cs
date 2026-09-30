namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class PerkTalentBonus
{
    private static readonly (string Id, string Name, string Use)[] _perks =
    [
        ("perk_green_thumb", "Green Thumb", "planting"),
        ("perk_master_brewer", "Master Brewer", "brewing beer and mead"),
        ("perk_sommelier", "Sommelier", "making wine"),
    ];

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        foreach (var (id, name, use) in _perks)
            plugin.Settings.While(PluginConfig.PerkTalentBonus, () => Edit(plugin, id, name, use));
    }

    private static IDisposable Edit(TimbnFrameworkPlugin plugin, string id, string name, string use)
    {
        int? craftStartTicks = null;
        return plugin.Balance.Edit<PerkDef>(
            id,
            perk =>
            {
                craftStartTicks = null;
                if (perk.craftMasteryBonus != 0)
                {
                    Plugin.Logger.LogInfo($"{name} already grants +{perk.craftMasteryBonus} talent, leaving it alone.");
                    return;
                }

                if (perk.craftStartTicks == 0)
                    return;

                craftStartTicks = perk.craftStartTicks;
                perk.craftMasteryBonus = perk.craftStartTicks;
                perk.craftStartTicks = 0;
                Plugin.Logger.LogInfo($"{name} now grants +{perk.craftMasteryBonus} talent when {use}.");
            },
            perk =>
            {
                if (craftStartTicks is not { } ticks)
                    return;

                perk.craftStartTicks = ticks;
                perk.craftMasteryBonus = 0;
                craftStartTicks = null;
            });
    }
}
