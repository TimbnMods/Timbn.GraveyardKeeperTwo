namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class BaitInBags
{
    public static void AddBaggedBait(List<Item> baits)
    {
        if (!PluginConfig.BaitInBags.Value)
            return;

        var inventory = MainGame.PlayerData?.Inventory?.Data;
        if (inventory == null)
            return;

        foreach (var bag in inventory.Inventory)
        {
            if (!bag.IsBag)
                continue;

            foreach (var item in bag.Inventory)
            {
                if (item.Definition.type == ItemType.Bait)
                    baits.Add(item);
            }
        }
    }
}
