using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

[HarmonyPatch(typeof(ChestInteractionHandler))]
internal static class ChestInteractionHandlerPatch
{
    [HarmonyPatch("FormInteractionInfo")]
    [HarmonyPostfix]
    private static void FormInteractionInfoPostFix(ChestInteractionHandler __instance, Wgo ___assignedWgo, ref InteractionInfos __result)
    {
        if (ChestLabelBoard.CanLabel(__instance, ___assignedWgo))
            __result.Add(new InteractionInfo(ControllerIconLibrary.GetIconId(GameKey.Action) + LLBase.L("timbn_chest_labels_label")));
    }
}
