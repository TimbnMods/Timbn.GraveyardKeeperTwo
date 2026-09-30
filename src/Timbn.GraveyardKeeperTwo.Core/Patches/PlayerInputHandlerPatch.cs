namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the player's input that skips the game's own interaction while a plugin's hint claims the key.</summary>
[HarmonyPatch(typeof(PlayerInputHandler))]
internal static class PlayerInputHandlerPatch
{
    [HarmonyPatch(nameof(PlayerInputHandler.UpdateInput))]
    [HarmonyPrefix]
    private static bool UpdateInputPreFix() =>
        !TimbnInteractionHint.IsKeyClaimed();
}
