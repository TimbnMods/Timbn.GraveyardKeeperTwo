namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(MovementComponent))]
internal static class MovementComponentRoutePatch
{
    private static bool Prepare()
    {
        if (TimbnPlannedRoute.IsAvailable)
            return true;

        TimbnCorePlugin.Logger.LogWarning($"{nameof(MovementComponentRoutePatch)}|The game's walker changed, so people walking the paths keep to the one way links.");
        return false;
    }

    [HarmonyPatch("FindPathGDGraph")]
    [HarmonyPrefix]
    private static bool FindPathGDGraphPreFix(MovementComponent __instance) => !TimbnPlannedRoute.TryUse(__instance);

    [HarmonyPatch("FindPathRecastGraph")]
    [HarmonyPrefix]
    private static bool FindPathRecastGraphPreFix(MovementComponent __instance) => !TimbnPlannedRoute.TryUseCorners(__instance);
}
