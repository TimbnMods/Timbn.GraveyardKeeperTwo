namespace Timbn.GraveyardKeeperTwo.ChestLabels.Patches;

// An empty icon name would print as a raw sprite tag, so the icon slot is hidden for a label with no icon. The widgets are pooled, so it is shown again for every other quality label.
[HarmonyPatch(typeof(UIQualityTooltipWidget))]
internal static class UIQualityTooltipWidgetPatch
{
    [HarmonyPatch(nameof(UIQualityTooltipWidget.Draw), typeof(UIQualityTooltipWidgetData))]
    [HarmonyPostfix]
    private static void DrawPostFix(UIQualityTooltipWidgetData data, Component ___iconLabel) =>
        ___iconLabel.gameObject.SetActive(!string.IsNullOrEmpty(data.IconName));
}
