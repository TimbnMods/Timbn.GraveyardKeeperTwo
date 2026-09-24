using LazyBearTechnology;
using System.Collections.Generic;
using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(UIGameBindingSettingsWindow))]
internal static class UIGameBindingSettingsWindowPatch
{
    [HarmonyPatch("UpdateBinding")]
    [HarmonyPrefix]
    private static void UpdateBindingPreFix(UIGameBindingSettingsWindow __instance, out Dictionary<KeyBinding, KeyCode>? __state) =>
        __state = PluginConfig.KeepHiddenKeys.Value ? HiddenKeyBindings.Snapshot(__instance) : null;

    [HarmonyPatch("UpdateBinding")]
    [HarmonyPostfix]
    private static void UpdateBindingPostFix(Dictionary<KeyBinding, KeyCode>? __state)
    {
        if (__state != null)
            HiddenKeyBindings.Restore(__state);
    }

    [HarmonyPatch(nameof(UIGameBindingSettingsWindow.Redraw))]
    [HarmonyPostfix]
    private static void RedrawPostFix(UIGameBindingSettingsWindow __instance)
    {
        if (PluginConfig.KeepHiddenKeys.Value)
            HiddenKeyBindings.RepairCleared(__instance);
    }
}
