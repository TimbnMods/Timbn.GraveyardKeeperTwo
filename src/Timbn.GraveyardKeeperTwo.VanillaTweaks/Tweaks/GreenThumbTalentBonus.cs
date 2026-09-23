namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class GreenThumbTalentBonus
{
    private const string _perkId = "perk_green_thumb";

    private PerkDef? _patched;
    private int _craftStartTicks;

    public void Apply()
    {
        if (_patched != null)
            return;

        var perk = GameBalance.Me.GetDataOrNull<PerkDef>(_perkId);
        if (perk == null)
        {
            Plugin.Logger.LogWarning($"The balance has no {_perkId}, leaving Green Thumb alone.");
            return;
        }

        if (perk.craftMasteryBonus != 0)
        {
            Plugin.Logger.LogInfo($"Green Thumb already grants +{perk.craftMasteryBonus} green talent, leaving it alone.");
            return;
        }

        if (perk.craftStartTicks == 0)
            return;

        _craftStartTicks = perk.craftStartTicks;
        perk.craftMasteryBonus = perk.craftStartTicks;
        perk.craftStartTicks = 0;
        _patched = perk;
        Plugin.Logger.LogInfo($"Green Thumb now grants +{perk.craftMasteryBonus} green talent when planting.");
    }

    public void Revert()
    {
        if (_patched == null)
            return;

        _patched.craftStartTicks = _craftStartTicks;
        _patched.craftMasteryBonus = 0;
        _patched = null;
    }
}
