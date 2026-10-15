using GK2.FlowCanvasNodes;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(Flow_SetWGOPosition))]
internal static class Flow_SetWGOPositionPatch
{
    private static Func<GKCustomFlowNodeWithWgoData, WgoData?>? _wgoData;

    private static bool Prepare()
    {
        _wgoData ??= TimbnReflection.TryDelegate<Func<GKCustomFlowNodeWithWgoData, WgoData?>>(
            () => AccessTools.Method(typeof(GKCustomFlowNodeWithWgoData), "GetWgoData"),
            "GKCustomFlowNodeWithWgoData.GetWgoData");
        if (_wgoData != null)
            return true;

        TimbnCorePlugin.Logger.LogWarning($"{nameof(Flow_SetWGOPositionPatch)}|The game's story teleport changed, so schedules do not notice a story graph teleporting someone.");
        return false;
    }

    [HarmonyPatch("Teleport")]
    [HarmonyPrefix]
    private static void TeleportPreFix(Flow_SetWGOPosition __instance) => TimbnGameMoves.OnTeleport(_wgoData!(__instance));
}
