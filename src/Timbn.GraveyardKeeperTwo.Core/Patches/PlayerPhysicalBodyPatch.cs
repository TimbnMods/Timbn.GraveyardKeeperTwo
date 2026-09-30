namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(PlayerPhysicalBody))]
internal static class PlayerPhysicalBodyPatch
{
    [HarmonyPatch(nameof(PlayerPhysicalBody.MoveByDirection))]
    [HarmonyPrefix]
    private static void MoveByDirectionPreFix(PlayerPhysicalBody __instance, out float? __state) =>
        __state = TimbnMovement.SpeedUp(__instance);

    [HarmonyPatch(nameof(PlayerPhysicalBody.MoveByDirection))]
    [HarmonyFinalizer]
    private static void MoveByDirectionFinalizer(PlayerPhysicalBody __instance, float? __state)
    {
        if (__state is { } gameSpeed)
            __instance.SpeedMultiplier = gameSpeed;
    }
}
