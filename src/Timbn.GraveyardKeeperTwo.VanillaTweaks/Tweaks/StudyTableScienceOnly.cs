namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class StudyTableScienceOnly
{
    private const string _tableId = "survey_wgo";
    private const string _scienceId = "science";

    private readonly List<Item> _filteredInventories = [];
    private WGODef? _filteredDefinition;

    public void Apply()
    {
        FilterDefinition();
        foreach (var table in MainGame.Instance.GameSave.WorldData.GetWgoDataList(_tableId))
        {
            var inventory = table.Inventory;
            if (inventory == null)
                continue;

            FilterInventory(inventory);
            EvictStrays(table, inventory);
        }
    }

    public void Revert()
    {
        foreach (var data in _filteredInventories)
            data.RemoveProperty<WhiteListFilterSerializedItemProperty>();
        _filteredInventories.Clear();

        _filteredDefinition?.inventoryWhiteList.itemsIds.Remove(_scienceId);
        _filteredDefinition = null;
    }

    public static void RemoveSavedFilters()
    {
        var removed = 0;
        foreach (var table in MainGame.Instance.GameSave.WorldData.GetWgoDataList(_tableId))
        {
            var data = table.Inventory?.Data;
            if (data == null || !data.TryGetProperty<WhiteListFilterSerializedItemProperty>(out var property))
                continue;

            var whiteList = property.WhiteList;
            if (whiteList.groupsIds.Count > 0 || whiteList.itemsIds.Count != 1 || whiteList.itemsIds[0] != _scienceId)
                continue;

            data.RemoveProperty<WhiteListFilterSerializedItemProperty>();
            removed++;
        }

        if (removed > 0)
            Plugin.Logger.LogInfo($"Removed the science only filter from {removed} study table(s).");
    }

    private void FilterDefinition()
    {
        var definition = GameBalance.Me.GetDataOrNull<WGODef>(_tableId);
        if (definition == null || _filteredDefinition == definition || !definition.inventoryWhiteList.IsEmpty)
            return;

        definition.inventoryWhiteList.AddElement(_scienceId, ItemFilter.ItemFilterElementType.Item);
        _filteredDefinition = definition;
    }

    private void FilterInventory(Inventory inventory)
    {
        if (inventory.Data.HasProperty<WhiteListFilterSerializedItemProperty>())
            return;

        var whiteList = new WhiteListItemFilter();
        whiteList.AddElement(_scienceId, ItemFilter.ItemFilterElementType.Item);
        inventory.Data.AddProperty(new WhiteListFilterSerializedItemProperty(whiteList));
        _filteredInventories.Add(inventory.Data);
    }

    private static void EvictStrays(WgoData table, Inventory inventory)
    {
        var strays = inventory.Data.Inventory.Where(i => !i.IsEmpty && i.id != _scienceId).ToList();
        if (strays.Count == 0)
            return;

        var player = new MultiInventory(MainGame.PlayerData);
        foreach (var stray in strays)
        {
            if (!inventory.RemoveItemFromInventoryByUID(stray))
                continue;

            var moved = player.AddItem(stray);
            if (moved < stray.Count)
            {
                var rest = Item.Copy(stray);
                rest.Count = stray.Count - moved;
                MainGame.Instance.dropSystem.DropItemAsDropView(rest, table.WorldId, table.Position);
            }

            Plugin.Logger.LogInfo($"Moved {stray.Count} {stray.id} out of a study table, {moved} into your inventory "
                + $"and {stray.Count - moved} dropped beside the table.");
        }
    }
}
