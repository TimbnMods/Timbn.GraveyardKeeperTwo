namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(WgoData), nameof(WgoData.FireInteractionEvent))]
internal static class WgoDataPatch
{
    private static bool Prefix(WgoData __instance, ref bool __result)
    {
        if (!TimbnDialog.TryHandleInteraction(__instance))
            return true;

        __result = true;
        return false;
    }
}
