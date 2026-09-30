namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on starting a new game, so Core knows the next save has no mod data yet.</summary>
[HarmonyPatch(typeof(MainGame))]
internal static class MainGamePatch
{
    [HarmonyPatch(nameof(MainGame.CreateGameSaveAndStart))]
    [HarmonyPrefix]
    private static void CreateGameSaveAndStartPreFix() =>
        TimbnSaves.OnNewGameStarting();
}
