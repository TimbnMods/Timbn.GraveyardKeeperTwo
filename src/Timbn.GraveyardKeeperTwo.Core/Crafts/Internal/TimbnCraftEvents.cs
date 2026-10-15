namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Tells a craft's handlers when the game starts or finishes one of its crafts. The game raises both from the station's
/// own craft hooks, once per unit of a craft with a count above one, and a timed craft that throws while finishing never
/// gets as far as the finish hook.
/// </summary>
internal static class TimbnCraftEvents
{
    private static readonly TimbnCraftEventRouter<TimbnCraftEvent> _started = new();
    private static readonly TimbnCraftEventRouter<TimbnCraftEvent> _finished = new();

    internal static IDisposable OnStarted(string craftId, Action<TimbnCraftEvent> handler) => _started.Add(craftId, handler);

    internal static IDisposable OnFinished(string craftId, Action<TimbnCraftEvent> handler) => _finished.Add(craftId, handler);

    internal static void Started(WgoData station, CraftElementBase craft) => Raise(_started, station, craft, "started");

    internal static void Finished(WgoData station, CraftElementBase craft) => Raise(_finished, station, craft, "finished");

    private static void Raise(TimbnCraftEventRouter<TimbnCraftEvent> router, WgoData station, CraftElementBase craft, string what)
    {
        if (router.IsEmpty)
            return;

        var handlers = router.HandlersFor(craft.CraftId);
        if (handlers.Count == 0)
            return;

        var craftEvent = new TimbnCraftEvent(station, craft.CraftId);
        foreach (var handler in handlers)
            TimbnSafe.Run(() => handler(craftEvent), TimbnCorePlugin.Logger, $"{nameof(TimbnCraftEvents)}|A handler for craft {craft.CraftId} {what}");
    }
}
