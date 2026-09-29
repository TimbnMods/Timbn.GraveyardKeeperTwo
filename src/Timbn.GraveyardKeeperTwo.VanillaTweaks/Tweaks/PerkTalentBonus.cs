namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class PerkTalentBonus
{
    private static readonly (string Id, string Name, string Use)[] _perks =
    [
        ("perk_green_thumb", "Green Thumb", "planting"),
        ("perk_master_brewer", "Master Brewer", "brewing beer and mead"),
        ("perk_sommelier", "Sommelier", "making wine"),
    ];

    private readonly Dictionary<PerkDef, int> _patched = [];

    public void Apply()
    {
        foreach (var (id, name, use) in _perks)
            Apply(id, name, use);
    }

    public void Revert()
    {
        foreach (var (perk, craftStartTicks) in _patched)
        {
            perk.craftStartTicks = craftStartTicks;
            perk.craftMasteryBonus = 0;
        }

        _patched.Clear();
    }

    private void Apply(string id, string name, string use)
    {
        var perk = GameBalance.Me.GetDataOrNull<PerkDef>(id);
        if (perk == null)
        {
            Plugin.Logger.LogWarning($"The balance has no {id}, leaving {name} alone.");
            return;
        }

        if (_patched.ContainsKey(perk))
            return;

        if (perk.craftMasteryBonus != 0)
        {
            Plugin.Logger.LogInfo($"{name} already grants +{perk.craftMasteryBonus} talent, leaving it alone.");
            return;
        }

        if (perk.craftStartTicks == 0)
            return;

        _patched[perk] = perk.craftStartTicks;
        perk.craftMasteryBonus = perk.craftStartTicks;
        perk.craftStartTicks = 0;
        Plugin.Logger.LogInfo($"{name} now grants +{perk.craftMasteryBonus} talent when {use}.");
    }
}
