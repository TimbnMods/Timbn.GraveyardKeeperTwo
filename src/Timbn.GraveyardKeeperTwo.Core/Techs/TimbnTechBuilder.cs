namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a technology for the plugin's Techs.Add, a node on the game's tech tree that the player buys with tech
/// points. It needs <see cref="At"/> for its cell and <see cref="After"/> for the tech that comes before it. What the
/// tech unlocks is set from the other side, by the station's or craft's UnlockedBy with this tech's id.
/// </summary>
/// <example>
/// A tech on the anatomy tab, straight after Body Places II.
/// <code><![CDATA[
/// Techs.Add("timbn_infusion_rack", "Infusion Rack", tech => tech
///     .Tab(TechTreeTab.Anatomy)
///     .At(8, 2)
///     .After(TimbnGameIds.Techs.BodyPlaces2)
///     .Cost(red: 50, green: 50, blue: 20));
/// ]]></code>
/// </example>
public sealed class TimbnTechBuilder
{
    internal TimbnTechBuilder(string id, string name)
    {
        Id = id;
        Name = name;
    }

    internal string Id { get; }

    internal string Name { get; }

    internal TechTreeTab TabId { get; private set; } = TechTreeTab.Building;

    internal bool HasCell { get; private set; }

    internal float X { get; private set; }

    internal float Y { get; private set; }

    internal List<string> ParentIds { get; } = [];

    internal int Red { get; private set; }

    internal int Green { get; private set; }

    internal int Blue { get; private set; }

    internal List<string>? RowIds { get; private set; }

    /// <summary>Which tab of the tech tree holds the tech, the building tab when left alone.</summary>
    /// <param name="tab">The tab.</param>
    public TimbnTechBuilder Tab(TechTreeTab tab)
    {
        TabId = tab;
        return this;
    }

    /// <summary>
    /// The spot the tech sits at. The game puts the tech at that column and row of the tab and never moves anything
    /// over for it, so pick a spot nothing else uses. Every vanilla tab is five rows tall (row 0 to 4), and columns can
    /// grow as far right as needed. Half steps work too, the way the game's Graveyard Path and Fence I sits at row 0.5.
    /// </summary>
    /// <param name="column">The column, counted from the left (posX).</param>
    /// <param name="row">The row, counted from the bottom, so a bigger row is higher on the tree (posY).</param>
    public TimbnTechBuilder At(float column, float row)
    {
        X = column;
        Y = row;
        HasCell = true;
        return this;
    }

    /// <summary>The techs the player needs first, which the tree joins to this one with a line. The tech is open once every one of them is bought.</summary>
    /// <param name="techId">A tech's id, from <see cref="TimbnGameIds.Techs"/> or another plugin.</param>
    /// <param name="more">More techs' ids.</param>
    public TimbnTechBuilder After(string techId, params string[] more)
    {
        ParentIds.Add(techId);
        ParentIds.AddRange(more);
        return this;
    }

    /// <summary>
    /// The icons the tree and the tech's popup list, in this order, instead of everything the tech unlocks. The popup
    /// has room for three. A station added with Stations.Add (or any build desk entry) shows as the building, a craft
    /// with its recipe, and an item with no recipe and a line saying where it is crafted. An id the game doesn't have
    /// yet is skipped.
    /// </summary>
    /// <param name="id">A station's id, a build desk entry's id, an item's id or a craft's id.</param>
    /// <param name="more">More ids.</param>
    public TimbnTechBuilder Shows(string id, params string[] more)
    {
        RowIds ??= [];
        RowIds.Add(id);
        RowIds.AddRange(more);
        return this;
    }

    /// <summary>The tech points the tech costs, any of them left at zero.</summary>
    /// <param name="red">Red tech points.</param>
    /// <param name="green">Green tech points.</param>
    /// <param name="blue">Blue tech points.</param>
    public TimbnTechBuilder Cost(int red = 0, int green = 0, int blue = 0)
    {
        Red = red;
        Green = green;
        Blue = blue;
        return this;
    }
}
