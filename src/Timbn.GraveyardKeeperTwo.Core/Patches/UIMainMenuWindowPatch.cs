namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(UIMainMenuWindow))]
internal static class UIMainMenuWindowPatch
{
    [HarmonyPatch(nameof(UIMainMenuWindow.Init))]
    [HarmonyPostfix]
    private static void InitPostFix(UIMainMenuWindow __instance) =>
        TimbnSafe.Run(() => { if (TimbnModsWindowHost.ShowModSettings.Value) TimbnModSettingsButton.Create(__instance); }, TimbnModsWindowHost.Log, $"{nameof(UIMainMenuWindowPatch)}|Adding the Mod Settings button");

    // Runs last so buttons other mods add in their own Open postfix are already in place.
    [HarmonyPatch(nameof(UIMainMenuWindow.Open))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void OpenPostFix(UIMainMenuWindow __instance) =>
        TimbnSafe.Run(() =>
        {
            TimbnModSettingsButton.MoveUnderSettings(__instance);
            TimbnModSettingsButton.Relayout(__instance);
        }, TimbnModsWindowHost.Log, $"{nameof(UIMainMenuWindowPatch)}|Laying out the main menu buttons");
}
