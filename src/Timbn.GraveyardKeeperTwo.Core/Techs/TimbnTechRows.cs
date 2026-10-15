namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Builds the reward icons the tech tree and a tech's popup draw. The game builds them once from the tech's reward
/// lists, so this rebuilds them after a mod changes a list or a tech names its own rows, and skips a reward the
/// balance doesn't have yet instead of failing on it the way the game's own pass does.
/// </summary>
internal static class TimbnTechRows
{
    internal static void Refresh(TechDef tech)
    {
        if (!TimbnTechGrants.TreeBuilt || GameBalance.Me is not { } balance)
            return;

        tech.linkedEntityWidgetDatas.Clear();
        var rows = TimbnTechs.RowsOf(tech.id) is { } named ? Named(balance, named) : Listed(balance, tech);
        foreach (var row in rows)
        {
            row.NotShowStudyWidgetInItemTooltips = true;
            // An item row keeps its own "crafted at" line, where a craft row would show its recipe instead.
            row.ShowCraftedAtFromCraftDefInsteadOfItem = row.LinkedEntityType != LinkedEntityType.ItemDef;
            tech.linkedEntityWidgetDatas.Add(row);
        }
    }

    internal static void RefreshAll()
    {
        if (GameBalance.Me is not { } balance)
            return;

        foreach (var tech in balance.techDefs.Where(tech => TimbnTechs.RowsOf(tech.id) != null))
            Refresh(tech);
    }

    private static IEnumerable<LinkedEntityWidgetData> Named(GameBalance balance, IReadOnlyList<string> ids)
    {
        foreach (var id in ids)
        {
            var building = balance.GetDataOrNull<BuildingDef>(id)
                ?? balance.GetDataOrNull<BuildingDef>(TimbnStationRules.BuildingId(TimbnStationRules.SiteId(id)));
            if (building != null)
                yield return new LinkedEntityWidgetData(building, null);
            else if (balance.GetDataOrNull<ItemDef>(id) is { } item)
                yield return new LinkedEntityWidgetData(item, null);
            else if (balance.GetDataOrNull<CraftDef>(id) is { } craft)
                yield return new LinkedEntityWidgetData(craft, null);
        }
    }

    // The same rows the game's own pass makes, in the same order.
    private static IEnumerable<LinkedEntityWidgetData> Listed(GameBalance balance, TechDef tech)
    {
        foreach (var id in tech.craftsAfterUnlock)
        {
            if (balance.GetDataOrNull<CraftDef>(id) is { } craft)
                yield return new LinkedEntityWidgetData(craft, null);
        }

        foreach (var id in tech.alchemyFormulasAfterUnlock)
        {
            if (balance.GetDataOrNull<AlchemyFormulaDef>(id) is { } formula)
                yield return new LinkedEntityWidgetData(formula, null);
        }

        foreach (var id in tech.buildingsAfterUnlock)
        {
            if (balance.GetDataOrNull<BuildingDef>(id ?? "") is { } building)
                yield return new LinkedEntityWidgetData(building, null);
        }

        foreach (var id in tech.townBuildingsAfterUnlock)
        {
            if (balance.GetDataOrNull<TownBuildingDef>(id) is { } townBuilding)
                yield return new LinkedEntityWidgetData(townBuilding, null);
        }

        foreach (var id in tech.perksAfterUnlock)
        {
            if (balance.GetDataOrNull<PerkDef>(id) is { isHidden: false } perk)
                yield return new LinkedEntityWidgetData(perk, null);
        }
    }
}
