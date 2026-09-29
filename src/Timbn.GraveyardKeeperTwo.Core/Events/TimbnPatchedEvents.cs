namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnPatchedEvents
{
    internal static event Action? SleepStarted;

    internal static event Action? SleepEnded;

    internal static event Action<GlobalEventsSystem.Event.Type, string>? Triggered;

    internal static event Action<GameBalance>? BalanceLoaded;

    internal static void RaiseSleepStarted() => SleepStarted?.Invoke();

    internal static void RaiseSleepEnded() => SleepEnded?.Invoke();

    internal static void RaiseTriggered(GlobalEventsSystem.Event.Type type, string id)
    {
        if (Triggered is { } triggered)
            triggered(type, id);
    }

    internal static void RaiseBalanceLoaded(GameBalance balance) => BalanceLoaded?.Invoke(balance);
}
