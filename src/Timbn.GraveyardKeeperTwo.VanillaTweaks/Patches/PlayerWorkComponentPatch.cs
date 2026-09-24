using System.Collections.Generic;
using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(PlayerWorkComponent))]
internal static class PlayerWorkComponentPatch
{
    [HarmonyPatch("FindNearestDockPoint")]
    [HarmonyPostfix]
    private static void FindNearestDockPointPostFix(
        PlayerWorkComponent __instance,
        PlayerController ___playerController,
        Vector2 direction,
        List<Wgo>? onlyWgoInList,
        ref DockPoint? __result)
    {
        if (__result == null && PluginConfig.DismantleUnreachable.Value && onlyWgoInList is [var station])
            __result = UnreachableDismantle.Active?.StandInFor(__instance, station, ___playerController.transform, direction);
    }
}
