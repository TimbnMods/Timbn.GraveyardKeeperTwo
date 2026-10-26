namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Passes GK2 Move Buildings' moved hook on to the mod event bus as "moved", with the old and new ids of the building
/// as strings (oldId and newId). That mod publishes its hook list in the AppDomain under gk2.movebuilding.v1, so no
/// reference is needed. It is looked for each time a save is ready, since the mod may load after Core starts, and the
/// hook is added to the list that is there then. A reload of that mod reuses the same list, so the hook stays in it.
/// </summary>
internal static class TimbnMoveBuildingsLink
{
    internal const string Publisher = "gk2.movebuilding";
    internal const string EventId = "moved";

    private const string _key = "gk2.movebuilding.v1";

    private static List<Action<string, string>>? _hooks;
    private static IDisposable? _publisher;

    internal static void Check()
    {
        var hooks = AppDomain.CurrentDomain.GetData(_key) is Dictionary<string, object> record
            && record.TryGetValue("onMoved", out var list)
                ? list as List<Action<string, string>>
                : null;

        if (ReferenceEquals(hooks, _hooks))
            return;

        Detach();
        if (hooks == null)
            return;

        hooks.Add(OnMoved);
        _hooks = hooks;
        _publisher = TimbnModEventBus.AddPublisher(EventId, Publisher);
    }

    internal static void Detach()
    {
        _hooks?.Remove(OnMoved);
        _hooks = null;
        _publisher?.Dispose();
        _publisher = null;
    }

    private static void OnMoved(string oldId, string newId) =>
        TimbnModEventBus.Publish(EventId, new { oldId, newId }, TimbnCorePlugin.Logger);
}
