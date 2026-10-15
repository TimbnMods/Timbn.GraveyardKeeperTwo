namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(UIMagnifyingGlassWidget))]
internal static class UIMagnifyingGlassWidgetPatch
{
    [HarmonyPatch(nameof(UIMagnifyingGlassWidget.Draw))]
    [HarmonyPostfix]
    private static void DrawPostFix(UIMagnifyingGlassWidget __instance, UIMagnifyingGlassWidgetData data) =>
        TimbnQuestMarker.FixDoorIcon(__instance, data);
}
