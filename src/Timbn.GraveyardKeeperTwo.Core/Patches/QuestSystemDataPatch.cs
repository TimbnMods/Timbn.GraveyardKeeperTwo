namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(QuestSystemData))]
internal static class QuestSystemDataPatch
{
    [HarmonyPatch(nameof(QuestSystemData.PrepareForGame))]
    [HarmonyPrefix]
    private static void PrepareForGamePreFix(QuestSystemData __instance) =>
        TimbnSafe.Run(() => TimbnQuests.StubOrphans(__instance), $"{nameof(QuestSystemDataPatch)}|Standing in for saved quests whose mod is gone");

    [HarmonyPatch(nameof(QuestSystemData.PrepareForGame))]
    [HarmonyPostfix]
    private static void PrepareForGamePostFix(QuestSystemData __instance) =>
        TimbnSafe.Run(() => TimbnQuests.OnQuestsPrepared(__instance), $"{nameof(QuestSystemDataPatch)}|Preparing mod quests for the save");
}
