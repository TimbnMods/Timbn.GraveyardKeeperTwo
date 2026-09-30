namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(WgoData))]
internal static class WgoDataPatch
{
    [HarmonyPatch(nameof(WgoData.FireInteractionEvent))]
    [HarmonyPrefix]
    private static bool FireInteractionEventPreFix(WgoData __instance, ref bool __result)
    {
        if (!TimbnDialog.TryHandleInteraction(__instance))
            return true;

        __result = true;
        return false;
    }

    [HarmonyPatch(nameof(WgoData.AddInteractionEvent))]
    [HarmonyPostfix]
    private static void AddInteractionEventPostFix(WgoData __instance, string id) =>
        TimbnDialog.OnEventAdded(__instance, id);
}
