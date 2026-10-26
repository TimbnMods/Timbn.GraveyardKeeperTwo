namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(WorldData))]
internal static class WorldDataPatch
{
    // Moving an object to another scene takes the same object out and puts it straight back, which is not a removal.
    private static int _putBack;

    [HarmonyPatch(nameof(WorldData.MoveWgoDataToAnotherGameScene))]
    [HarmonyPrefix]
    private static void MoveWgoDataToAnotherGameScenePreFix() => _putBack++;

    [HarmonyPatch(nameof(WorldData.MoveWgoDataToAnotherGameScene))]
    [HarmonyFinalizer]
    private static void MoveWgoDataToAnotherGameSceneFinalizer() => _putBack--;

    [HarmonyPatch(nameof(WorldData.RemoveWgoDataFromGameScene), typeof(WgoData), typeof(bool))]
    [HarmonyPrefix]
    private static void RemoveWgoDataFromGameScenePreFix(WorldData __instance, WgoData wgoData, out WgoData? __state) =>
        __state = __instance.GetWgoData(wgoData.UniqueId);

    [HarmonyPatch(nameof(WorldData.RemoveWgoDataFromGameScene), typeof(WgoData), typeof(bool))]
    [HarmonyPostfix]
    private static void RemoveWgoDataFromGameScenePostFix(WgoData? __state, bool clearCraftComponent)
    {
        // Keeping the craft means the object comes back, as when the game changes its id or Build Tools moves it.
        if (clearCraftComponent)
            Removed(__state);
    }

    // Wgo.RemoveWithData uses the overload by id, which does not go through the one above.
    [HarmonyPatch(nameof(WorldData.RemoveWgoDataFromGameScene), typeof(SGuid))]
    [HarmonyPrefix]
    private static void RemoveWgoDataFromGameSceneByIdPreFix(WorldData __instance, SGuid sGuid, out WgoData? __state) =>
        __state = __instance.GetWgoData(sGuid);

    [HarmonyPatch(nameof(WorldData.RemoveWgoDataFromGameScene), typeof(SGuid))]
    [HarmonyPostfix]
    private static void RemoveWgoDataFromGameSceneByIdPostFix(WgoData? __state) => Removed(__state);

    private static void Removed(WgoData? data)
    {
        if (data != null && _putBack == 0)
            TimbnPatchedEvents.RaiseWorldObjectRemoved(data);
    }
}
