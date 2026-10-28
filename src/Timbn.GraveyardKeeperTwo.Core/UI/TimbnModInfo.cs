namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds what a mod tells the Mods window about itself, returned from <see cref="TimbnFrameworkPlugin.ModInfo"/>.
/// Every field is optional. A mod that gives none gets the plain entry. The Nexus, Steam Workshop and Thunderstore
/// links must be https pages on that site. WebsiteLink can be any https page. Any other link is ignored with a
/// warning.
/// </summary>
public sealed class TimbnModInfo
{
    /// <summary>
    /// A name shared by a family of mods. The mod list shows one entry with that name, in the place of the first mod
    /// that names it, and picking it opens a page that lists each of the mods in it. A group of one mod is just that mod.
    /// </summary>
    public string? ModGroupName { get; set; }

    /// <summary>
    /// An id only the group's own mods give, such as a GUID made once for the group, so a mod that only shares the
    /// <see cref="ModGroupName"/> is not put in it. Mods with the same id make one group, under the first one's name. It is
    /// optional. Mods that give no id are grouped by <see cref="ModGroupName"/> alone. They never join a group that has an
    /// id. It does nothing without a <see cref="ModGroupName"/>.
    /// </summary>
    public string? ModGroupId { get; set; }

    /// <summary>
    /// Whether the mod is listed inside its <see cref="ModGroupName"/>. Turn it off to keep the mod as an entry of its
    /// own in the mod list, outside the group's page. It does nothing for a mod with no group.
    /// </summary>
    public bool ShowInGroup { get; set; } = true;

    /// <summary>A line about the mod, shown in its About card in the player's language.</summary>
    public string? Description { get; set; }

    /// <summary>Who made the mod, shown beside its version.</summary>
    public string? Author { get; set; }

    /// <summary>The mod's page on Nexus Mods (nexusmods.com).</summary>
    public string? NexusLink { get; set; }

    /// <summary>The mod's Steam Workshop page (steamcommunity.com).</summary>
    public string? SteamWorkshopLink { get; set; }

    /// <summary>The mod's page on Thunderstore (thunderstore.io).</summary>
    public string? ThunderstoreLink { get; set; }

    /// <summary>The mod's own page, such as its repository or documentation. Any https site.</summary>
    public string? WebsiteLink { get; set; }
}
