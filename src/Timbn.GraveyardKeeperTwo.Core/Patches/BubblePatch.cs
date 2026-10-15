namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(Bubble))]
internal static class BubblePatch
{
    [HarmonyPatch(nameof(Bubble.ShowMultiAnswer))]
    [HarmonyPrefix]
    private static void ShowMultiAnswerPreFix(List<AnswerVisualData> answers, Transform targetTransform, WgoData dialogParticipant, ref Action<string> onChosen, Action onDisappeared, bool isOverBlackout) =>
        TimbnDialog.AddToMenu(answers, targetTransform, dialogParticipant, ref onChosen, onDisappeared, isOverBlackout);
}
