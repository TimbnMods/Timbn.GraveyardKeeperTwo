namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(DeliveryOrder), nameof(DeliveryOrder.CanOrderBeExecuted))]
internal static class DeliveryOrderPatch
{
    [HarmonyPostfix]
    private static void RefuseWithoutRoom(DeliveryOrder __instance, ref bool __result, ref string reasonIfNot)
    {
        if (!__result || !PluginConfig.OvenNoLostIngredients.Value)
            return;

        var slots = __instance.ZombieWgoData?.CraftableObjectCraftInventory;
        if (slots == null || slots.CanAddItemToInventory(new Item(__instance.Item.id, __instance.Item.Count)))
            return;

        __result = false;
        reasonIfNot = "inventory_is_full";
    }
}
