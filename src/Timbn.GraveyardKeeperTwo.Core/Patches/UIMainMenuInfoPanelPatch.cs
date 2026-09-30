namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(UIMainMenuInfoPanel))]
internal static class UIMainMenuInfoPanelPatch
{
    [HarmonyPatch("SetMenuOnlyLabelsVisible")]
    [HarmonyPostfix]
    private static void SetMenuOnlyLabelsVisiblePostFix() =>
        TimbnMainMenu.DrawLines();
}
