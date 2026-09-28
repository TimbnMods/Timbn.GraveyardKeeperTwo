namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class SortIntoBags
{
    private const string _universalBagGroup = "u_bag";

    private static bool _movingFromChest;

    public static void BeginMove(Inventory from, Inventory to) =>
        _movingFromChest = PluginConfig.SortIntoBags.Value
            && IsPlayerInventory(to.Data)
            && !IsPlayerInventory(from.Data)
            && from.Data is { IsBag: false }
            && from != MainGame.PlayerData.toolBeltInventory;

    public static void EndMove() => _movingFromChest = false;

    public static bool TreatAsPickup(Item inventory) => _movingFromChest && IsPlayerInventory(inventory);

    public static bool TryFill(Item inventory, Item sourceItem, Item? ignoredBag, bool ignoreAllBags, out List<Item> addedItems)
    {
        addedItems = [];
        if (!PluginConfig.SortIntoBags.Value
            || ignoreAllBags
            || sourceItem == null
            || sourceItem.IsEmpty
            || sourceItem.Count <= 0
            || sourceItem.IsBag
            || !IsPlayerInventory(inventory))
        {
            return false;
        }

        var bags = inventory.Inventory
            .Where(bag => IsMatchingBag(bag, sourceItem, ignoredBag))
            .OrderByDescending(bag => bag.GetTotalCountInInventory(sourceItem.id) > 0)
            .ToList();

        foreach (var bag in bags)
        {
            if (bag.AddItemToInventory(sourceItem, out var bagAdded, null, ignoreAllBags: true))
                addedItems.AddRange(bagAdded);

            if (sourceItem.Count <= 0 || sourceItem.Definition.stackCount == 1 && addedItems.Count > 0)
                break;
        }

        return addedItems.Count > 0;
    }

    private static bool IsMatchingBag(Item bag, Item sourceItem, Item? ignoredBag) =>
        bag.IsBag
        && !bag.Definition.bagItemGroups.Contains(_universalBagGroup)
        && sourceItem.Definition.CanBeInsertedInBag(bag.Definition)
        && (ignoredBag == null || bag.UniqueId.Guid != ignoredBag.UniqueId.Guid);

    private static bool IsPlayerInventory(Item? inventory) =>
        inventory != null && inventory == MainGame.PlayerData?.Inventory?.Data;
}
