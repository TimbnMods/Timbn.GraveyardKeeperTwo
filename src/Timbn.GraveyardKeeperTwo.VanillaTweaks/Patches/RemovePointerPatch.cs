using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(RemovePointer), "IsFromCurrentWorldZone")]
internal static class RemovePointerPatch
{
    [HarmonyPostfix]
    private static void AllowWholeBuildArea(IBuildRemovable removable, ref bool __result)
    {
        if (!__result && PluginConfig.RemoveInWholeArea.Value && NestedZoneRemoval.IsInsideBuildArea(removable))
            __result = true;
    }
}
