namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

[HarmonyPatch(typeof(WorldData))]
internal static class WorldDataPatch
{
    // Changing an object's id removes it with its craft kept and adds it straight back, which is not a removal.
    [HarmonyPatch(nameof(WorldData.RemoveWgoDataFromGameScene), typeof(WgoData), typeof(bool))]
    [HarmonyPostfix]
    private static void RemoveWgoDataFromGameScenePostFix(WgoData wgoData, bool clearCraftComponent)
    {
        if (clearCraftComponent)
            Plugin.Instance?.Board.ChestRemoved(wgoData.UniqueId);
    }

    // Wgo.RemoveWithData uses this one, which does not go through the overload above.
    [HarmonyPatch(nameof(WorldData.RemoveWgoDataFromGameScene), typeof(SGuid))]
    [HarmonyPostfix]
    private static void RemoveWgoDataFromGameScenePostFix(SGuid sGuid) =>
        Plugin.Instance?.Board.ChestRemoved(sGuid);
}
