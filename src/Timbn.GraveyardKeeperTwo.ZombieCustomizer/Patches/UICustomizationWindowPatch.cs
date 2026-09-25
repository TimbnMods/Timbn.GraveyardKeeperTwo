namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer.Patches;

[HarmonyPatch(typeof(UICustomizationWindow))]
internal static class UICustomizationWindowPatch
{
    [HarmonyPatch(nameof(UICustomizationWindow.Open))]
    [HarmonyPostfix]
    private static void OpenPostFix(UICustomizationWindow __instance) => ZombieCustomization.OnWindowOpened(__instance);

    [HarmonyPatch("OnApplyPressed")]
    [HarmonyPrefix]
    private static bool OnApplyPressedPreFix(UICustomizationWindow __instance) => !ZombieCustomization.TryApply(__instance);

    [HarmonyPatch(nameof(UICustomizationWindow.Close))]
    [HarmonyPostfix]
    private static void ClosePostFix() => ZombieCustomization.OnWindowClosed();
}
