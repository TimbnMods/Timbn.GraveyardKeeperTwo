namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(PlayerInputHandler))]
internal static class PlayerInputHandlerPatch
{
    [HarmonyPatch(nameof(PlayerInputHandler.UpdateInput))]
    [HarmonyPrefix]
    private static bool UpdateInputPreFix()
    {
        TimbnCarry.IsReadingInput = true;
        return !TimbnInteractionHint.IsKeyClaimed();
    }

    [HarmonyPatch(nameof(PlayerInputHandler.UpdateInput))]
    [HarmonyFinalizer]
    private static void UpdateInputFinalizer() =>
        TimbnCarry.IsReadingInput = false;

    [HarmonyPatch(nameof(PlayerInputHandler.UpdateInputInteractionOnly))]
    [HarmonyPrefix]
    private static bool UpdateInputInteractionOnlyPreFix()
    {
        TimbnCarry.IsReadingInput = true;
        return !TimbnInteractionHint.IsKeyClaimed();
    }

    [HarmonyPatch(nameof(PlayerInputHandler.UpdateInputInteractionOnly))]
    [HarmonyFinalizer]
    private static void UpdateInputInteractionOnlyFinalizer() =>
        TimbnCarry.IsReadingInput = false;
}
