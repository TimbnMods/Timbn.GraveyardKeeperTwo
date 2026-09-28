using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(DropSystem))]
internal static class DropSystemPatch
{
    [HarmonyPatch(nameof(DropSystem.CollectAllGameResDropsToPlayer))]
    [HarmonyPostfix]
    private static void CollectAllGameResDropsToPlayerPostFix()
    {
        if (PluginConfig.RecoverBattleRewards.Value)
            LostBattleRewards.Recover();
    }
}
