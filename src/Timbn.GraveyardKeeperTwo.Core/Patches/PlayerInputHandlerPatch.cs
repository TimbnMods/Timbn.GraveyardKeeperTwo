namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(PlayerInputHandler))]
internal static class PlayerInputHandlerPatch
{
    [HarmonyPatch(nameof(PlayerInputHandler.UpdateInput))]
    [HarmonyPrefix]
    private static bool UpdateInputPreFix() =>
        !TimbnInteractionHint.IsKeyClaimed();
}
