namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A buildable workstation added with the plugin's Stations.Add. Hand it to a craft's <see cref="TimbnCraftBuilder.In(TimbnStation)"/>
/// so the craft runs there. The ids follow the game's own naming, so a station timbn_smoking_rack has the construction
/// site timbn_smoking_rack_place and the build desk entry timbn_smoking_rack_place_p.
/// </summary>
public sealed class TimbnStation
{
    internal TimbnStation(string id)
    {
        Id = id;
        SiteId = TimbnStationRules.SiteId(id);
        BuildingId = TimbnStationRules.BuildingId(SiteId);
    }

    /// <summary>The finished station's world object id, which crafts name in craftsIn.</summary>
    public string Id { get; }

    /// <summary>The construction site's world object id, the outline the player works off with the hammer.</summary>
    public string SiteId { get; }

    /// <summary>The build desk entry's id, which the player's unlocked buildings and a tech's buildingsAfterUnlock name. The builder's UnlockedBy puts it on a tech for you.</summary>
    public string BuildingId { get; }
}
