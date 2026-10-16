namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(HUD))]
internal static class HUDPatch
{
    // Private in the game, so it is named by string.
    [HarmonyPatch("HasWorkInteractionEvent")]
    [HarmonyPostfix]
    private static void HasWorkInteractionEventPostFix(WgoData wgoData, ref bool __result) =>
        __result = __result || TimbnQuestMarker.IsMarked(wgoData);
}
