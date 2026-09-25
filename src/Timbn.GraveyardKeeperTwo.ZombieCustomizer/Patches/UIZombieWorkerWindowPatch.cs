namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer.Patches;

[HarmonyPatch(typeof(UIZombieWorkerWindow))]
internal static class UIZombieWorkerWindowPatch
{
    [HarmonyPatch(nameof(UIZombieWorkerWindow.Open))]
    [HarmonyPostfix]
    private static void OpenPostFix(UIZombieWorkerWindow __instance, UIZombieWorkerWindowData data) =>
        RestyleButton.Attach(__instance, data);
}
