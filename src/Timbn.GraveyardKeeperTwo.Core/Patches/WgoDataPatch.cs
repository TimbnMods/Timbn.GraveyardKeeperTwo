namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(WgoData))]
internal static class WgoDataPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(WgoData.FireInteractionEvent))]
    private static bool FireInteractionEventPreFix(WgoData __instance, ref bool __result)
    {
        if (!TimbnDialog.TryHandleInteraction(__instance))
            return true;

        __result = true;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(WgoData.AddInteractionEvent))]
    private static void AddInteractionEventPostFix(WgoData __instance, string id) =>
        TimbnDialog.OnEventAdded(__instance, id);
}
