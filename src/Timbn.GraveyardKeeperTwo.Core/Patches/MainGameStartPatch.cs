namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(MainGame))]
internal static class MainGameStartPatch
{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void StartPostFix() =>
        TimbnSafe.Run(TimbnGameEvents.OnMainGameStarted, $"{nameof(MainGameStartPatch)}|Moving the game start handlers behind the game's own");
}
