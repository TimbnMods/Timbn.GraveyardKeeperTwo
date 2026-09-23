using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(ZombieWgoData), nameof(ZombieWgoData.ShouldShowNoStorageIcon), MethodType.Getter)]
internal static class ZombieWgoDataPatch
{
    [HarmonyPostfix]
    private static void ShowForStuckCarriers(ZombieWgoData __instance, ref bool __result)
    {
        if (!__result && PluginConfig.StuckCarriers.Value && StuckCarriers.IsStuck(__instance))
            __result = true;
    }
}
