namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.SortIntoBags))]
[HarmonyPatch(typeof(InventoryUIItemMoveOpHandler))]
internal static class InventoryUIItemMoveOpHandlerPatch
{
    [HarmonyPatch("TryMoveItem")]
    [HarmonyPrefix]
    private static void TryMoveItemPreFix(Inventory from, Inventory to) =>
        Plugin.Instance?.BagSorting.BeginMove(from, to);

    [HarmonyPatch("TryMoveItem")]
    [HarmonyFinalizer]
    private static void TryMoveItemFinalizer() =>
        Plugin.Instance?.BagSorting.EndMove();
}
