using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

// Chests use the base class for F, so the checks for a chest happen here.
[HarmonyPatch(typeof(WGOInteractionHandlerBase))]
internal static class WGOInteractionHandlerBasePatch
{
    [HarmonyPatch(nameof(WGOInteractionHandlerBase.HasInteraction2))]
    [HarmonyPostfix]
    private static void HasInteraction2PostFix(WGOInteractionHandlerBase __instance, Wgo ___assignedWgo, ref bool __result)
    {
        if (ChestLabelBoard.CanLabel(__instance, ___assignedWgo))
            __result = true;
    }

    [HarmonyPatch(nameof(WGOInteractionHandlerBase.Interact2))]
    [HarmonyPrefix]
    private static bool Interact2PreFix(WGOInteractionHandlerBase __instance, Wgo ___assignedWgo, ref bool __result)
    {
        if (!ChestLabelBoard.CanLabel(__instance, ___assignedWgo) || Plugin.Instance is not { } plugin)
            return true;

        var chest = ___assignedWgo.Data;
        plugin.UI.ShowPrompt(
            LLBase.L("timbn_chest_labels_title"),
            plugin.Board.GetText(chest),
            text => plugin.Board.SetText(chest, text),
            LLBase.L("timbn_chest_labels_placeholder"));
        __result = true;
        return false;
    }
}
