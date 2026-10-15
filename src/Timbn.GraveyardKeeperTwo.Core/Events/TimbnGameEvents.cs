namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnGameEvents
{
    private static readonly Relay _gameStarted = new(h => MainGame.OnGameStarted += h, h => MainGame.OnGameStarted -= h);
    private static readonly Relay _goToMainMenu = new(h => MainGame.OnGoToMainMenu += h, h => MainGame.OnGoToMainMenu -= h);

    // The game adds its own handlers to these during MainGame.Start, which runs after plugins load, so a plain += would
    // put a plugin ahead of them (Bubble clears every speech bubble on game start, the notifier is not listening yet).
    internal static IDisposable GameStarted(Action handler) => On(handler, _gameStarted.Add, _gameStarted.Remove);

    internal static IDisposable GoToMainMenu(Action handler) => On(handler, _goToMainMenu.Add, _goToMainMenu.Remove);

    /// <summary>Moves the relays behind every handler the game added while it started.</summary>
    internal static void OnMainGameStarted()
    {
        _gameStarted.MoveToEnd();
        _goToMainMenu.MoveToEnd();
    }

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

    private sealed class Relay(Action<Action> attach, Action<Action> detach)
    {
        private readonly List<Action> _handlers = [];
        private bool _attached;

        public void Add(Action handler)
        {
            _handlers.Add(handler);
            if (_attached)
                return;

            attach(Raise);
            _attached = true;
        }

        public void Remove(Action handler) => _handlers.Remove(handler);

        public void MoveToEnd()
        {
            if (!_attached)
                return;

            detach(Raise);
            attach(Raise);
        }

        private void Raise()
        {
            foreach (var handler in _handlers.ToList())
                handler();
        }
    }

    private static string Describe(Delegate handler) => $"{nameof(TimbnGameEvents)}|{TimbnSafe.Describe(handler)}";
}
