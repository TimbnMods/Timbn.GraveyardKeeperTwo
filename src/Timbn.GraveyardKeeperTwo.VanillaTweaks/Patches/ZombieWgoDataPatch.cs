using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(ZombieWgoData))]
internal static class ZombieWgoDataPatch
{
    [HarmonyPatch(nameof(ZombieWgoData.ShouldShowNoStorageIcon), MethodType.Getter)]
    [HarmonyPostfix]
    private static void ShowForStuckCarriers(ZombieWgoData __instance, ref bool __result)
    {
        if (!__result && PluginConfig.StuckCarriers.Value && StuckCarriers.IsStuck(__instance))
            __result = true;
    }

    [HarmonyPatch(nameof(ZombieWgoData.UnAttachFromWgoData))]
    [HarmonyPrefix]
    private static void UnAttachFromWgoDataPreFix(ZombieWgoData __instance) =>
        StuckResourceWorkers.Active?.OnDetaching(__instance);
}
