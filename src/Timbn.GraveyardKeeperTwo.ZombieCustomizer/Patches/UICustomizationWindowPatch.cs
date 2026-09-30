namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer.Patches;

[HarmonyPatch(typeof(UICustomizationWindow))]
internal static class UICustomizationWindowPatch
{
    [HarmonyPatch(nameof(UICustomizationWindow.Open))]
    [HarmonyPostfix]
    private static void OpenPostFix(UICustomizationWindow __instance) =>
        Plugin.Instance?.Customization.OnWindowOpened(__instance);

    [HarmonyPatch("OnApplyPressed")]
    [HarmonyPrefix]
    private static bool OnApplyPressedPreFix(UICustomizationWindow __instance) =>
        Plugin.Instance?.Customization.TryApply(__instance) != true;

    [HarmonyPatch(nameof(UICustomizationWindow.Close))]
    [HarmonyPostfix]
    private static void ClosePostFix() =>
        Plugin.Instance?.Customization.OnWindowClosed();
}
