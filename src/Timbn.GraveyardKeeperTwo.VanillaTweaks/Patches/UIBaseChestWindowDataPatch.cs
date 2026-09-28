using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(UIBaseChestWindowData))]
internal static class UIBaseChestWindowDataPatch
{
    [HarmonyPatch("ItemAvailabilityConditionLeft")]
    [HarmonyPostfix]
    private static void ItemAvailabilityConditionLeftPostFix(UIBaseChestWindowData __instance, Item item, ref bool __result)
    {
        if (__result && item != null && BagFilters.Blocks(__instance.SecondMultiInventoryData?.SelectedWidgetData?.Inventory?.Data, item.Definition))
            __result = false;
    }

    [HarmonyPatch("ItemAvailabilityConditionRight")]
    [HarmonyPostfix]
    private static void ItemAvailabilityConditionRightPostFix(UIBaseChestWindowData __instance, Item item, ref bool __result)
    {
        if (__result && item != null && BagFilters.Blocks(__instance.FirstMultiInventoryData?.SelectedWidgetData?.Inventory?.Data, item.Definition))
            __result = false;
    }
}
