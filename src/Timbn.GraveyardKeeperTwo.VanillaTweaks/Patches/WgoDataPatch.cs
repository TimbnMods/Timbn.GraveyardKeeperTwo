using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(WgoData))]
internal static class WgoDataPatch
{
    [HarmonyPatch(nameof(WgoData.GetDropPos))]
    [HarmonyPostfix]
    private static void GetDropPosPostFix(WgoData __instance, ref Vector3 __result)
    {
        if (UnreachableDismantle.Active is { } dismantle && dismantle.TryGetDropPosition(__instance, out var position))
            __result = position;
    }
}
