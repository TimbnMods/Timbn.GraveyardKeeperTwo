using System.Collections.Generic;
using System.Linq;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class SpecialStorageFilters
{
    private static readonly Dictionary<string, string[]> _missingWhiteLists = new()
    {
        ["well_garden_1"] = ["water"],
        ["well_garden_2"] = ["water"],
    };

    private readonly Dictionary<WGODef, WhiteListItemFilter> _originalWhiteLists = [];
    private readonly List<Item> _addedWhiteLists = [];
    private readonly List<Item> _addedBlackLists = [];

    public void Apply()
    {
        AddMissingWhiteLists();

        var filtered = 0;
        foreach (var definition in GameBalance.Me.wgoDefs)
        {
            if (definition == null || !definition.OpenInMultiInventory || definition.inventorySize <= 0)
                continue;

            if (IsEmpty(definition.inventoryWhiteList) && IsEmpty(definition.inventoryBlackList))
                continue;

            foreach (var storage in MainGame.Instance.GameSave.WorldData.GetWgoDataList(definition.id))
            {
                var inventory = storage.Inventory;
                if (inventory?.Data == null)
                    continue;

                if (FilterInventory(definition, inventory.Data))
                    filtered++;

                DropStrays(storage, inventory);
            }
        }

        if (filtered > 0)
            Plugin.Logger.LogInfo($"Added the missing item filter to {filtered} storage object(s).");
    }

    public void Revert()
    {
        foreach (var data in _addedWhiteLists)
            data.RemoveProperty<WhiteListFilterSerializedItemProperty>();
        _addedWhiteLists.Clear();

        foreach (var data in _addedBlackLists)
            data.RemoveProperty<BlackListFilterSerializedItemProperty>();
        _addedBlackLists.Clear();

        foreach (var (definition, original) in _originalWhiteLists)
            definition.inventoryWhiteList = original;
        _originalWhiteLists.Clear();
    }

    private void AddMissingWhiteLists()
    {
        foreach (var (id, itemIds) in _missingWhiteLists)
        {
            var definition = GameBalance.Me.GetData<WGODef>(id);
            if (definition == null || _originalWhiteLists.ContainsKey(definition) || !IsEmpty(definition.inventoryWhiteList))
                continue;

            var whiteList = new WhiteListItemFilter();
            foreach (var itemId in itemIds)
                whiteList.AddElement(itemId, ItemFilter.ItemFilterElementType.Item);

            _originalWhiteLists[definition] = definition.inventoryWhiteList;
            definition.inventoryWhiteList = whiteList;
        }
    }

    private bool FilterInventory(WGODef definition, Item data)
    {
        var added = false;
        if (!IsEmpty(definition.inventoryWhiteList) && !data.HasProperty<WhiteListFilterSerializedItemProperty>())
        {
            data.AddProperty(new WhiteListFilterSerializedItemProperty(Copy(new WhiteListItemFilter(), definition.inventoryWhiteList)));
            _addedWhiteLists.Add(data);
            added = true;
        }

        if (!IsEmpty(definition.inventoryBlackList) && !data.HasProperty<BlackListFilterSerializedItemProperty>())
        {
            data.AddProperty(new BlackListFilterSerializedItemProperty(Copy(new BlackListItemFilter(), definition.inventoryBlackList)));
            _addedBlackLists.Add(data);
            added = true;
        }

        return added;
    }

    private static void DropStrays(WgoData storage, Inventory inventory)
    {
        var strays = inventory.Data.Inventory
            .Where(i => i != null && !i.IsEmpty && i.Definition != null && !Allows(inventory.Data, i.Definition))
            .ToList();

        foreach (var stray in strays)
        {
            if (!inventory.RemoveItemFromInventoryByUID(stray))
                continue;

            MainGame.Instance.dropSystem.DropItem(stray, storage.WorldId, storage.GetDropPos(stray));
            Plugin.Logger.LogInfo($"Dropped {stray.Count} {stray.id} out of {storage.id} beside it.");
        }
    }

    private static bool Allows(Item data, ItemDef definition)
    {
        if (data.TryGetProperty<WhiteListFilterSerializedItemProperty>(out var whiteList) && !whiteList.WhiteList.Contains(definition))
            return false;

        return !data.TryGetProperty<BlackListFilterSerializedItemProperty>(out var blackList) || !blackList.BlackList.Contains(definition);
    }

    private static T Copy<T>(T target, ItemFilter source)
        where T : ItemFilter
    {
        target.itemsIds.AddRange(source.itemsIds);
        target.groupsIds.AddRange(source.groupsIds);
        return target;
    }

    private static bool IsEmpty(ItemFilter? filter) => filter == null || filter.IsEmpty;
}
