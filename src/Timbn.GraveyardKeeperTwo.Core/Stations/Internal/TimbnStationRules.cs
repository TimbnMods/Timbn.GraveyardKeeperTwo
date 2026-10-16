namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The naming and defaults behind a buildable station or bench extension, following the game's own pattern of a
/// station id, a site id_place that turns into it, a building id_place_p, and removals id_r and id_place_r. Kept free
/// of game statics so it can be tested.
/// </summary>
internal static class TimbnStationRules
{
    internal const string DefaultBuildDesk = TimbnGameIds.BuildDesks.Yard;
    internal const string DefaultStationTemplate = TimbnGameIds.Workstations.Yard.WoodworkingWorkbench1;
    internal const string DefaultExtensionTemplate = TimbnGameIds.Workstations.Yard.ToolRack;
    internal const float DefaultReach = 0.64f;

    internal static string SiteId(string stationId) => stationId + "_place";

    internal static string BuildingId(string placedId) => placedId + "_p";

    internal static string RemovalId(string worldObjectId) => worldObjectId + "_r";

    internal static string DescriptionKey(string key) => key + "_d";

    /// <summary>
    /// Which game station's site, building and removal definitions a new station copies. The station it behaves like
    /// when the game builds that one from a build desk and lets the player dismantle it, otherwise the woodworking
    /// bench, since a station like the kitchen oven comes with the house and has no building of its own, and the
    /// resurrection table is built but never removed.
    /// </summary>
    internal static string BuildFamily(string behaviorTemplateId, Func<string, bool> hasBuilding) =>
        new[] { BuildingId(SiteId(behaviorTemplateId)), RemovalId(behaviorTemplateId), RemovalId(SiteId(behaviorTemplateId)) }.All(hasBuilding)
            ? behaviorTemplateId
            : DefaultStationTemplate;

    /// <summary>The game expression that lets E put an overhead zombie in a station's slot, the corpse pallet's own check, only while no craft runs on the station.</summary>
    internal const string HoldPutCondition = "PlayerHasOvrhdAndCanInsert() && IsNotCraftingNow()";

    /// <summary>
    /// What E does when it puts a zombie in, the corpse pallet's insert plus the game value its remove tool reads, so a
    /// station holding a zombie can't be dismantled with it inside.
    /// </summary>
    internal static readonly string[] HoldPut = ["InsertOvrhdItem()", "SetWGOPar(\"lock_building_removal\", 1)"];

    /// <summary>When E takes the zombie back, only with free hands and only while no craft runs on the station.</summary>
    internal const string HoldTakeCondition = "NoOvhdItem() && HasItemInInvByGroup(\"zombie\") && IsNotCraftingNow()";

    /// <summary>What E does when it takes the zombie back, the corpse pallet's take with the removal lock lifted.</summary>
    internal static readonly string[] HoldTake = ["TakeOvrhdItemByGroup(\"zombie\")", "SetWGOPar(\"lock_building_removal\", 0)"];

    /// <summary>Which game extension's building and removal definitions a new extension copies, the tool rack's when its own template has none.</summary>
    internal static string ExtensionFamily(string templateId, Func<string, bool> hasBuilding) =>
        hasBuilding(BuildingId(templateId)) ? templateId : DefaultExtensionTemplate;

    /// <summary>Half of each cost item rounded down, with items that round to nothing left out.</summary>
    internal static List<(string Item, int Count)> HalfRefund(IEnumerable<(string Item, int Count)> cost) =>
        Refund(cost.Select(item => (item.Item, item.Count / 2)));

    /// <summary>Only the refund items that give something back.</summary>
    internal static List<(string Item, int Count)> Refund(IEnumerable<(string Item, int Count)> refund) =>
        refund.Where(item => item.Count > 0).ToList();

    /// <summary>
    /// The build area a building is limited to. An area the mod named wins, the template's area is kept while the
    /// building goes on a build desk the template also uses, and any other build desk gets no area limit, since the template's
    /// area belongs to another zone.
    /// </summary>
    internal static string BuildArea(string buildDeskId, string? areaId, IReadOnlyList<string> templateBuildDesks, string templateArea) =>
        areaId ?? (templateBuildDesks.Contains(buildDeskId) ? templateArea : "");

    /// <summary>The text keys a station shows under, its own id, its site, its building and the building's description.</summary>
    internal static Dictionary<string, string> StationTexts(string id, string name, string description)
    {
        var buildingId = BuildingId(SiteId(id));
        return Texts((id, name), (SiteId(id), name), (buildingId, name), (DescriptionKey(buildingId), description));
    }

    /// <summary>The text keys an extension shows under, its own id, its building and the building's description.</summary>
    internal static Dictionary<string, string> ExtensionTexts(string id, string name, string description)
    {
        var buildingId = BuildingId(id);
        return Texts((id, name), (buildingId, name), (DescriptionKey(buildingId), description));
    }

    /// <summary>Text by key with the empty texts left out, so a lang file can supply them instead.</summary>
    internal static Dictionary<string, string> Texts(params (string Key, string Text)[] texts)
    {
        Dictionary<string, string> result = [];
        foreach (var (key, text) in texts)
        {
            if (!string.IsNullOrEmpty(text))
                result[key] = text;
        }

        return result;
    }
}
