namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Sizes and colors the icon of a Core bubble, and puts the game's own icons back to their own size and color.</summary>
[HarmonyPatch(typeof(UIWorkbenchAdditionWorldIconWidget))]
internal static class UIWorkbenchAdditionWorldIconWidgetPatch
{
    [HarmonyPatch(nameof(UIWorkbenchAdditionWorldIconWidget.Redraw))]
    [HarmonyPostfix]
    private static void RedrawPostFix(UIWorkbenchAdditionWorldIconWidget __instance) =>
        TimbnSafe.Run(() => TimbnBubble.Style(__instance), $"{nameof(UIWorkbenchAdditionWorldIconWidgetPatch)}|The look of a bubble");
}
