namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds crafts on behalf of a plugin, at the game's own stations or at ones added with Stations.Add. Each craft is
/// copied from one of the game's, shows up in the station's craft window straight away, and is removed when the plugin
/// unloads. Core keeps a queued one out of the game's save so the save still loads once the plugin is gone. Reach it
/// through the plugin's Crafts property.
/// </summary>
public sealed class TimbnPluginCrafts
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginCrafts(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a craft. See <see cref="TimbnCraftBuilder"/> for what it can hold. The craft window titles the craft with the
    /// name and shows the description in its tooltip. Call it in OnAwake, after the station it runs at when that is a
    /// station of the plugin's own.
    /// </summary>
    /// <example>
    /// Smoked fish from a river fillet and a log at the smoking rack, copied from the oven's baked fish.
    /// <code><![CDATA[
    /// Crafts.Add("timbn_smoke_fillet_fish_river", "Smoked Fish", "A fish fillet and a log of firewood make one smoked fish.", craft => craft
    ///     .In(rack)
    ///     .BasedOn("cook_baked_fish")
    ///     .Needs(("fillet_fish_river", 1), ("firewood", 1))
    ///     .Makes("timbn_smoked_fish")
    ///     .AutoFinish());
    /// ]]></code>
    /// </example>
    /// <param name="id">The craft's id, for example "timbn_smoke_fillet_fish_river". Keep it stable, since a save keeps a queued craft under it.</param>
    /// <param name="name">The name shown in the craft window. Empty leaves the text to the plugin's lang files.</param>
    /// <param name="description">The description shown in the craft's tooltip. Empty leaves it to the lang files.</param>
    /// <param name="craft">Describes the craft.</param>
    /// <returns>A handle that removes the craft early when disposed. You don't need to keep it.</returns>
    /// <exception cref="ArgumentException">The id is empty, or a count expression doesn't parse.</exception>
    public IDisposable Add(string id, string name, string description, Action<TimbnCraftBuilder> craft)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("A craft needs an id.", nameof(id));

        var builder = new TimbnCraftBuilder(id, name, description);
        craft(builder);
        return _owner.Subscriptions.Add(TimbnCrafts.Add(builder));
    }

    /// <summary>
    /// Adds a craft whose name and description come from the plugin's lang files, under the craft's id and id_d. See
    /// <see cref="Add(string, string, string, Action{TimbnCraftBuilder})"/>.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Crafts.Add("timbn_smoke_fillet_fish_river", craft => craft
    ///     .In(rack)
    ///     .BasedOn("cook_baked_fish")
    ///     .Needs(("fillet_fish_river", 1), ("firewood", 1))
    ///     .Makes("timbn_smoked_fish"));
    /// ]]></code>
    /// with lang/en.txt holding timbn_smoke_fillet_fish_river and timbn_smoke_fillet_fish_river_d.
    /// </example>
    /// <param name="id">The craft's id, for example "timbn_smoke_fillet_fish_river". Keep it stable, since a save keeps a queued craft under it.</param>
    /// <param name="craft">Describes the craft.</param>
    /// <returns>A handle that removes the craft early when disposed. You don't need to keep it.</returns>
    /// <exception cref="ArgumentException">The id is empty, or a count expression doesn't parse.</exception>
    public IDisposable Add(string id, Action<TimbnCraftBuilder> craft) => Add(id, "", "", craft);

    /// <summary>
    /// Starts a craft at a station from code, with no craft window, the way the game's own StartCraft expression puts
    /// the berries on a bush. The craft goes in the station's queue and runs like one the player started, so a station
    /// that already runs one starts it after. End it early with the game's
    /// station.CraftComponent.TryFinishCurCraft(), which finishes it as if its time ran out.
    /// </summary>
    /// <param name="station">The station, for example one found with <see cref="TimbnWorld.FindAll(string)"/>.</param>
    /// <param name="craftId">The craft's id. It has to be one made at that station.</param>
    /// <returns>False when the game has no such craft.</returns>
    public bool Start(WgoData station, string craftId)
    {
        if (GameBalance.GetCraftDef(craftId) == null)
            return false;

        station.CraftComponent.AddToQueue(new CraftElement(craftId, 1, new CraftParamsData(craftId, station)));
        return true;
    }
}
