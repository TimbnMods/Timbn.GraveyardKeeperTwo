using System;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(UIInfoWidget), nameof(UIInfoWidget.UpdateDescription))]
internal static class UIInfoWidgetPatch
{
    private static readonly Action<UIInfoWidget> _unsubscribe = AccessTools.MethodDelegate<Action<UIInfoWidget>>(
        AccessTools.Method(typeof(UIInfoWidget), "UnsubscribeFromWgoEvents"));

    [HarmonyPrefix]
    private static bool SkipDestroyed(UIInfoWidget __instance)
    {
        if (__instance)
            return true;

        _unsubscribe(__instance!);
        Plugin.Logger.LogInfo("Dropped a destroyed info widget that was still listening to a workstation's inventory.");
        return false;
    }
}
