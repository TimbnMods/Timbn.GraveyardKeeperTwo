using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(Item))]
internal static class ItemPatch
{
    [HarmonyPatch(nameof(Item.CanAddItemCountToInventory), typeof(ItemDef), typeof(int), typeof(bool), typeof(Item), typeof(bool))]
    [HarmonyPrefix]
    private static bool CanAddItemCountToInventoryPreFix(Item __instance, ItemDef itemDef, ref int __result)
    {
        if (!BagFilters.Blocks(__instance, itemDef))
            return true;

        __result = 0;
        return false;
    }
}
