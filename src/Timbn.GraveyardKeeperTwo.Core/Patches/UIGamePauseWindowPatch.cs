namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(UIGamePauseWindow))]
internal static class UIGamePauseWindowPatch
{
    [HarmonyPatch(nameof(UIGamePauseWindow.Init))]
    [HarmonyPostfix]
    private static void InitPostFix(UIGamePauseWindow __instance) =>
        TimbnSafe.Run(() => { if (TimbnModsWindowHost.ShowModSettings.Value) TimbnModSettingsButton.CreateInPause(__instance); }, TimbnModsWindowHost.Log, $"{nameof(UIGamePauseWindowPatch)}|Adding the Mods button");

    // Runs last so buttons other mods add in their own Open postfix are already in place.
    [HarmonyPatch(nameof(UIGamePauseWindow.Open))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void OpenPostFix(UIGamePauseWindow __instance) =>
        TimbnSafe.Run(() => TimbnModSettingsButton.MoveUnderSettings(__instance), TimbnModsWindowHost.Log, $"{nameof(UIGamePauseWindowPatch)}|Ordering the Mod Settings button");
}
