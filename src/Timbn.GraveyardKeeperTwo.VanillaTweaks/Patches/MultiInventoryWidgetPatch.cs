using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(MultiInventoryWidget))]
internal static class MultiInventoryWidgetPatch
{
    [HarmonyPatch("SetDefaultStateForWidgetAndInactiveForOthers")]
    [HarmonyPostfix]
    private static void SetDefaultStateForWidgetAndInactiveForOthersPostFix(MultiInventoryWidget __instance) =>
        BagFilters.RefreshOtherChestSide(__instance);
}
