using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(Item))]
internal static class ItemPatch
{
    [HarmonyPatch(
        nameof(Item.AddItemToInventory),
        new[] { typeof(Item), typeof(List<Item>), typeof(Item), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
    [HarmonyPrefix]
    private static bool AddItemToInventoryPreFix(
        Item __instance,
        Item sourceItem,
        ref List<Item> addedItems,
        Item? ignoredBag,
        ref bool ignoreAllBags,
        ref bool __result,
        out List<Item>? __state)
    {
        __state = null;
        if (SortIntoBags.TreatAsPickup(__instance))
            ignoreAllBags = false;

        if (!SortIntoBags.TryFill(__instance, sourceItem, ignoredBag, ignoreAllBags, out var bagged))
            return true;

        if (sourceItem.Count > 0 && sourceItem.Definition.stackCount > 1)
        {
            __state = bagged;
            return true;
        }

        addedItems = bagged;
        __result = true;
        return false;
    }

    [HarmonyPatch(
        nameof(Item.AddItemToInventory),
        new[] { typeof(Item), typeof(List<Item>), typeof(Item), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
    [HarmonyPostfix]
    private static void AddItemToInventoryPostFix(ref List<Item> addedItems, ref bool __result, List<Item>? __state)
    {
        if (__state == null)
            return;

        addedItems.InsertRange(0, __state);
        __result = true;
    }
}
