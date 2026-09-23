using System.Collections.Generic;
using System.Linq;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class OvenIngredientSlots
{
    private static readonly string[] _ovenIds = ["kitchen_oven", "kitchen_oven_t2"];

    private readonly Dictionary<WGODef, int> _originalSizes = [];

    public void Apply()
    {
        foreach (var id in _ovenIds)
        {
            var definition = GameBalance.Me.GetDataOrNull<WGODef>(id);
            if (definition == null)
                continue;

            var needed = MostIngredients(id);
            if (definition.craftInventorySize < needed)
            {
                if (!_originalSizes.ContainsKey(definition))
                    _originalSizes[definition] = definition.craftInventorySize;
                definition.craftInventorySize = needed;
            }

            var widened = 0;
            foreach (var oven in MainGame.Instance.GameSave.WorldData.GetWgoDataList(id))
            {
                var slots = oven.CraftableObjectCraftInventory?.Data;
                if (slots == null || slots.InventorySize >= needed)
                    continue;

                slots.InventorySize = needed;
                widened++;
            }

            if (widened > 0)
                Plugin.Logger.LogInfo($"Gave {widened} {id} room for {needed} ingredients.");
        }
    }

    public void Revert()
    {
        foreach (var pair in _originalSizes)
            pair.Key.craftInventorySize = pair.Value;
        _originalSizes.Clear();
    }

    private static int MostIngredients(string stationId) =>
        GameBalance.Me.craftDefs
            .Where(craft => craft.craftsIn.Contains(stationId))
            .Select(craft => craft.needItems
                .Where(need => GameBalance.Me.GetDataOrNull<ItemDef>(need.id)?.isFuel != true)
                .Select(need => need.id)
                .Distinct()
                .Count())
            .DefaultIfEmpty(0)
            .Max();
}
