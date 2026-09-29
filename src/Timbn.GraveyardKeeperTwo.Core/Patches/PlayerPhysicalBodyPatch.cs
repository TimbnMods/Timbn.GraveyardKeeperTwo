namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(PlayerPhysicalBody))]
internal static class PlayerPhysicalBodyPatch
{
    [HarmonyPatch(nameof(PlayerPhysicalBody.LockMovement))]
    [HarmonyPrefix]
    private static bool LockMovementPreFix(PlayerPhysicalBody __instance, bool isLock) =>
        TimbnMovement.Allow(__instance, isLock);
}
