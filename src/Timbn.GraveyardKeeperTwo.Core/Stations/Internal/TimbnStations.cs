namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Turns a station or extension builder into its balance definitions, models and text through Core's balance entries,
/// so each goes in on every balance load and comes out with the owning plugin.
/// </summary>
internal static class TimbnStations
{
    private static readonly AccessTools.FieldRef<BuildingDef, string> _buildResultIcon = AccessTools.FieldRefAccess<BuildingDef, string>("buildResultIcon");

    internal static IDisposable Add(TimbnStationBuilder station)
    {
        if (station.StationModel is null)
            throw new InvalidOperationException($"Station {station.Id} needs a model. Call Model on the builder.");

        var sides = station.Sides;
        if (TimbnLookRules.FindProblem([.. station.Overlays.Select(overlay => (overlay.Facing, overlay.ReplacesBase))],
            [.. sides.Where(side => side.MirrorOf == null).Select(side => side.Facing)],
            sides.Where(side => side.MirrorOf != null).ToDictionary(side => side.Facing, side => side.MirrorOf!.Value)) is { } problem)
            throw new InvalidOperationException($"Station {station.Id}. {problem}");

        if (TimbnSlotRules.FindProblem(station.Slots, sides) is { } slotProblem)
            throw new InvalidOperationException($"Station {station.Id}. {slotProblem}");

        if (station.InSlotId is { } slotId && sides.Any(side => side.Site != null))
            throw new InvalidOperationException($"Station {station.Id} builds into slot {slotId}, where it goes down finished with no construction site. Leave out its site.");

        // The game's slot pieces all go down finished. A site's handover to the station runs the game's removal, which takes it off the host's slot list.
        var instant = station.SkipsConstruction || station.InSlotId != null;
        var id = station.Id;
        var siteId = TimbnStationRules.SiteId(id);
        var buildingId = TimbnStationRules.BuildingId(siteId);
        var cost = station.CostItems.ToList();
        var refund = station.RefundItems is { } given ? TimbnStationRules.Refund(given) : TimbnStationRules.HalfRefund(cost);
        string Family(GameBalance balance) => TimbnStationRules.BuildFamily(station.TemplateId, building => HasBuilding(balance, building));

        List<IDisposable> undo = [];
        AddTexts(undo, TimbnStationRules.StationTexts(id, station.Name, station.Description));
        undo.Add(TimbnModels.AddStation(id, sides, station.DockGap, station.HasZombieSpot, station.Overlays, station.Slots));
        if (station.HasZombieSpot)
            undo.Add(TimbnZombieSpots.Register(id, station.ZombieTakesOrders));

        if (!instant)
            undo.Add(TimbnModels.AddStation(siteId, SiteSides(id, sides, undo), TimbnPartShape.SiteDockDistance(station.DockGap)));

        undo.Add(TimbnBalance.AddCopy<WGODef>(_ => station.TemplateId, id, (_, def) =>
        {
            if (station.IconId.Length > 0)
                def.craftIconId = station.IconId;

            if (station.ClearsFuel)
            {
                def.fuelItemId = "";
                def.fuelItemId2 = "";
            }

            if (station.ExtensionIds is { } extensions)
                def.attachedWorkbenchExtensionIds = [.. extensions];

            if (station.TalentId is { } talent)
                def.talent = talent;

            if (station.AllowsZombie is { } takesZombie)
                def.canInsertZombie = takesZombie;

            if (station.ToolType is { } tool)
                def.toolAction = new ToolAction { actionableTool = tool };

            if (station.CraftSlotCount is { } craftSlots)
                def.craftInventorySize = craftSlots;

            if (station.StorageSlotCount is { } storageSlots)
                def.inventorySize = storageSlots;

            if (station.IsAutomatic is { } automatic)
            {
                def.isAutoCrafter = automatic;
                if (station.AutoTickDuration is { } tickDuration)
                    def.autocraftTickDuration = TimbnExpression.Number(tickDuration);
            }

            if (station.ActionSound is { } sound)
                def.sfxOnActionTick = sound;

            if (station.HoldsZombieItem)
                HoldZombie(def);

            if (station.HasZombieSpot && !TimbnZombieSpotRules.HandlerSeatsAtPoint(def.interactionType))
                TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnStations)}|{id} has a zombie spot but is based on {station.TemplateId}, whose handler never seats a zombie there. Base it on zombie_supplier_station.");

            station.EditStation?.Invoke(def);
        }));
        if (!instant)
        {
            undo.Add(TimbnBalance.AddCopy<WGODef>(balance => TimbnStationRules.SiteId(Family(balance)), siteId,
                (_, site) =>
            {
                site.replaceToWgoOnDie = TimbnExpression.PureText(id);
                // The site is paid out as tech points when it turns into the station, so a copy of a site that pays must not.
                site.techRed = site.techGreen = site.techBlue = 0;
            }));
        }

        // With no construction the build desk entry places the finished station itself.
        undo.Add(AddBuilding(balance => TimbnStationRules.BuildingId(TimbnStationRules.SiteId(Family(balance))), buildingId, instant ? id : siteId, id,
            station.BuildDeskId, station.AreaId, cost, station.IconId, station.TechId, station.TabId, station.BuildLimit, station.InSlotId));
        undo.Add(AddRemoval(balance => TimbnStationRules.RemovalId(Family(balance)), TimbnStationRules.RemovalId(id), id, station.BuildDeskId, refund));
        if (!instant)
        {
            undo.Add(AddRemoval(balance => TimbnStationRules.RemovalId(TimbnStationRules.SiteId(Family(balance))), TimbnStationRules.RemovalId(siteId), siteId,
                station.BuildDeskId, cost));
        }
        return TimbnUndo.All(undo);
    }

    internal static IDisposable AddExtension(TimbnExtensionBuilder extension)
    {
        var model = extension.ExtensionModel ?? throw new InvalidOperationException($"Extension {extension.Id} needs a model. Call Model on the builder.");
        var id = extension.Id;
        var cost = extension.CostItems.ToList();
        var refund = extension.RefundItems is { } given ? TimbnStationRules.Refund(given) : TimbnStationRules.HalfRefund(cost);
        string Family(GameBalance balance) => TimbnStationRules.ExtensionFamily(extension.TemplateId, building => HasBuilding(balance, building));

        List<IDisposable> undo = [];
        AddTexts(undo, TimbnStationRules.ExtensionTexts(id, extension.Name, extension.Description));
        undo.Add(TimbnModels.AddExtension(id, model, extension.Size, extension.ReachDistance));
        undo.Add(TimbnBalance.AddCopy<WGODef>(_ => extension.TemplateId, id, (_, def) =>
        {
            if (extension.IconId.Length > 0)
                def.craftIconId = extension.IconId;

            extension.EditExtension?.Invoke(def);
        }));
        undo.Add(AddBuilding(balance => TimbnStationRules.BuildingId(Family(balance)), TimbnStationRules.BuildingId(id), id, id,
            extension.BuildDeskId, extension.AreaId, cost, extension.IconId, extension.TechId, slotId: extension.InSlotId));
        undo.Add(AddRemoval(balance => TimbnStationRules.RemovalId(Family(balance)), TimbnStationRules.RemovalId(id), id, extension.BuildDeskId, refund));
        return TimbnUndo.All(undo);
    }

    // The site has the same facings as the station so a facing carries over when it is built. A facing with no site of its
    // own gets the generated outline of its footprint, and a mirrored one shares the site of the facing it flips.
    private static List<TimbnSide<TimbnModel>> SiteSides(string id, IReadOnlyList<TimbnSide<TimbnModel>> sides, List<IDisposable> undo)
    {
        Dictionary<TimbnFacing, TimbnModel> models = [];
        foreach (var side in sides.Where(side => side.MirrorOf == null))
        {
            var name = side.Facing == TimbnFacing.Down ? id : id + side.Facing;
            if (side.Site is null)
            {
                var (made, owner) = TimbnConstructionSite.Create(name, side.Size);
                undo.Add(owner);
                models[side.Facing] = made;
            }
            else
            {
                models[side.Facing] = side.Site;
            }
        }

        return [.. sides.Select(side => new TimbnSide<TimbnModel>(side.Facing, models[side.MirrorOf ?? side.Facing], side.Size, side.MirrorOf))];
    }

    // The game's corpse pallet works this way. Its handler runs the definition's own expressions, E puts in with the
    // first one and takes out with the second, and the whitelist keeps anything but a zombie out of the slot.
    // Nobody works such a station by hand, so its crafts run by themselves, the way the game's resurrection runs on its table.
    private static void HoldZombie(WGODef def)
    {
        def.interactionType = WGODef.InteractionType.CustomInteraction;
        def.isAutoCrafter = true;
        def.inventorySize = 1;
        def.inventoryWhiteList = new WhiteListItemFilter { groupsIds = ["zombie"] };
        def.customInteraction = HoldInteraction(TimbnStationRules.HoldPutCondition, TimbnStationRules.HoldPut, "hint_put");
        def.customInteraction2 = HoldInteraction(TimbnStationRules.HoldTakeCondition, TimbnStationRules.HoldTake, "hint_take");
    }

    private static CustomInteraction HoldInteraction(string condition, IEnumerable<string> execution, string hint) => new()
    {
        condition = TimbnExpression.Formula(condition),
        execution = [.. execution.Select(TimbnExpression.Formula)],
        hint = hint,
    };

    private static bool HasBuilding(GameBalance balance, string buildingId) => balance.GetDataOrNull<BuildingDef>(buildingId) != null;

    private static void AddTexts(List<IDisposable> undo, Dictionary<string, string> texts)
    {
        if (texts.Count > 0)
            undo.Add(TimbnLocale.Add(texts));
    }

    private static IDisposable AddBuilding(Func<GameBalance, string> templateOf, string id, string placedId, string previewId,
        string buildDeskId, string? areaId, IReadOnlyList<(string Item, int Count)> cost, string iconId, string? techId, string? tabId = null, int? limit = null, string? slotId = null)
    {
        var added = TimbnBalance.AddCopy<BuildingDef>(templateOf, id, (_, building) =>
        {
            if (tabId != null)
                building.tab = tabId;

            var area = TimbnStationRules.BuildArea(buildDeskId, areaId, building.buildsIn, building.customBuildAreaId ?? "");
            building.wgoId = placedId;
            building.customWgoPlacePreview = previewId;
            building.buildsIn = [buildDeskId];
            building.customBuildAreaId = area;
            if (slotId != null)
            {
                building.chooseCustomBuildAreaType = BuildingDef.BuildAreaChoosingType.FullCoverSoft;
                building.customBuildAreaId = slotId;
            }

            building.isNeedsUnlock = techId != null;
            building.needItems = [.. cost.Select(item => new NeedItemData(item.Item, item.Count))];
            if (iconId.Length > 0)
                _buildResultIcon(building) = iconId;

            if (limit is { } max)
                AddLimit(building, previewId, max);
        });
        return techId is null ? added : TimbnUndo.All([added, TimbnTechGrants.Building(techId, id)]);
    }

    private static IDisposable AddRemoval(Func<GameBalance, string> templateOf, string id, string worldObjectId, string buildDeskId,
        IReadOnlyList<(string Item, int Count)> refund) =>
        TimbnBalance.AddCopy<BuildingDef>(templateOf, id, (_, removal) =>
        {
            removal.wgoId = worldObjectId;
            removal.buildsIn = [buildDeskId];
            removal.outputItems.chanceOutputItems = [.. refund.Select(item => new ChanceOutputItem { id = item.Item, count = TimbnExpression.Number(item.Count) })];
            removal.outputItems.groupChanceOutputItems.Clear();
        });

    private static void AddLimit(BuildingDef building, string stationId, int max)
    {
        building.limitMax = max;
        building.currentLimitExpression = TimbnExpression.Formula(TimbnStationLimitRules.CurrentExpression(stationId, TimbnStationRules.SiteId(stationId)));
    }
}
