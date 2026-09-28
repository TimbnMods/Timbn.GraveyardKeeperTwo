using System.Collections.Generic;
using System.Linq;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class BagFilters
{
    private static readonly AccessTools.FieldRef<UIBaseChestWindow, MultiInventoryWidget> _leftSide =
        AccessTools.FieldRefAccess<UIBaseChestWindow, MultiInventoryWidget>("leftMultiInventoryWidget");

    private static readonly AccessTools.FieldRef<UIBaseChestWindow, MultiInventoryWidget> _rightSide =
        AccessTools.FieldRefAccess<UIBaseChestWindow, MultiInventoryWidget>("rightMultiInventoryWidget");

    public static bool Blocks(Item? bag, ItemDef? definition) =>
        PluginConfig.BagFilters.Value
        && bag != null
        && definition != null
        && bag.IsBag
        && !definition.CanBeInsertedInBag(bag.Definition);

    public static void RefreshOtherChestSide(MultiInventoryWidget side)
    {
        if (!PluginConfig.BagFilters.Value)
            return;

        var window = side.GetComponentInParent<UIBaseChestWindow>();
        if (window == null)
            return;

        var left = _leftSide(window);
        var other = side == left ? _rightSide(window) : left;
        if (other == null)
            return;

        foreach (var inventory in other.DrawnInventories)
            inventory.UpdateItemRelatedWidgetStateForCells();
    }

    public static void EjectStrays()
    {
        if (!PluginConfig.BagFilters.Value)
            return;

        var player = MainGame.PlayerData;
        if (player?.Inventory?.Data == null)
            return;

        EjectStrays(player.Inventory, player);

        foreach (var definition in GameBalance.Me.wgoDefs)
        {
            if (definition == null || definition.inventorySize <= 0)
                continue;

            foreach (var storage in MainGame.Instance.GameSave.WorldData.GetWgoDataList(definition.id))
            {
                if (storage?.Inventory?.Data != null)
                    EjectStrays(storage.Inventory, player);
            }
        }
    }

    private static void EjectStrays(Inventory holder, PlayerData player)
    {
        foreach (var bag in holder.Data.Inventory.Where(i => i != null && i.IsBag).ToList())
        {
            List<Item> strays = [.. bag.Inventory.Where(i => i != null && !i.IsEmpty && i.Definition != null && !i.Definition.CanBeInsertedInBag(bag.Definition))];
            foreach (var stray in strays)
            {
                var item = bag.RemoveItemFromInventoryByUID(stray.UniqueId.Guid);
                if (item.IsEmpty)
                    continue;

                var count = item.Count;
                Place(item, bag, holder, player);
                Plugin.Logger.LogInfo($"Moved {count} {item.id} out of {bag.id}, which doesn't take it.");
            }
        }
    }

    private static void Place(Item item, Item bag, Inventory holder, PlayerData player)
    {
        holder.AddItemToInventory(item, out _, bag);
        if (item.Count > 0 && holder != player.Inventory)
            player.Inventory.AddItemToInventory(item, out _, bag);

        if (item.Count <= 0)
            return;

        var position = player.position.Value + new Vector3(player.Direction.x, 0f, player.Direction.y);
        MainGame.Instance.dropSystem.DropItem(item, player.currentGameSceneId, position);
        Plugin.Logger.LogInfo($"No room for {item.Count} {item.id}, dropped it at your feet.");
    }
}
