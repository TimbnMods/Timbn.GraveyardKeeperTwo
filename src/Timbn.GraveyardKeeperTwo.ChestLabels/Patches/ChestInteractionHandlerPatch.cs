using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

[HarmonyPatch(typeof(ChestInteractionHandler))]
internal static class ChestInteractionHandlerPatch
{
    [HarmonyPatch("FormInteractionInfo")]
    [HarmonyPostfix]
    private static void FormInteractionInfoPostFix(ref InteractionInfos __result) =>
        __result.Add(new InteractionInfo(ControllerIconLibrary.GetIconId(GameKey.Action) + LLBase.L("timbn_chest_labels_label")));
}
