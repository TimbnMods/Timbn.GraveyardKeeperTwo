using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.RemoveInWholeArea))]
[HarmonyPatch(typeof(RemovePointer))]
internal static class RemovePointerPatch
{
    [HarmonyPatch("IsFromCurrentWorldZone")]
    [HarmonyPostfix]
    private static void IsFromCurrentWorldZonePostFix(IBuildRemovable removable, ref bool __result)
    {
        if (!__result && PluginConfig.RemoveInWholeArea.Value && NestedZoneRemoval.IsInsideBuildArea(removable))
            __result = true;
    }
}
