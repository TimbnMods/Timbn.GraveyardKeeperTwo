namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Turns the pickup hint and the pickup off while the player holds something only code takes.</summary>
[HarmonyPatch(typeof(BigDropInteractionHandler))]
internal static class BigDropInteractionHandlerPatch
{
    [HarmonyPatch(nameof(BigDropInteractionHandler.HasInteraction))]
    [HarmonyPostfix]
    private static void HasInteractionPostFix(ref bool __result)
    {
        if (__result && TimbnCarry.HandsLocked)
            __result = false;
    }
}
