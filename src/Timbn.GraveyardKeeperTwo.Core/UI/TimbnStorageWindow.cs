using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnStorageWindow
{
    private const string _chestId = "chest_home";

    private static readonly AccessTools.FieldRef<WgoData, Inventory>? _inventory =
        TimbnReflection.TryField<WgoData, Inventory>("inventory");

    internal static IDisposable Open(IDictionary<string, int> contents, string titleKey, int slots, IReadOnlyCollection<string> itemIds, ManualLogSource logger)
    {
        if (!TimbnGame.IsInGame || _inventory is null || MainGame.PlayerController == null)
            return TimbnUndo.None;

        WhiteListItemFilter? filter = null;
        if (itemIds.Count > 0)
        {
            filter = new WhiteListItemFilter();
            filter.itemsIds.AddRange(itemIds);
        }

        var inventory = Inventory.Create(slots, whiteList: filter, customViewId: titleKey);
        Dictionary<string, int> leftOver = [];
        foreach (var (id, count) in contents.ToList())
        {
            if (count <= 0 || GameBalance.Me.GetDataOrNull<ItemDef>(id) is null)
                continue;

            var fits = Math.Min(count, inventory.Data.CanAddItemCountToInventory(new Item(id, count), count));
            if (fits > 0)
                inventory.AddItemToInventory(new Item(id, fits));

            if (count > fits)
                leftOver[id] = count - fits;
        }

        var chest = new WgoData(_chestId, MainGame.PlayerController.MovablePosition, MainGame.PlayerData.currentGameSceneId);
        _inventory(chest) = inventory;

        void WriteBack(List<Item>? _ = null)
        {
            contents.Clear();
            foreach (var item in inventory.Data.Inventory)
            {
                if (item is null || item.IsEmpty)
                    continue;

                contents[item.id] = (contents.TryGetValue(item.id, out var have) ? have : 0) + item.Count;
            }

            foreach (var (id, count) in leftOver)
                contents[id] = (contents.TryGetValue(id, out var have) ? have : 0) + count;
        }

        var open = true;
        void Closed()
        {
            if (!open)
                return;

            open = false;
            WriteBack();
            inventory.OnItemsAdd -= WriteBack;
            inventory.OnItemsRemove -= WriteBack;
        }

        inventory.OnItemsAdd += WriteBack;
        inventory.OnItemsRemove += WriteBack;
        var window = LazyUI.GetWindow<UIChestWindow>();
        window.Open(new UIBaseChestWindowData(MainGame.PlayerData.inventory, null, chest), _ => Closed());
        logger.LogDebug($"{nameof(TimbnPluginUI)}.{nameof(TimbnPluginUI.OpenStorage)}|Opened {titleKey} with {string.Join(", ", contents.Select(pair => $"{pair.Value} {pair.Key}"))}.");
        return new TimbnUndo(() =>
        {
            var wasOpen = open;
            Closed();
            if (wasOpen && window != null && window.IsShown)
                window.CloseWithoutCallback();
        });
    }
}
