using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

[HarmonyPatch(typeof(Wgo))]
internal static class WgoPatch
{
    [HarmonyPatch(nameof(Wgo.GetWidgetData))]
    [HarmonyPostfix]
    private static void GetWidgetDataPostFix(Wgo __instance, List<LazyWidgetDataBase> __result) =>
        Plugin.Instance?.Board.AddLabel(__instance, __result);
}
