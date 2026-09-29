namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(MainGame))]
internal static class MainGamePatch
{
    [HarmonyPatch(nameof(MainGame.CreateGameSaveAndStart))]
    [HarmonyPrefix]
    private static void CreateGameSaveAndStartPreFix() =>
        TimbnSaves.OnNewGameStarting();
}
