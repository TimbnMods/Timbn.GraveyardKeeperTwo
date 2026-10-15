namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnPatchedEvents
{
    internal static event Action? SleepStarted;

    internal static event Action? SleepEnded;

    internal static event Action<bool>? SleepRefused;

    internal static event Action<GlobalEventsSystem.Event.Type, string>? Triggered;

    internal static event Action<GameBalance>? BalanceLoaded;

    internal static event Action<WgoPart, ZombieWgoData>? ZombieViewReady;

    internal static void RaiseSleepStarted() => SleepStarted?.Invoke();

    internal static void RaiseSleepEnded() => SleepEnded?.Invoke();

    internal static void RaiseSleepRefused(bool wouldSave) => SleepRefused?.Invoke(wouldSave);

    internal static void RaiseTriggered(GlobalEventsSystem.Event.Type type, string id) => Triggered?.Invoke(type, id);

    internal static void RaiseBalanceLoaded(GameBalance balance) => BalanceLoaded?.Invoke(balance);

    internal static void RaiseZombieViewReady(WgoPart part, ZombieWgoData zombie) => ZombieViewReady?.Invoke(part, zombie);
}
