namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.DismantleUnreachable))]
[HarmonyPatch(typeof(WgoData))]
internal static class WgoDataPatch
{
    [HarmonyPatch(nameof(WgoData.GetDropPos))]
    [HarmonyPostfix]
    private static void GetDropPosPostFix(WgoData __instance, ref Vector3 __result)
    {
        if (Plugin.Instance?.Dismantle.TryGetDropPosition(__instance, out var position) == true)
            __result = position;
    }
}
