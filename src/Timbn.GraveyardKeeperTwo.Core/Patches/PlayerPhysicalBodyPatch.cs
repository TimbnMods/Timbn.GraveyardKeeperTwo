namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the player's movement that applies the walk speed holds.</summary>
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
