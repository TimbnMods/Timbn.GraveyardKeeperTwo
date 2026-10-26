namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A way for mods to tell each other things through Core without a reference to each other's DLL. A mod publishes
/// an id with some plain data, and any mod that listens to that id is called once with it. Delivery is one way, right
/// away, to whoever listens at that moment, with no history for a mod that starts listening later. Ids are plain
/// strings with no namespace, and any number of mods can publish the same one. Everything a plugin adds here is taken
/// away when it unloads. Reach it through the plugin's ModEvents property.
/// </summary>
/// <remarks>
/// The payload is sent as JSON, so keep it to plain data (strings, numbers, true and false, lists and nested
/// objects), and send ids, not game objects. A listener never needs the publisher's DLL. It writes its own small
/// class with the same property names, which are matched ignoring case, and fields it leaves out stay at their
/// default. The shape is the publisher's to document, and a publisher should only ever add fields, since a rename
/// breaks every listener.
/// </remarks>
public sealed class TimbnPluginModEvents
{
    private readonly TimbnFrameworkPlugin _owner;
    private readonly HashSet<string> _published = [];

    internal TimbnPluginModEvents(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>Every id mods have published or listened to, with the mods publishing it.</summary>
    public IReadOnlyList<TimbnModEvent> Known => TimbnModEventBus.Known;

    /// <summary>
    /// Publishes data under an id, calling every mod listening to it once, straight away. The first time this plugin
    /// publishes an id it joins that id's publishing mods, until it unloads.
    /// </summary>
    /// <example>
    /// <code>
    /// ModEvents.Publish("moved", new { oldId = oldChest, newId = newChest });
    /// </code>
    /// </example>
    /// <param name="id">The id to publish under.</param>
    /// <param name="payload">Plain data to send, or null for an event with nothing in it.</param>
    public void Publish(string id, object? payload = null)
    {
        if (_published.Add(id))
            _owner.Subscriptions.Add(TimbnModEventBus.AddPublisher(id, _owner.Metadata.GUID));

        TimbnModEventBus.Publish(id, payload, _owner.PluginLogger);
    }

    /// <summary>
    /// Calls the handler once for each event published under the id from now on, with the data read into your own
    /// class. A payload that does not fit the class is logged and skipped, and an event published with nothing in it is
    /// skipped too, so listen with <see cref="Listen(string, Action)"/> for an id that carries no data.
    /// </summary>
    /// <example>
    /// <code>
    /// ModEvents.Listen&lt;Moved&gt;("moved", moved => Rekey(moved.OldId, moved.NewId));
    /// </code>
    /// </example>
    /// <param name="id">The id to listen to. It does not need to exist yet.</param>
    /// <param name="handler">Runs for each event.</param>
    /// <returns>A handle that stops listening when disposed. It also stops when the plugin unloads, so you don't need to keep it.</returns>
    public IDisposable Listen<T>(string id, Action<T> handler) =>
        _owner.Subscriptions.Add(TimbnModEventBus.Listen(id, handler, _owner.PluginLogger));

    /// <summary>
    /// Calls the handler once for each event published under the id from now on, whatever it carries, for an event that is
    /// only a signal, such as a mod saying it finished loading its content.
    /// </summary>
    /// <example>
    /// <code>
    /// ModEvents.Listen("my_pack_loaded", RefreshShop);
    /// </code>
    /// </example>
    /// <param name="id">The id to listen to. It does not need to exist yet.</param>
    /// <param name="handler">Runs for each event.</param>
    /// <returns>A handle that stops listening when disposed. It also stops when the plugin unloads, so you don't need to keep it.</returns>
    public IDisposable Listen(string id, Action handler) =>
        _owner.Subscriptions.Add(TimbnModEventBus.Listen(id, handler, _owner.PluginLogger));

    /// <summary>Calls the handler once for each event published under the id from now on, with the payload as the JSON text it was sent as, which is "null" for an event with nothing in it.</summary>
    /// <param name="id">The id to listen to. It does not need to exist yet.</param>
    /// <param name="handler">Runs for each event.</param>
    /// <returns>A handle that stops listening when disposed. It also stops when the plugin unloads, so you don't need to keep it.</returns>
    public IDisposable Listen(string id, Action<string> handler) =>
        _owner.Subscriptions.Add(TimbnModEventBus.Listen(id, handler, _owner.PluginLogger));
}
