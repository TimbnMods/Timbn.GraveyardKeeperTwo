namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds buildable workstations and bench extensions on behalf of a plugin, each offered at a build desk and built,
/// worked and dismantled like the game's own. Core makes every definition the game expects from a few lines, keeps a
/// built one out of the game's save so the save still loads once the plugin is gone, and removes everything when the
/// plugin unloads. Reach it through the plugin's Stations property.
/// </summary>
public sealed class TimbnPluginStations
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginStations(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a workstation the player builds from a build desk. Placing it puts down a construction site that the player works
    /// off with the hammer, then the station appears in its place, and the build desk's remove tool dismantles it for a
    /// refund. See <see cref="TimbnStationBuilder"/> for what a station can hold. Call it in OnAwake, before the crafts
    /// that run at it.
    /// </summary>
    /// <example>
    /// A box that splits boards into sticks, worked like the sawhorse and taking a tool rack.
    /// <code><![CDATA[
    /// var box = Stations.Add("timbn_stick_box", "Stick Box", "A red box for splitting boards into sticks.", station => station
    ///     .Model(bundle.Model("RedBox"), new Vector3(1f, 0.6f, 1f))
    ///     .Site(bundle.Model("RedBoxSite"))
    ///     .BasedOn("sawhorse")
    ///     .Icon("timbn_stick_box")
    ///     .Cost(("flitch", 2))
    ///     .Extensions("tool_rack"));
    /// ]]></code>
    /// </example>
    /// <param name="id">The station's world object id, for example "timbn_stick_box". Keep it stable, since saves name a built station by it.</param>
    /// <param name="name">The name shown in the build desk and over the station. Empty leaves the text to the plugin's lang files.</param>
    /// <param name="description">The description shown in the build desk. Empty leaves it to the lang files.</param>
    /// <param name="station">Describes the station.</param>
    /// <returns>
    /// The station, for naming it in a craft's In. It lives until the plugin unloads, so unlike an extension or a craft
    /// there is no handle to remove it early.
    /// </returns>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="InvalidOperationException">No model was given.</exception>
    public TimbnStation Add(string id, string name, string description, Action<TimbnStationBuilder> station)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("A station needs an id.", nameof(id));

        var builder = new TimbnStationBuilder(id, name, description);
        station(builder);
        _owner.Subscriptions.Add(TimbnStations.Add(builder));
        return new TimbnStation(id);
    }

    /// <summary>
    /// Adds a workstation whose name and build desk description come from the plugin's lang files. The station and its
    /// construction site are named under the station's id, and the build desk entry under its BuildingId with the
    /// description under BuildingId_d. See <see cref="Add(string, string, string, Action{TimbnStationBuilder})"/>.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// var box = Stations.Add("timbn_stick_box", station => station
    ///     .Model(bundle.Model("RedBox"), new Vector3(1f, 0.6f, 1f))
    ///     .BasedOn("sawhorse")
    ///     .Cost(("flitch", 2)));
    /// ]]></code>
    /// with lang/en.txt holding timbn_stick_box, timbn_stick_box_place, timbn_stick_box_place_p and
    /// timbn_stick_box_place_p_d.
    /// </example>
    /// <param name="id">The station's world object id, for example "timbn_stick_box". Keep it stable, since saves name a built station by it.</param>
    /// <param name="station">Describes the station.</param>
    /// <returns>The station, for naming it in a craft's In. It lives until the plugin unloads.</returns>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="InvalidOperationException">No model was given.</exception>
    public TimbnStation Add(string id, Action<TimbnStationBuilder> station) => Add(id, "", "", station);

    /// <summary>
    /// Adds a bench extension like the game's tool rack, placed straight from a build desk with no construction site. It
    /// attaches to a station that lists it in <see cref="TimbnStationBuilder.Extensions"/> when the two stand next to
    /// each other, and crafts that call NeedsExtension with its id open at that station while it is attached. See
    /// <see cref="TimbnExtensionBuilder"/> for what an extension can hold. Call it in OnAwake.
    /// </summary>
    /// <param name="id">The extension's world object id, for example "timbn_red_magic". Keep it stable, since saves name a built extension by it.</param>
    /// <param name="name">The name shown in the build desk. Empty leaves the text to the plugin's lang files.</param>
    /// <param name="description">The description shown in the build desk. Empty leaves it to the lang files.</param>
    /// <param name="extension">Describes the extension.</param>
    /// <returns>A handle that removes the extension early when disposed. You don't need to keep it.</returns>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="InvalidOperationException">No model was given.</exception>
    public IDisposable AddExtension(string id, string name, string description, Action<TimbnExtensionBuilder> extension)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("An extension needs an id.", nameof(id));

        var builder = new TimbnExtensionBuilder(id, name, description);
        extension(builder);
        return _owner.Subscriptions.Add(TimbnStations.AddExtension(builder));
    }

    /// <summary>
    /// Adds a bench extension whose name and build desk description come from the plugin's lang files, under the
    /// extension's id, its build desk entry id_p and the description id_p_d. See
    /// <see cref="AddExtension(string, string, string, Action{TimbnExtensionBuilder})"/>.
    /// </summary>
    /// <param name="id">The extension's world object id, for example "timbn_red_magic". Keep it stable, since saves name a built extension by it.</param>
    /// <param name="extension">Describes the extension.</param>
    /// <returns>A handle that removes the extension early when disposed. You don't need to keep it.</returns>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="InvalidOperationException">No model was given.</exception>
    public IDisposable AddExtension(string id, Action<TimbnExtensionBuilder> extension) => AddExtension(id, "", "", extension);
}
