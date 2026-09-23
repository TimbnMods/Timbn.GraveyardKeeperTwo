namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(QuestSystemData), nameof(QuestSystemData.PrepareForGame))]
internal static class QuestSystemDataPatch
{
    private static void Prefix(QuestSystemData __instance) => TimbnQuests.StubOrphans(__instance);

    private static void Postfix(QuestSystemData __instance) => TimbnQuests.OnQuestsPrepared(__instance);
}
