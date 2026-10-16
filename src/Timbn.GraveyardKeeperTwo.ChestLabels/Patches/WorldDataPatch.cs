namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

[HarmonyPatch(typeof(WorldData))]
internal static class WorldDataPatch
{
    [HarmonyPatch(nameof(WorldData.RemoveWgoDataFromGameScene), typeof(WgoData), typeof(bool))]
    [HarmonyPostfix]
    private static void RemoveWgoDataFromGameScenePostFix(WgoData wgoData) =>
        Plugin.Instance?.Board.ChestRemoved(wgoData);
}
