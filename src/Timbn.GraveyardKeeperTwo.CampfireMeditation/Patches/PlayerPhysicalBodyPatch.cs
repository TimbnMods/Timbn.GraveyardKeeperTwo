namespace Timbn.GraveyardKeeperTwo.CampfireMeditation.Patches;

[HarmonyPatch(typeof(PlayerPhysicalBody))]
internal static class PlayerPhysicalBodyPatch
{
    [HarmonyPatch(nameof(PlayerPhysicalBody.LockMovement))]
    [HarmonyPrefix]
    private static bool LockMovementPreFix(PlayerPhysicalBody __instance, bool isLock) => MovementLock.Allow(__instance, isLock);
}
