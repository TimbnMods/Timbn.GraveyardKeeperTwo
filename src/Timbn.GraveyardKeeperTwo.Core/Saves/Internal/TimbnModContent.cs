using LazyBearTechnology;
using Newtonsoft.Json.Linq;
using Sirenix.Serialization;
using System.Globalization;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps mod items, buffs, crafts and stations out of the game's save, so the save still loads once the mod is gone.
/// They come out while the game writes its save, go into the Timbn save file under that save's date, and go back
/// where they were when that save loads with the mod installed. Things whose mod is missing at load wait in the
/// file until it is back, and a buff whose mod is missing ends as the game would end it, running its remove effects.
/// A save that already holds unknown items, buffs, crafts or stations has them moved into the file as it loads.
/// </summary>
internal static class TimbnModContent
{
    internal const string SectionId = "Timbn.GraveyardKeeperTwo.Core.ModContent";

    private const string _emptyItemId = TimbnGameIds.Items.Empty;
    private const BindingFlags _fieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Dictionary<Type, FieldInfo[]> _members = [];

    private static readonly AccessTools.FieldRef<LazyExpressionBase, string> _expressionSource =
        AccessTools.FieldRefAccess<LazyExpressionBase, string>("expressionStringUnparsed");

    private static readonly AccessTools.FieldRef<WgoData, Inventory?> _inventory = AccessTools.FieldRefAccess<WgoData, Inventory?>("inventory");

    private static readonly AccessTools.FieldRef<CraftComponent, List<CraftElementBase>> _queue =
        AccessTools.FieldRefAccess<CraftComponent, List<CraftElementBase>>(TimbnStrippedEntry.QueueMember);

    private static readonly AccessTools.FieldRef<CraftComponent, int> _currentIndex = AccessTools.FieldRefAccess<CraftComponent, int>("curCraftQueueIdx");
    private static readonly AccessTools.FieldRef<CraftComponent, CraftComponentStatus> _status = AccessTools.FieldRefAccess<CraftComponent, CraftComponentStatus>("status");
    private static readonly AccessTools.FieldRef<CraftComponent, int> _prevQueueCount = AccessTools.FieldRefAccess<CraftComponent, int>("prevQueueCount");
    private static readonly AccessTools.FieldRef<CraftComponent, bool> _hasPreFinishUpdate = AccessTools.FieldRefAccess<CraftComponent, bool>("hasPreFinishUpdate");

    private static readonly AccessTools.FieldRef<CraftComponent, CraftElementBase?> _lastStarted =
        AccessTools.FieldRefAccess<CraftComponent, CraftElementBase?>(TimbnStrippedEntry.LastStartedMember);

    private static readonly AccessTools.FieldRef<WgoData, List<SGuid>> _extensions = AccessTools.FieldRefAccess<WgoData, List<SGuid>>("attachedWorkbenchExtensions");
    private static readonly AccessTools.FieldRef<WgoData, List<SGuid>> _parents = AccessTools.FieldRefAccess<WgoData, List<SGuid>>("workbenchParents");
    private static readonly AccessTools.FieldRef<ZombieWgoData, SGuid> _attachedStation = AccessTools.FieldRefAccess<ZombieWgoData, SGuid>("attachedWgoDataUniqueId");

    // The Quality setter redraws the world zone widget, which throws while the save is still being prepared.
    private static readonly AccessTools.FieldRef<TownSystem, int> _townQuality = AccessTools.FieldRefAccess<TownSystem, int>("quality");
    private static readonly AccessTools.FieldRef<ZombieWgoData, ZombieType> _zombieType = AccessTools.FieldRefAccess<ZombieWgoData, ZombieType>("zombieType");

    // The IsInteractable setter raises an event that redraws the zombie, which the strip and the load both run too early for.
    private static readonly AccessTools.FieldRef<WgoData, bool> _interactable = AccessTools.FieldRefAccess<WgoData, bool>("isInteractable");
    private static readonly AccessTools.FieldRef<WgoPartBakedDataCollection, Dictionary<string, WgoPartBakedData>> _bakedParts =
        AccessTools.FieldRefAccess<WgoPartBakedDataCollection, Dictionary<string, WgoPartBakedData>>("cachedData");

    private static readonly AccessTools.FieldRef<GdPointsData, List<GDPointData>> _scenePoints = AccessTools.FieldRefAccess<GdPointsData, List<GDPointData>>("scenePoints");

    private static TimbnStrippedStore _store = new();
    private static List<TimbnStrippedEntry> _carried = [];
    private static List<TimbnStrippedEntry> _toEnd = [];
    private static List<TimbnStrippedEntry>? _toRestore;
    private const string _removalLock = "lock_building_removal";
    private static bool _restoringAfterGap;
    private static List<TimbnStrippedEntry>? _pending;
    private static string _pendingPlaythrough = "";
    private static Dictionary<CraftComponent, TimbnCraftQueueState> _queueStates = [];
    private static HashSet<string> _incompleteQueues = [];

    internal static void OnContinueGame(JObject sections, SaveSlotData slot, GameSave? save)
    {
        _carried = [];
        _toEnd = [];
        _pending = null;

        // Cleared first so a throw below never leaves the last slot's sets to be written into this one.
        _store = new TimbnStrippedStore();
        _store = TimbnStrippedStore.FromJson(sections[SectionId]);
        var picked = _store.Pick(slot.saveDateTime, ReadDate(slot), PlaythroughOf(save));
        _toRestore = picked?.Entries;
        _restoringAfterGap = picked is { Exact: false };
        if (picked is { Exact: false } gap)
        {
            TimbnCorePlugin.Logger.LogWarning(
                $"{nameof(TimbnModContent)}|The save of {slot.saveDateTime} was made without Timbn Core, so the mod content of " +
                $"{gap.Stamp}, the last save made with it, goes back, and its buffs end rather than pick up again.");
        }
        else if (picked is null && !_store.IsEmpty)
        {
            TimbnCorePlugin.Logger.LogWarning(
                $"{nameof(TimbnModContent)}|The Timbn save file has no mod content for the save of {slot.saveDateTime} " +
                $"(it has {string.Join(", ", _store.Stamps)}), so none is put back.");
        }
    }

    internal static void OnNewGame()
    {
        _store = new TimbnStrippedStore();
        _carried = [];
        _toEnd = [];
        _toRestore = null;
        _restoringAfterGap = false;
        _pending = null;
    }

    internal static void OnGameStarted()
    {
        var ending = _toEnd;
        _toEnd = [];
        foreach (var entry in ending)
            TimbnSafe.Run(() => End(entry), $"{nameof(TimbnModContent)}|Ending {entry.Id}");
    }

    internal static void OnPrepareForGame(GameSave save)
    {
        var restoring = _toRestore;
        _toRestore = null;
        if (restoring is { Count: > 0 })
            Restore(save, restoring);

        _restoringAfterGap = false;
        ReleaseStrandedZombies(save);

        List<TimbnStrippedEntry> swept = [];
        Strip(save, Kinds.Missing(), undo: null, swept);
        if (swept.Count > 0)
        {
            foreach (var entry in swept)
                TimbnSafe.Run(() => ReleaseHeld(save, entry), $"{nameof(TimbnModContent)}|Handing back what {entry.Id} held");

            _carried.AddRange(swept);
            TimbnCorePlugin.Logger.LogWarning(
                $"{nameof(TimbnModContent)}|Moved {swept.Count} thing(s) with no definition out of the save into the Timbn save file " +
                $"until their mod is back: {Describe(swept)}");
        }
    }

    internal static IDisposable StripForSave(GameSave save)
    {
        List<Action> undo = [];
        List<TimbnStrippedEntry> stripped = [];
        Strip(save, Kinds.ForSave(), undo, stripped);
        _pending = stripped;
        _pendingPlaythrough = PlaythroughOf(save);
        if (stripped.Count > 0)
            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnModContent)}|Kept {stripped.Count} mod thing(s) out of the save: {Describe(stripped)}");

        return new TimbnUndo(() =>
        {
            for (var i = undo.Count - 1; i >= 0; i--)
                undo[i]();
        });
    }

    internal static void KeepSection(JObject sections, string stamp)
    {
        List<TimbnStrippedEntry> entries = [.. _pending ?? [], .. _carried];
        _pending = null;
        if (entries.Count == 0 && _store.IsEmpty)
            return;

        DateTime? savedAt = DateTime.TryParse(stamp, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
        _store.Add(stamp, savedAt, _pendingPlaythrough, entries);
        sections[SectionId] = _store.ToJson();
    }

    private static void Strip(GameSave save, Kinds take, List<Action>? undo, List<TimbnStrippedEntry> taken)
    {
        try
        {
            // Stations first, so whatever a mod station holds leaves with it and the walks below never see it.
            StripStations(save, take.Station, taken, undo);
            StripMembers(save.playerData, TimbnStrippedEntry.Player, "", "", take.Item, taken, undo);
            StripPinned(save.playerData, take.Item, taken, undo);
            foreach (var scene in save.worldData.gameSceneDataList)
            {
                foreach (var wgo in scene.wgoDataList)
                {
                    StripMembers(wgo, TimbnStrippedEntry.WorldObject, scene.id, wgo.UniqueId.Id, take.Item, taken, undo);
                    StripCrafts(wgo, scene.id, take.Craft, taken, undo);
                    StripParts(wgo, scene.id, take.Part, taken, undo);
                    StripFilters(wgo, scene.id, take.Item, taken, undo);
                }

                StripDrops(scene, take.Item, taken, undo);
            }

            foreach (var vendor in save.vendorSystem.vendors)
                StripMembers(vendor, TimbnStrippedEntry.Vendor, "", vendor.id, take.Item, taken, undo);

            StripPerks(save.perkSystemData.activePerks, take.Perk, taken, undo);
            StripQuests(save.questSystemData?.questCollection?.quests, take.Quest, taken, undo);
        }
        catch (Exception ex)
        {
            // What was taken before the throw stays in taken and undo, so it still reaches the Timbn file and goes back.
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnModContent)}|Stopped partway through the save after taking {taken.Count} mod thing(s), so any left stay in the game's save: {ex}");
        }
    }

    private static void StripMembers(object? owner, string root, string scene, string ownerId, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (owner is null)
            return;

        foreach (var field in Members(owner.GetType()))
        {
            var entry = new TimbnStrippedEntry { Root = root, Scene = scene, Owner = ownerId, Member = field.Name };
            switch (field.GetValue(owner))
            {
                case Inventory inventory:
                    StripList(inventory.Data?.Inventory, entry, [], take, taken, undo);
                    break;
                case List<Item> list:
                    StripList(list, entry, [], take, taken, undo);
                    break;
                case Item item when Takes(item, take):
                    entry.Id = item.id;
                    entry.Blob = Pack(item);
                    taken.Add(entry);
                    field.SetValue(owner, Item.Empty);
                    undo?.Add(() => field.SetValue(owner, item));
                    break;
                case Item item:
                    StripList(item.Inventory, entry, [], take, taken, undo);
                    break;
            }
        }
    }

    private static void StripList(List<Item>? list, TimbnStrippedEntry place, List<int> path, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (list is null)
            return;

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is { } item && !Takes(item, take))
                StripList(item.Inventory, place, [.. path, i], take, taken, undo);
        }

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var item = list[i];
            if (item is null || !Takes(item, take))
                continue;

            taken.Add(new TimbnStrippedEntry
            {
                Root = place.Root,
                Scene = place.Scene,
                Owner = place.Owner,
                Member = place.Member,
                Path = [.. path, i],
                Id = item.id,
                Blob = Pack(item),
            });
            list.RemoveAt(i);
            var index = i;
            undo?.Add(() => list.Insert(Math.Min(index, list.Count), item));
        }
    }

    private static void StripDrops(GameSceneData scene, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        var drops = scene.droppedItems;
        for (var i = drops.Count - 1; i >= 0; i--)
        {
            var drop = drops[i];
            if (drop?.Item is not { } item || !Takes(item, take))
                continue;

            taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Drop, Scene = scene.id, Path = [i], Id = item.id, Blob = Pack(drop) });
            drops.RemoveAt(i);
            var index = i;
            undo?.Add(() => drops.Insert(Math.Min(index, drops.Count), drop));
        }
    }

    private static void StripPerks(List<PerkData> perks, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        for (var i = perks.Count - 1; i >= 0; i--)
        {
            var perk = perks[i];
            if (perk is null || string.IsNullOrEmpty(perk.id) || !take(perk.id))
                continue;

            taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Perk, Path = [i], Id = perk.id, Blob = Pack(perk), Ending = EndingOf(perk.id) });
            perks.RemoveAt(i);
            var index = i;
            undo?.Add(() => perks.Insert(Math.Min(index, perks.Count), perk));
        }
    }

    // A quest's saved entry is its id, status and two flags. Its triggers are not saved and come back from the
    // definition on load, so the entry is all that has to leave with the mod.
    private static void StripQuests(List<QuestData>? quests, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (quests is null)
            return;

        for (var i = quests.Count - 1; i >= 0; i--)
        {
            var quest = quests[i];
            if (quest is null || string.IsNullOrEmpty(quest.id) || !take(quest.id))
                continue;

            taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Quest, Path = [i], Id = quest.id, Blob = Pack(quest) });
            quests.RemoveAt(i);
            var index = i;
            undo?.Add(() => quests.Insert(Math.Min(index, quests.Count), quest));
        }
    }

    // The hotbar pins are item ids in a plain string array, which the member walk above never sees, and the game
    // draws every pinned id as an item with a definition when the save opens.
    private static void StripPinned(PlayerData? player, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (player?.pinnedItems is not { } pinned)
            return;

        for (var i = 0; i < pinned.Length; i++)
        {
            var itemId = pinned[i];
            if (string.IsNullOrEmpty(itemId) || !take(itemId))
                continue;

            taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Pinned, Path = [i], Id = itemId });
            pinned[i] = string.Empty;
            var index = i;
            undo?.Add(() => pinned[index] = itemId);
        }
    }

    private static void StripCrafts(WgoData wgo, string scene, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (wgo.CraftComponent is not { } component || _queue(component) is not { } queue)
            return;

        var lastStarted = _lastStarted(component);
        var lastStartedIsMod = lastStarted is not null && take(lastStarted.CraftId ?? "");
        var plan = TimbnCraftQueueStrip.Plan(queue.Select(element => element?.CraftId ?? "").ToList(), _currentIndex(component), _status(component), take);
        if (plan.Removed.Count == 0 && !lastStartedIsMod)
            return;

        var state = new TimbnCraftQueueState
        {
            CurrentIndex = _currentIndex(component),
            Status = (int)_status(component),
            HasPreFinishUpdate = _hasPreFinishUpdate(component),
            PrevQueueCount = _prevQueueCount(component),
        };
        var owner = wgo.UniqueId.Id;
        List<(int Index, CraftElementBase Element)> removed = [];
        foreach (var index in plan.Removed)
        {
            var element = queue[index];
            removed.Add((index, element));
            taken.Add(new TimbnStrippedEntry
            {
                Root = TimbnStrippedEntry.Craft,
                Scene = scene,
                Owner = owner,
                Member = TimbnStrippedEntry.QueueMember,
                Path = [index],
                Id = element.CraftId,
                Blob = Pack(element),
                Queue = state,
            });
        }

        if (lastStartedIsMod)
        {
            taken.Add(new TimbnStrippedEntry
            {
                Root = TimbnStrippedEntry.Craft,
                Scene = scene,
                Owner = owner,
                Member = TimbnStrippedEntry.LastStartedMember,
                Id = lastStarted!.CraftId ?? "",
                Blob = Pack(lastStarted),
                Queue = state,
            });
        }

        for (var i = removed.Count - 1; i >= 0; i--)
            queue.RemoveAt(removed[i].Index);

        _currentIndex(component) = plan.CurrentIndex;
        _status(component) = plan.Status;
        if (plan.CurrentRemoved)
            _hasPreFinishUpdate(component) = false;

        if (plan.Emptied)
            _prevQueueCount(component) = 0;

        if (lastStartedIsMod)
            _lastStarted(component) = null;

        undo?.Add(() =>
        {
            foreach (var (index, element) in removed)
                queue.Insert(Math.Min(index, queue.Count), element);

            ApplyQueueState(component, state);
            if (lastStartedIsMod)
                _lastStarted(component) = lastStarted;
        });
    }

    private static void StripParts(WgoData wgo, string scene, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        var parts = wgo.AdditionalWgoPartsData;
        if (parts is null)
            return;

        for (var i = parts.Count - 1; i >= 0; i--)
        {
            var part = parts[i];
            if (part is null || !take(part.id))
                continue;

            taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Part, Scene = scene, Owner = wgo.UniqueId.Id, Path = [i], Id = part.id, Blob = Pack(part) });
            parts.RemoveAt(i);
            var index = i;
            undo?.Add(() => parts.Insert(Math.Min(index, parts.Count), part));
        }
    }

    private static void StripFilters(WgoData wgo, string scene, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (SlotsOf(wgo) is not { } slots)
            return;

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot is null || !take(slot.slotItemId))
                continue;

            var itemId = slot.slotItemId;
            taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Filter, Scene = scene, Owner = wgo.UniqueId.Id, Path = [i], Id = itemId });
            slot.slotItemId = string.Empty;
            undo?.Add(() => slot.slotItemId = itemId);
        }
    }

    private static List<ConveyorChestSlotData>? SlotsOf(WgoData? wgo) => (wgo as ConveyorWgoData)?.ConveyorComponent switch
    {
        ConveyorChestComponent chest => chest.SlotsData,
        ConveyorChestOutComponent chestOut => chestOut.SlotsData,
        _ => null,
    };

    private static void StripStations(GameSave save, Func<string, bool> take, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        Dictionary<Guid, string> stations = [];
        foreach (var scene in save.worldData.gameSceneDataList)
        {
            var list = scene.wgoDataList;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var wgo = list[i];
                if (wgo is null || string.IsNullOrEmpty(wgo.id) || !take(wgo.id))
                    continue;

                taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Station, Scene = scene.id, Owner = wgo.UniqueId.Id, Path = [i], Id = wgo.id, Blob = Pack(wgo) });
                list.RemoveAt(i);
                var index = i;
                undo?.Add(() => list.Insert(Math.Min(index, list.Count), wgo));
                stations[wgo.UniqueId.Guid] = wgo.id;
                StripTownShare(save, wgo, undo);
                StripPoints(save, wgo, taken, undo);
            }
        }

        if (stations.Count == 0)
            return;

        foreach (var scene in save.worldData.gameSceneDataList)
        {
            for (var z = 0; z < scene.worldZones.Count; z++)
            {
                var zone = scene.worldZones[z];
                var place = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Zone, Scene = scene.id, Owner = zone.id ?? "", Path = [z] };
                StripGuids(zone.wgoDataList, place, nameof(WorldZoneData.wgoDataList), stations, taken, undo);
                StripGuids(zone.customQualityWgoDataList, place, nameof(WorldZoneData.customQualityWgoDataList), stations, taken, undo);
            }

            foreach (var wgo in scene.wgoDataList)
            {
                var place = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Link, Scene = scene.id, Owner = wgo.UniqueId.Id };
                StripGuids(_extensions(wgo), place, "attachedWorkbenchExtensions", stations, taken, undo);
                StripGuids(_parents(wgo), place, "workbenchParents", stations, taken, undo);
                if (wgo is ZombieWgoData zombie)
                    StripAttachment(zombie, scene.id, stations, taken, undo);
            }
        }

        StripActivities(save.craftSystemData?.zombieCraftActivities, nameof(GameSave.craftSystemData), stations, taken, undo);
        StripActivities(save.conveyorSystemData?.zombieCraftActivities, nameof(GameSave.conveyorSystemData), stations, taken, undo);
    }

    /// <summary>
    /// A station adds its town quality and its entry in an NPC group when the game places it, and the game takes both
    /// back in OnRemove. The strip leaves OnRemove out, so without this the quality stays raised and the group keeps a
    /// guid for a station that is gone once the mod is removed.
    /// </summary>
    private static void StripTownShare(GameSave save, WgoData station, List<Action>? undo)
    {
        if (GameBalance.Me.GetDataOrNull<WGODef>(station.id) is not { } definition)
            return;

        if (definition.townQuality > 0 && save.townSystem is { } town)
        {
            var quality = definition.townQuality;
            town.Quality -= quality;
            undo?.Add(() => town.Quality += quality);
        }

        if (!string.IsNullOrEmpty(definition.npcLifeSimGroup) && save.npcLifeSimulatorData?.GetGroupById(definition.npcLifeSimGroup) is { } group)
        {
            group.RemoveWgoFromGroup(station);
            undo?.Add(() => group.AddWgoToGroup(station));
        }
    }

    private static void PutTownShare(GameSave save, WgoData station)
    {
        if (GameBalance.Me.GetDataOrNull<WGODef>(station.id) is not { } definition)
            return;

        if (definition.townQuality > 0 && save.townSystem is { } town)
            _townQuality(town) += definition.townQuality;

        if (!string.IsNullOrEmpty(definition.npcLifeSimGroup) && save.npcLifeSimulatorData?.GetGroupById(definition.npcLifeSimGroup) is { } group)
            group.AddWgoToGroup(station);
    }

    private static void StripPoints(GameSave save, WgoData station, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        // A station's NPC points are the same instances as the world's list, and the blob only holds copies, so the
        // world's are taken out with it and the station's own go back into the list when it returns.
        if (station.gdPointsData is not { Count: > 0 } points || save.worldData.gdPointsData is not { } world || _scenePoints(world) is not { } scenePoints)
            return;

        for (var ordinal = 0; ordinal < points.Count; ordinal++)
        {
            var point = points[ordinal];
            var index = scenePoints.IndexOf(point);
            if (index < 0)
                continue;

            taken.Add(new TimbnStrippedEntry
            {
                Root = TimbnStrippedEntry.Point,
                Scene = station.WorldId ?? "",
                Owner = station.UniqueId.Id,
                Path = [index],
                Id = station.id,
                Blob = ordinal.ToString(CultureInfo.InvariantCulture),
            });
            scenePoints.RemoveAt(index);
            undo?.Add(() => scenePoints.Insert(Math.Min(index, scenePoints.Count), point));
        }
    }

    private static void StripGuids(List<SGuid>? list, TimbnStrippedEntry place, string member, Dictionary<Guid, string> stations, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (list is null)
            return;

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var guid = list[i];
            if (guid is null || !stations.TryGetValue(guid.Guid, out var stationId))
                continue;

            taken.Add(new TimbnStrippedEntry
            {
                Root = place.Root,
                Scene = place.Scene,
                Owner = place.Owner,
                Member = member,
                Path = [.. place.Path, i],
                Id = stationId,
                Blob = guid.Id,
            });
            list.RemoveAt(i);
            var index = i;
            undo?.Add(() => list.Insert(Math.Min(index, list.Count), guid));
        }
    }

    private static void StripAttachment(ZombieWgoData zombie, string scene, Dictionary<Guid, string> stations, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        var station = _attachedStation(zombie);
        if (station is null || station.IsEmpty || !stations.TryGetValue(station.Guid, out var stationId))
            return;

        var type = _zombieType(zombie);
        var interactable = _interactable(zombie);
        var dock = zombie.takenDockPointsParentSGuid;
        var dockIsStation = dock is not null && stations.ContainsKey(dock.Guid);
        taken.Add(new TimbnStrippedEntry
        {
            Root = TimbnStrippedEntry.Worker,
            Scene = scene,
            Owner = zombie.UniqueId.Id,
            Id = stationId,
            Attachment = new TimbnWorkerAttachment { Station = station.Id, ZombieType = (int)type, DockParent = dockIsStation ? dock!.Id : "", Locked = TimbnSeatedZombieRules.WasLocked(interactable) },
        });

        // The same fields the game's own detach clears, so the save holds a free zombie standing where it was.
        _attachedStation(zombie) = SGuid.Empty;
        _zombieType(zombie) = ZombieType.Free;
        _interactable(zombie) = true;
        if (dockIsStation)
            zombie.takenDockPointsParentSGuid = null;

        undo?.Add(() =>
        {
            _attachedStation(zombie) = station;
            _zombieType(zombie) = type;
            _interactable(zombie) = interactable;
            if (dockIsStation)
                zombie.takenDockPointsParentSGuid = dock;
        });
    }

    private static void StripActivities(List<ZombieCraftActivity>? activities, string member, Dictionary<Guid, string> stations, List<TimbnStrippedEntry> taken, List<Action>? undo)
    {
        if (activities is null)
            return;

        for (var i = activities.Count - 1; i >= 0; i--)
        {
            var activity = activities[i];
            if (activity?.WgoUniqueId is not { } station || !stations.TryGetValue(station.Guid, out var stationId))
                continue;

            taken.Add(new TimbnStrippedEntry { Root = TimbnStrippedEntry.Activity, Member = member, Path = [i], Id = stationId, Blob = Pack(activity) });
            activities.RemoveAt(i);
            var index = i;
            undo?.Add(() => activities.Insert(Math.Min(index, activities.Count), activity));
        }
    }

    private static void Restore(GameSave save, List<TimbnStrippedEntry> entries)
    {
        var restored = 0;
        _queueStates = [];
        _incompleteQueues = entries
            .Where(entry => entry.Root == TimbnStrippedEntry.Craft && !CraftExists(entry.Id))
            .Select(entry => entry.Owner)
            .ToHashSet();
        foreach (var entry in TimbnStrippedStore.RestoreOrder(entries))
        {
            var isPerk = entry.Root == TimbnStrippedEntry.Perk;
            var known = IsKnown(entry);
            if (isPerk && (!known || _restoringAfterGap) && entry.Ending is not null)
            {
                _toEnd.Add(entry);
                continue;
            }

            if (isPerk && _restoringAfterGap)
                continue;

            if (!known)
            {
                TimbnSafe.Run(() => ReleaseHeld(save, entry), $"{nameof(TimbnModContent)}|Handing back what {entry.Id} held");
                _carried.Add(entry);
                continue;
            }

            var put = false;
            TimbnSafe.Run(() => put = Put(save, entry), $"{nameof(TimbnModContent)}|Putting back {entry.Id}");
            if (put)
                restored++;
            else
                _carried.Add(entry);
        }

        // Only after every craft of a queue is back, so the saved index never points at a craft that stayed in the file.
        if (!_restoringAfterGap)
        {
            foreach (var pair in _queueStates)
                ApplyQueueState(pair.Key, pair.Value);
        }

        _queueStates = [];
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnModContent)}|Put back {restored} mod thing(s) from the Timbn save file.");
        if (_carried.Count > 0)
            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnModContent)}|{_carried.Count} wait in the Timbn save file until their mod is back: {Describe(_carried)}");
    }

    private static bool IsKnown(TimbnStrippedEntry entry)
    {
        if (entry.Root == TimbnStrippedEntry.Perk)
            return PerkExists(entry.Id);

        if (entry.Root == TimbnStrippedEntry.Craft)
            return CraftExists(entry.Id);

        if (entry.Root == TimbnStrippedEntry.Part)
            return PartExists(entry.Id);

        if (entry.Root == TimbnStrippedEntry.Quest)
            return QuestExists(entry.Id);

        return entry.IsStationPart ? StationExists(entry.Id) : ItemExists(entry.Id);
    }

    private static bool Put(GameSave save, TimbnStrippedEntry entry)
    {
        switch (entry.Root)
        {
            case TimbnStrippedEntry.Perk:
                var perk = Unpack<PerkData>(entry.Blob);
                var perks = save.perkSystemData.activePerks;
                if (perks.Any(active => active.id == perk.id))
                {
                    TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|{perk.id} is already active, so the saved one is dropped.");
                    return true;
                }

                perks.Insert(Math.Min(entry.Path.FirstOrDefault(), perks.Count), perk);
                return true;
            case TimbnStrippedEntry.Drop:
                var drop = Unpack<DropData>(entry.Blob);
                if (save.worldData.GetGameSceneDataById(entry.Scene) is { } scene)
                {
                    scene.droppedItems.Insert(Math.Min(entry.Path.FirstOrDefault(), scene.droppedItems.Count), drop);
                    return true;
                }

                return PlaceElsewhere(save, drop.Item, null, entry);
            case TimbnStrippedEntry.Craft:
                return PutCraft(save, entry);
            case TimbnStrippedEntry.Station:
                return PutStation(save, entry);
            case TimbnStrippedEntry.Zone:
                return PutZoneGuid(save, entry);
            case TimbnStrippedEntry.Link:
                return PutLink(save, entry);
            case TimbnStrippedEntry.Worker:
                return PutAttachment(save, entry);
            case TimbnStrippedEntry.Activity:
                return PutActivity(save, entry);
            case TimbnStrippedEntry.Point:
                return PutPoint(save, entry);
            case TimbnStrippedEntry.Part:
                return PutPart(save, entry);
            case TimbnStrippedEntry.Filter:
                return PutFilter(save, entry);
            case TimbnStrippedEntry.Pinned:
                return PutPinned(save, entry);
            case TimbnStrippedEntry.Quest:
                return PutQuest(save, entry);
            default:
                return PutItem(save, entry, Unpack<Item>(entry.Blob));
        }
    }

    private static bool PutCraft(GameSave save, TimbnStrippedEntry entry)
    {
        var element = Unpack<CraftElementBase>(entry.Blob);
        if (FindWgo(save, entry.Scene, entry.Owner)?.CraftComponent is not { } component || _queue(component) is not { } queue)
        {
            // The station is gone, so the craft can't be, but whatever it had already taken in can still be handed back.
            var handedBack = 0;
            foreach (var item in element.CraftInput ?? [])
            {
                if (PlaceElsewhere(save, item, null, entry))
                    handedBack++;
            }

            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The station {entry.Id} was queued at is gone, so the craft is dropped and {handedBack} of its ingredients handed back.");
            return true;
        }

        if (entry.Member == TimbnStrippedEntry.LastStartedMember)
        {
            _lastStarted(component) = element;
        }
        else
        {
            var at = Math.Min(entry.Path.FirstOrDefault(), queue.Count);
            queue.Insert(at, element);
        }

        if (entry.Queue is { } state && !_incompleteQueues.Contains(entry.Owner))
            _queueStates[component] = state;

        return true;
    }

    // A zombie lying in a station is its item inside the station's saved copy, while the zombie's own data stays in the
    // game's save, so a mod that never comes back would take the zombie with it. Whatever the game can hold without the
    // mod goes on the ground where the station stood, and the copy keeps only the mod's own items.
    private static void ReleaseHeld(GameSave save, TimbnStrippedEntry entry)
    {
        if (entry.Root != TimbnStrippedEntry.Station)
            return;

        var wgo = Unpack<WgoData>(entry.Blob);
        List<TimbnStrippedEntry> held = [];
        StripMembers(wgo, TimbnStrippedEntry.WorldObject, entry.Scene, entry.Owner, ItemExists, held, undo: null);

        // Taking items out of the list leaves the cached fill size behind, so an emptied slot would still refuse to take anything.
        // The field, not the property, which reads the definition and the mod's is gone.
        if (held.Count > 0 && _inventory(wgo)?.Data is { } inventory)
            AccessTools.Method(typeof(Item), "CalculateInventoryFillSize").Invoke(inventory, null);

        // A station that held a zombie has its removal lock set, and with the zombie gone it would come back locked for good.
        // Checked even when nothing is left to hand back, since an earlier load may have handed the zombie back already.
        var locked = wgo.GetGameRes(_removalLock) != 0;
        if (locked)
        {
            wgo.SetGameRes(_removalLock, 0);
            if (held.Count == 0)
            {
                entry.Blob = Pack(wgo);
                TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|{entry.Id} ({entry.Owner}) waits for its mod and was still locked against removal with nothing in it, so it is unlocked.");
                return;
            }
        }

        if (held.Count == 0)
            return;

        var scene = save.worldData.GetGameSceneDataById(entry.Scene);
        foreach (var each in held)
        {
            var item = Unpack<Item>(each.Blob);
            if (scene is not null)
                scene.droppedItems.Add(new DropData(item, wgo.Position, entry.Scene));
            else
                PlaceElsewhere(save, item, null, each);
        }

        entry.Blob = Pack(wgo);
        TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|{entry.Id} ({entry.Owner}) waits for its mod, so the {held.Count} item(s) it held are on the ground where it stood: {Describe(held)}.");
    }

    private static bool PutStation(GameSave save, TimbnStrippedEntry entry)
    {
        if (save.worldData.GetGameSceneDataById(entry.Scene) is not { } scene)
            return false;

        var wgo = Unpack<WgoData>(entry.Blob);
        if (scene.wgoDataList.Any(other => other.UniqueId.Guid == wgo.UniqueId.Guid))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|{entry.Id} ({entry.Owner}) is already in {entry.Scene}, so the saved one is dropped.");
            return true;
        }

        scene.wgoDataList.Insert(Math.Min(entry.Path.FirstOrDefault(), scene.wgoDataList.Count), wgo);
        PutTownShare(save, wgo);
        return true;
    }

    private static bool PutZoneGuid(GameSave save, TimbnStrippedEntry entry)
    {
        if (save.worldData.GetGameSceneDataById(entry.Scene) is not { } scene || entry.Path.Count < 2)
            return false;

        var zones = scene.worldZones;
        var zone = entry.Path[0] < zones.Count && zones[entry.Path[0]].id == entry.Owner
            ? zones[entry.Path[0]]
            : zones.FirstOrDefault(candidate => candidate.id == entry.Owner);
        if (zone is null)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The zone {entry.Owner} in {entry.Scene} is gone, so {entry.Id} is not listed in it.");
            return true;
        }

        var list = entry.Member == nameof(WorldZoneData.customQualityWgoDataList) ? zone.customQualityWgoDataList : zone.wgoDataList;
        InsertGuid(list, entry.Path[1], entry.Blob);
        return true;
    }

    private static bool PutLink(GameSave save, TimbnStrippedEntry entry)
    {
        if (FindWgo(save, entry.Scene, entry.Owner) is not { } wgo)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The object {entry.Id} was linked to is gone, so the link is dropped.");
            return true;
        }

        var list = entry.Member == "workbenchParents" ? _parents(wgo) : _extensions(wgo);
        InsertGuid(list, entry.Path.FirstOrDefault(), entry.Blob);
        return true;
    }

    private static bool PutAttachment(GameSave save, TimbnStrippedEntry entry)
    {
        if (entry.Attachment is not { } attachment || string.IsNullOrEmpty(attachment.Station))
            return true;

        if (FindWgo(save, entry.Scene, entry.Owner) is not ZombieWgoData zombie)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The zombie that worked at {entry.Id} is gone, so it is not put back to work.");
            return true;
        }

        if (!SGuid.IsNullOrEmpty(_attachedStation(zombie)))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The zombie that worked at {entry.Id} works somewhere else now, so it stays there.");
            return true;
        }

        _attachedStation(zombie) = new SGuid(attachment.Station);
        _zombieType(zombie) = (ZombieType)attachment.ZombieType;
        if (attachment.DockParent.Length > 0)
            zombie.takenDockPointsParentSGuid = new SGuid(attachment.DockParent);

        if (attachment.Locked)
            _interactable(zombie) = false;

        return true;
    }

    // A zombie whose station was taken from under it by an older build, or by a mod that was removed while it was locked, would stay locked forever.
    private static void ReleaseStrandedZombies(GameSave save)
    {
        var released = 0;
        foreach (var scene in save.worldData.gameSceneDataList)
        {
            foreach (var wgo in scene.wgoDataList)
            {
                if (wgo is ZombieWgoData zombie && TimbnSeatedZombieRules.IsStranded(_zombieType(zombie), !SGuid.IsNullOrEmpty(_attachedStation(zombie)), _interactable(zombie)))
                {
                    _interactable(zombie) = true;
                    released++;
                }
            }
        }

        if (released > 0)
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|Unlocked {released} zombie(s) that were left locked with no station to unlock them.");
    }

    private static bool PutActivity(GameSave save, TimbnStrippedEntry entry)
    {
        var list = entry.Member == nameof(GameSave.conveyorSystemData) ? save.conveyorSystemData?.zombieCraftActivities : save.craftSystemData?.zombieCraftActivities;
        if (list is null)
            return false;

        var activity = Unpack<ZombieCraftActivity>(entry.Blob);
        list.RemoveAll(other => other?.ZombieUniqueId is { } zombie && zombie.Guid == activity.ZombieUniqueId.Guid);
        list.Insert(Math.Min(entry.Path.FirstOrDefault(), list.Count), activity);
        return true;
    }

    private static bool PutPoint(GameSave save, TimbnStrippedEntry entry)
    {
        if (FindWgo(save, entry.Scene, entry.Owner) is not { } station || save.worldData.gdPointsData is not { } world || _scenePoints(world) is not { } scenePoints)
            return true;

        if (!int.TryParse(entry.Blob, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ordinal) || ordinal < 0 || ordinal >= station.gdPointsData.Count)
            return true;

        var point = station.gdPointsData[ordinal];
        if (!scenePoints.Contains(point))
            scenePoints.Insert(Math.Min(entry.Path.FirstOrDefault(), scenePoints.Count), point);

        return true;
    }

    private static bool PutPart(GameSave save, TimbnStrippedEntry entry)
    {
        if (FindWgo(save, entry.Scene, entry.Owner) is not { } wgo)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The object that had the part {entry.Id} is gone, so the part is dropped.");
            return true;
        }

        var parts = wgo.AdditionalWgoPartsData;
        if (parts.Any(other => other.id == entry.Id))
            return true;

        parts.Insert(Math.Min(entry.Path.FirstOrDefault(), parts.Count), Unpack<WgoPartData>(entry.Blob));
        return true;
    }

    private static bool PutFilter(GameSave save, TimbnStrippedEntry entry)
    {
        var slots = SlotsOf(FindWgo(save, entry.Scene, entry.Owner));
        var index = entry.Path.FirstOrDefault();
        if (slots is null || index >= slots.Count || !string.IsNullOrEmpty(slots[index].slotItemId))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The chest slot that filtered {entry.Id} is gone or has another filter now, so the filter is dropped.");
            return true;
        }

        slots[index].slotItemId = entry.Id;
        return true;
    }

    private static bool PutPinned(GameSave save, TimbnStrippedEntry entry)
    {
        var pinned = save.playerData?.pinnedItems;
        var index = entry.Path.FirstOrDefault();
        if (pinned is null || index >= pinned.Length || !string.IsNullOrEmpty(pinned[index]))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The hotbar slot that held {entry.Id} is gone or pins another item now, so the pin is dropped.");
            return true;
        }

        pinned[index] = entry.Id;
        return true;
    }

    private static bool PutQuest(GameSave save, TimbnStrippedEntry entry)
    {
        var quests = save.questSystemData?.questCollection?.quests;
        if (quests is null)
            return false;

        if (quests.Any(quest => quest?.id == entry.Id))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|The quest {entry.Id} is already in the save, so the saved one is dropped.");
            return true;
        }

        quests.Insert(Math.Min(entry.Path.FirstOrDefault(), quests.Count), Unpack<QuestData>(entry.Blob));
        return true;
    }

    private static void InsertGuid(List<SGuid> list, int at, string id)
    {
        var guid = new SGuid(id);
        if (list.Contains(guid))
            return;

        list.Insert(Math.Min(at, list.Count), guid);
    }

    private static void ApplyQueueState(CraftComponent component, TimbnCraftQueueState state)
    {
        _currentIndex(component) = state.CurrentIndex;
        _status(component) = (CraftComponentStatus)state.Status;
        _hasPreFinishUpdate(component) = state.HasPreFinishUpdate;
        _prevQueueCount(component) = state.PrevQueueCount;
    }

    private static bool PutItem(GameSave save, TimbnStrippedEntry entry, Item item)
    {
        var owner = FindOwner(save, entry);
        var field = owner is null ? null : Members(owner.GetType()).FirstOrDefault(member => member.Name == entry.Member);
        if (field is null)
            return PlaceElsewhere(save, item, null, entry);

        var value = field.GetValue(owner);
        if (entry.Path.Count == 0)
        {
            if (value is Item slot && !slot.IsEmpty)
                return PlaceElsewhere(save, item, null, entry);

            field.SetValue(owner, item);
            return true;
        }

        if (value is null)
            return PlaceElsewhere(save, item, null, entry);

        var top = value switch
        {
            Inventory inventory => inventory.Data,
            Item holder => holder,
            _ => null,
        };
        var list = value is List<Item> plain ? plain : top?.Inventory;
        Item? container = null;
        for (var depth = 0; list is not null && depth < entry.Path.Count - 1; depth++)
        {
            var index = entry.Path[depth];

            // Indexes go stale once other items were carried out or the lists changed, so only a real container counts.
            container = index < list.Count ? list[index] : null;
            list = container is { InventorySize: > 0 } ? container.Inventory : null;
        }

        var at = entry.Path[entry.Path.Count - 1];
        if (list is not null && at <= list.Count && (container is null || HasRoom(container)))
        {
            list.Insert(at, item);
            return true;
        }

        return PlaceElsewhere(save, item, top, entry);
    }

    private static bool PlaceElsewhere(GameSave save, Item? item, Item? container, TimbnStrippedEntry entry)
    {
        if (item is null)
            return false;

        if (container is not null && HasRoom(container))
        {
            container.Inventory.Add(item);
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|{entry.Id} went back into its container, but not to its old spot.");
            return true;
        }

        if (save.playerData.inventory?.Data is { } player && HasRoom(player))
        {
            player.Inventory.Add(item);
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|{entry.Id} could not go back where it was, so it is in the player's inventory.");
            return true;
        }

        var sceneId = save.playerData.currentGameSceneId;
        if (save.worldData.GetGameSceneDataById(sceneId) is { } scene)
        {
            scene.droppedItems.Add(new DropData(item, save.playerData.position.Value, sceneId));
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModContent)}|{entry.Id} could not go back where it was, so it is on the ground at the player.");
            return true;
        }

        return false;
    }

    private static TimbnPerkEnding? EndingOf(string perkId)
    {
        if (GameBalance.Me.GetDataOrNull<PerkDef>(perkId) is not { } definition)
            return null;

        return new TimbnPerkEnding
        {
            SetResources = definition.setGameResOnRemove is { List.Count: > 0 } set ? Pack(set) : "",
            AddResources = definition.addGameResOnRemove is { } add && !add.IsEmpty() ? Pack(add) : "",
            Expressions = (definition.onRemoveExpressions ?? []).Select(expression => _expressionSource(expression)).Where(source => !string.IsNullOrEmpty(source)).ToList(),
        };
    }

    private static void End(TimbnStrippedEntry entry)
    {
        var ending = entry.Ending!;
        if (ending.SetResources.Length > 0)
            MainGame.PlayerData.SetRes(Unpack<GameRes>(ending.SetResources));

        if (ending.AddResources.Length > 0)
            MainGame.PlayerData.AddRes(Unpack<GameRes>(ending.AddResources));

        foreach (var source in ending.Expressions)
            TimbnSafe.Run(() => new TimbnExpression(source).Run(), $"{nameof(TimbnModContent)}|Running {source} as {entry.Id} ends");

        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnModContent)}|Ended {entry.Id}, since its mod is gone, and ran what it does when it ends.");
    }

    private static object? FindOwner(GameSave save, TimbnStrippedEntry entry) => entry.Root switch
    {
        TimbnStrippedEntry.Player => save.playerData,
        TimbnStrippedEntry.WorldObject => FindWgo(save, entry.Scene, entry.Owner),
        TimbnStrippedEntry.Vendor => save.vendorSystem.vendors.FirstOrDefault(vendor => vendor.id == entry.Owner),
        _ => null,
    };

    private static WgoData? FindWgo(GameSave save, string scene, string uniqueId) =>
        save.worldData.GetGameSceneDataById(scene)?.wgoDataList.FirstOrDefault(wgo => wgo.UniqueId.Id == uniqueId);

    private static bool HasRoom(Item container) => container.InventorySize > container.Inventory.Count;

    private static string PlaythroughOf(GameSave? save) => save?.playerData?.inventory?.Data?.UniqueId?.Id ?? "";

    private static DateTime? ReadDate(SaveSlotData slot)
    {
        CultureInfo culture;
        try
        {
            culture = string.IsNullOrEmpty(slot.serializedCulture) ? CultureInfo.CurrentCulture : new CultureInfo(slot.serializedCulture);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.CurrentCulture;
        }

        return DateTime.TryParse(slot.saveDateTime, culture, DateTimeStyles.None, out var date) ? date : null;
    }

    private static bool Takes(Item item, Func<string, bool> take) => take(item.id);

    private static bool ItemExists(string id) => GameBalance.Me.GetDataOrNull<ItemDef>(id) != null;

    private static bool PerkExists(string id) => GameBalance.Me.GetDataOrNull<PerkDef>(id) != null;

    private static bool QuestExists(string id) => TimbnQuests.HasDefinition(id);

    // CraftDef first, since GetCraftDefBase logs a line for every id it has to look up as an alchemy mix.
    private static bool CraftExists(string id) => GameBalance.Me.GetDataOrNull<CraftDef>(id) != null || GameBalance.GetCraftDefBase(id) != null;

    private static bool StationExists(string id) => GameBalance.Me.GetDataOrNull<WGODef>(id) != null;

    // Read from the cache directly, since Get logs a warning for every id it lacks.
    private static bool PartExists(string id)
    {
        var collection = LazySingletonSerializedSO<WgoPartBakedDataCollection>.Instance;
        collection.LoadCache();
        return _bakedParts(collection).ContainsKey(id);
    }

    private static FieldInfo[] Members(Type type)
    {
        if (_members.TryGetValue(type, out var found))
            return found;

        List<FieldInfo> fields = [];
        for (var level = type; level is not null && level != typeof(object); level = level.BaseType)
        {
            foreach (var field in level.GetFields(_fieldFlags))
            {
                var holdsItems = field.FieldType == typeof(Inventory) || field.FieldType == typeof(Item) || field.FieldType == typeof(List<Item>);
                var saved = !field.IsNotSerialized && (field.IsPublic || field.IsDefined(typeof(SerializeField), true) || field.IsDefined(typeof(OdinSerializeAttribute), true));
                if (holdsItems && saved)
                    fields.Add(field);
            }
        }

        return _members[type] = [.. fields];
    }

    private static string Pack<T>(T value) => Convert.ToBase64String(SerializationUtility.SerializeValue(value, DataFormat.Binary));

    private static T Unpack<T>(string blob) => SerializationUtility.DeserializeValue<T>(Convert.FromBase64String(blob), DataFormat.Binary);

    private static string Describe(List<TimbnStrippedEntry> entries) =>
        string.Join(", ", entries.GroupBy(entry => entry.Id).Select(group => group.Count() == 1 ? group.Key : $"{group.Key} x{group.Count()}"));

    /// <summary>Which ids of each kind come out of the save, as one predicate per kind.</summary>
    private sealed class Kinds(TimbnModOwned items, TimbnModOwned perks, TimbnModOwned crafts, TimbnModOwned stations, TimbnModOwned parts, TimbnModOwned quests)
    {
        public Func<string, bool> Part { get; } = parts.Takes;

        public Func<string, bool> Item { get; } = items.Takes;

        public Func<string, bool> Perk { get; } = perks.Takes;

        public Func<string, bool> Craft { get; } = crafts.Takes;

        public Func<string, bool> Station { get; } = stations.Takes;

        public Func<string, bool> Quest { get; } = quests.Takes;

        public static Kinds ForSave() => new(
            new TimbnModOwned(TimbnBalance.AddedIds<ItemDef>(), ItemExists, _emptyItemId),
            new TimbnModOwned(TimbnBalance.AddedIds<PerkDef>(), PerkExists),
            new TimbnModOwned(TimbnBalance.AddedIds<CraftDefBase>(), CraftExists),
            new TimbnModOwned(TimbnBalance.AddedIds<WGODef>(), StationExists),
            new TimbnModOwned(TimbnModels.AddedPartIds(), PartExists),
            new TimbnModOwned(TimbnBalance.AddedIds<QuestDef>(), QuestExists));

        public static Kinds Missing() => new(
            TimbnModOwned.Missing(ItemExists, _emptyItemId),
            TimbnModOwned.Missing(PerkExists),
            TimbnModOwned.Missing(CraftExists),
            TimbnModOwned.Missing(StationExists),
            TimbnModOwned.Missing(PartExists),
            TimbnModOwned.Missing(QuestExists));
    }
}
