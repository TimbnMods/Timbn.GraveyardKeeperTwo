namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(PlayerPhysicalBody))]
internal static class PlayerPhysicalBodyPatch
{
    [HarmonyPatch(nameof(PlayerPhysicalBody.MoveByDirection))]
    [HarmonyPrefix]
    private static void MoveByDirectionPreFix(PlayerPhysicalBody __instance, ref Vector2 direction, out float? __state)
    {
        TimbnPlayerWalk.Steer(__instance, ref direction);
        __state = TimbnMovement.SpeedUp(__instance);
    }

    [HarmonyPatch(nameof(PlayerPhysicalBody.MoveByDirection))]
    [HarmonyFinalizer]
    private static void MoveByDirectionFinalizer(PlayerPhysicalBody __instance, float? __state)
    {
        if (__state is { } gameSpeed)
            __instance.SpeedMultiplier = gameSpeed;
    }
}
