namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(MainGame))]
internal static class MainGamePatch
{
    [HarmonyPatch(nameof(MainGame.CreateGameSaveAndStart))]
    [HarmonyPrefix]
    private static void CreateGameSaveAndStartPreFix() =>
        TimbnSafe.Run(TimbnSaves.OnNewGameStarting, $"{nameof(MainGamePatch)}|Starting the Timbn save file for a new game");

    [HarmonyPatch(nameof(MainGame.ContinueGame))]
    [HarmonyPrefix]
    private static void ContinueGamePreFix(SaveSlotData saveSlotData, GameSave gameSave) =>
        TimbnSafe.Run(() => TimbnSaves.OnContinueGame(saveSlotData, gameSave), $"{nameof(MainGamePatch)}|Reading the Timbn save file for slot {saveSlotData?.slotName}");
}
