namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

// Chests use the base class for F, so the checks for a chest happen here.
[HarmonyPatch(typeof(WGOInteractionHandlerBase))]
internal static class WGOInteractionHandlerBasePatch
{
    [HarmonyPatch(nameof(WGOInteractionHandlerBase.HasInteraction2))]
    [HarmonyPostfix]
    private static void HasInteraction2PostFix(WGOInteractionHandlerBase __instance, ref bool __result)
    {
        if (ChestLabelBoard.IsChest(__instance))
            __result = true;
    }

    [HarmonyPatch(nameof(WGOInteractionHandlerBase.Interact2))]
    [HarmonyPrefix]
    private static bool Interact2PreFix(WGOInteractionHandlerBase __instance, Wgo ___assignedWgo, ref bool __result)
    {
        if (!ChestLabelBoard.IsChest(__instance) || Plugin.Instance is not { } plugin)
            return true;

        plugin.Window.OpenFor(___assignedWgo.Data);
        __result = true;
        return false;
    }
}
