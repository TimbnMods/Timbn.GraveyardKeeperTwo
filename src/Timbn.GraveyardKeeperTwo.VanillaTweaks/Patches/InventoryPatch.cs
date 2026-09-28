using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(Inventory))]
internal static class InventoryPatch
{
    [HarmonyPatch(nameof(Inventory.TakeAllItemsExistingInMeFromOtherInventory))]
    [HarmonyPrefix]
    private static void TakeAllItemsExistingInMeFromOtherInventoryPreFix(Inventory __instance, Inventory otherInventory, ref bool ignoreOtherBags)
    {
        if (MoveIdenticalFromBags.Applies(__instance, otherInventory))
            ignoreOtherBags = false;
    }

    [HarmonyPatch(nameof(Inventory.CanTakeAnyItemsExistingInMeFromOtherInventory))]
    [HarmonyPrefix]
    private static void CanTakeAnyItemsExistingInMeFromOtherInventoryPreFix(Inventory __instance, Inventory otherInventory, ref bool ignoreOtherBags)
    {
        if (MoveIdenticalFromBags.Applies(__instance, otherInventory))
            ignoreOtherBags = false;
    }
}
