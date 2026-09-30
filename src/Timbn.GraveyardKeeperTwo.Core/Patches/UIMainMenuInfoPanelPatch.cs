namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the main menu's credits panel that draws the plugin lines whenever it shows.</summary>
[HarmonyPatch(typeof(UIMainMenuInfoPanel))]
internal static class UIMainMenuInfoPanelPatch
{
    [HarmonyPatch("SetMenuOnlyLabelsVisible")]
    [HarmonyPostfix]
    private static void SetMenuOnlyLabelsVisiblePostFix() =>
        TimbnMainMenu.DrawLines();
}
