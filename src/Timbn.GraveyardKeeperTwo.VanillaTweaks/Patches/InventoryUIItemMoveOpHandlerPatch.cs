using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.SortIntoBags))]
[HarmonyPatch(typeof(InventoryUIItemMoveOpHandler))]
internal static class InventoryUIItemMoveOpHandlerPatch
{
    [HarmonyPatch("TryMoveItem")]
    [HarmonyPrefix]
    private static void TryMoveItemPreFix(Inventory from, Inventory to) =>
        SortIntoBags.BeginMove(from, to);

    [HarmonyPatch("TryMoveItem")]
    [HarmonyFinalizer]
    private static void TryMoveItemFinalizer() =>
        SortIntoBags.EndMove();
}
