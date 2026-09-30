namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Subscribes to the game's static events with handlers wrapped so a throw is logged, for the few events Core itself needs.</summary>
internal static class TimbnGameEvents
{
    internal static IDisposable GameStarted(Action handler) =>
        On(handler, h => MainGame.OnGameStarted += h, h => MainGame.OnGameStarted -= h);

    internal static IDisposable GoToMainMenu(Action handler) =>
        On(handler, h => MainGame.OnGoToMainMenu += h, h => MainGame.OnGoToMainMenu -= h);

    internal static IDisposable SaveWriteEnded(Action handler) =>
        On(handler, h => SaveSystem.OnSaveWriteEnded += h, h => SaveSystem.OnSaveWriteEnded -= h);

    internal static IDisposable QuestStarted(Action<QuestData> handler)
    {
        var quests = MainGame.Instance.GameSave.questSystemData;
        return On(handler, h => quests.OnQuestStarted += h, h => quests.OnQuestStarted -= h);
    }

    internal static IDisposable On(Action handler, Action<Action> add, Action<Action> remove)
    {
        void safe() => TimbnSafe.Run(handler, Describe(handler));
        add(safe);
        return new TimbnUndo(() => remove(safe));
    }

    internal static IDisposable On<T>(Action<T> handler, Action<Action<T>> add, Action<Action<T>> remove)
    {
        void safe(T arg) => TimbnSafe.Run(() => handler(arg), Describe(handler));
        add(safe);
        return new TimbnUndo(() => remove(safe));
    }

    internal static IDisposable On<T1, T2>(Action<T1, T2> handler, Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove)
    {
        void safe(T1 first, T2 second) => TimbnSafe.Run(() => handler(first, second), Describe(handler));
        add(safe);
        return new TimbnUndo(() => remove(safe));
    }

    private static string Describe(Delegate handler) => $"{nameof(TimbnGameEvents)}|{TimbnSafe.Describe(handler)}";
}
