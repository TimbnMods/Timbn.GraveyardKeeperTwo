namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(UIMainMenuInfoPanel), "SetMenuOnlyLabelsVisible")]
internal static class UIMainMenuInfoPanelPatch
{
    private static void Postfix() => TimbnMainMenu.DrawLines();
}
