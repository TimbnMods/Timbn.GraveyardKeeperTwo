namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(UIVendorWindow))]
internal static class UIVendorWindowPatch
{
    [HarmonyPatch(nameof(UIVendorWindow.Open), typeof(UIVendorWindowData))]
    [HarmonyPrefix]
    private static bool OpenPreFix(UIVendorWindowData data) => TimbnVendorGates.LetOpen(data);
}
