namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(QuestSystemData))]
internal static class QuestSystemDataPatch
{
    [HarmonyPatch(nameof(QuestSystemData.PrepareForGame))]
    [HarmonyPrefix]
    private static void PrepareForGamePreFix(QuestSystemData __instance) =>
        TimbnQuests.StubOrphans(__instance);

    [HarmonyPatch(nameof(QuestSystemData.PrepareForGame))]
    [HarmonyPostfix]
    private static void PrepareForGamePostFix(QuestSystemData __instance) =>
        TimbnQuests.OnQuestsPrepared(__instance);
}
