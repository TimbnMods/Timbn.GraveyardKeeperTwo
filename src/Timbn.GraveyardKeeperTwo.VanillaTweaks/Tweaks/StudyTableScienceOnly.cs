using System.Collections.Generic;
using System.Linq;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class StudyTableScienceOnly
{
    private const string _tableId = "survey_wgo";
    private const string _scienceId = "science";

    private readonly List<Item> _filteredInventories = [];

    public void Apply()
    {
        var filtered = 0;
        foreach (var table in MainGame.Instance.GameSave.WorldData.GetWgoDataList(_tableId))
        {
            var inventory = table.Inventory;
            if (inventory?.Data == null)
                continue;

            if (FilterInventory(inventory))
                filtered++;

            DropStrays(table, inventory);
        }

        if (filtered > 0)
            Plugin.Logger.LogInfo($"Made {filtered} study table(s) science only.");
    }

    public void Revert()
    {
        foreach (var data in _filteredInventories)
            data.RemoveProperty<WhiteListFilterSerializedItemProperty>();
        _filteredInventories.Clear();
    }

    private bool FilterInventory(Inventory inventory)
    {
        if (inventory.Data.HasProperty<WhiteListFilterSerializedItemProperty>())
            return false;

        var whiteList = new WhiteListItemFilter();
        whiteList.AddElement(_scienceId, ItemFilter.ItemFilterElementType.Item);
        inventory.Data.AddProperty(new WhiteListFilterSerializedItemProperty(whiteList));
        _filteredInventories.Add(inventory.Data);
        return true;
    }

    private static void DropStrays(WgoData table, Inventory inventory)
    {
        var strays = inventory.Data.Inventory.Where(i => i != null && !i.IsEmpty && i.id != _scienceId).ToList();
        foreach (var stray in strays)
        {
            if (!inventory.RemoveItemFromInventoryByUID(stray))
                continue;

            MainGame.Instance.dropSystem.DropItem(stray, table.WorldId, table.GetDropPos(stray));
            Plugin.Logger.LogInfo($"Dropped {stray.Count} {stray.id} out of a study table beside it.");
        }
    }
}
