namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.DismantleUnreachable))]
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
        if (__result == null && onlyWgoInList is [var station])
            __result = Plugin.Instance?.Dismantle.StandInFor(__instance, station, ___playerController.transform, direction);
    }
}
