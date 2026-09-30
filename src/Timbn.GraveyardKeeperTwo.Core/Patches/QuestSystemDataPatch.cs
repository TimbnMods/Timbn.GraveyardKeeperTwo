namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the quest system's load that keeps saved quests without a definition and lays out the quest tree.</summary>
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
