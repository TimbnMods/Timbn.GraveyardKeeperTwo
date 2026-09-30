using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.StuckResourceWorkers))]
[HarmonyPatch(typeof(ZombieWgoData))]
internal static class ZombieWgoDataStuckResourceWorkersPatch
{
    [HarmonyPatch(nameof(ZombieWgoData.UnAttachFromWgoData))]
    [HarmonyPrefix]
    private static void UnAttachFromWgoDataPreFix(ZombieWgoData __instance) =>
        StuckResourceWorkers.OnDetaching(__instance);
}
