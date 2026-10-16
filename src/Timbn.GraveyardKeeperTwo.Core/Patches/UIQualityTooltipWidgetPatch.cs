namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Hides the icon slot of a Core label that has no icon, and shows it again for every other quality label since the widgets are pooled.</summary>
[HarmonyPatch(typeof(UIQualityTooltipWidget))]
internal static class UIQualityTooltipWidgetPatch
{
    [HarmonyPatch(nameof(UIQualityTooltipWidget.Draw), typeof(UIQualityTooltipWidgetData))]
    [HarmonyPostfix]
    private static void DrawPostFix(UIQualityTooltipWidget __instance, UIQualityTooltipWidgetData data) =>
        TimbnSafe.Run(() => TimbnLabel.ShowIcon(__instance, data), $"{nameof(UIQualityTooltipWidgetPatch)}|The icon slot of a label");
}
