using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.StuckCarriers))]
[HarmonyPatch(typeof(ZombieWgoData))]
internal static class ZombieWgoDataStuckCarriersPatch
{
    [HarmonyPatch(nameof(ZombieWgoData.ShouldShowNoStorageIcon), MethodType.Getter)]
    [HarmonyPostfix]
    private static void ShouldShowNoStorageIconPostFix(ZombieWgoData __instance, ref bool __result)
    {
        if (!__result && PluginConfig.StuckCarriers.Value && StuckCarriers.IsStuck(__instance))
            __result = true;
    }
}
