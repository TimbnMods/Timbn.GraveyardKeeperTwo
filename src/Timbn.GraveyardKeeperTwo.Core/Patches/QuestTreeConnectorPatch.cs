namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(QuestTreeConnector))]
internal static class QuestTreeConnectorPatch
{
    [HarmonyPatch(nameof(QuestTreeConnector.Draw))]
    [HarmonyPostfix]
    private static void DrawPostFix(QuestTreeConnector __instance, LazyScrollableElement child, bool isBackground) =>
        TimbnQuestTree.TintLine(__instance, child, isBackground);
}
