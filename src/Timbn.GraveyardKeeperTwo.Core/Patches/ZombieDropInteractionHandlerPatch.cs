namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>The zombie pickup has its own check, so it is turned off the same way while the player holds something only code takes.</summary>
[HarmonyPatch(typeof(ZombieDropInteractionHandler))]
internal static class ZombieDropInteractionHandlerPatch
{
    [HarmonyPatch(nameof(ZombieDropInteractionHandler.HasInteraction))]
    [HarmonyPostfix]
    private static void HasInteractionPostFix(ref bool __result)
    {
        if (__result && TimbnOverhead.HandsLocked)
            __result = false;
    }
}
