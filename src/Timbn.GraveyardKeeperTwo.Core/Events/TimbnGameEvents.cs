namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnGameEvents
{
    internal static IDisposable GameStarted(Action handler) =>
        On(handler, h => MainGame.OnGameStarted += h, h => MainGame.OnGameStarted -= h);

    internal static IDisposable GoToMainMenu(Action handler) =>
        On(handler, h => MainGame.OnGoToMainMenu += h, h => MainGame.OnGoToMainMenu -= h);

    internal static IDisposable GamePaused(Action handler) =>
        On(handler, h => MainGame.OnGamePaused += h, h => MainGame.OnGamePaused -= h);

    internal static IDisposable GameUnpaused(Action handler) =>
        On(handler, h => MainGame.OnGameUnpaused += h, h => MainGame.OnGameUnpaused -= h);

    internal static IDisposable NewDayStarted(Action<int> handler) =>
        On(handler, h => EnvironmentEngine.OnNewDayStarted += h, h => EnvironmentEngine.OnNewDayStarted -= h);

    internal static IDisposable NewWeekdayStarted(Action<int> handler) =>
        On(handler, h => EnvironmentEngine.OnNewDayStartedWithDayNumber += h, h => EnvironmentEngine.OnNewDayStartedWithDayNumber -= h);

    internal static IDisposable TimeOfDayChanged(Action<float, bool> handler) =>
        On(handler, h => EnvironmentEngine.OnTimeOfDayChangedEvent += h, h => EnvironmentEngine.OnTimeOfDayChangedEvent -= h);

    internal static IDisposable SaveWriteStarted(Action handler) =>
        On(handler, h => SaveSystem.OnSaveWriteStarted += h, h => SaveSystem.OnSaveWriteStarted -= h);

    internal static IDisposable SaveWriteEnded(Action handler) =>
        On(handler, h => SaveSystem.OnSaveWriteEnded += h, h => SaveSystem.OnSaveWriteEnded -= h);

    internal static IDisposable SaveLoadingStarted(Action handler) =>
        On(handler, h => SaveSystem.OnSaveLoadingStarted += h, h => SaveSystem.OnSaveLoadingStarted -= h);

    internal static IDisposable SaveLoadingEnded(Action handler) =>
        On(handler, h => SaveSystem.OnSaveLoadingEnded += h, h => SaveSystem.OnSaveLoadingEnded -= h);

    internal static IDisposable PlayerTeleported(Action handler) =>
        On(handler, h => PlayerController.OnPlayerTeleported += h, h => PlayerController.OnPlayerTeleported -= h);

    internal static IDisposable WorldObjectSpawned(Action<Wgo> handler) =>
        On(handler, h => Wgo.OnWgoSpawn += h, h => Wgo.OnWgoSpawn -= h);

    internal static IDisposable WorldObjectDestroyed(Action<Wgo> handler) =>
        On(handler, h => Wgo.OnWgoDestroy += h, h => Wgo.OnWgoDestroy -= h);

    internal static IDisposable QuestStarted(Action<QuestData> handler)
    {
        var quests = MainGame.Instance.GameSave.questSystemData;
        return On(handler, h => quests.OnQuestStarted += h, h => quests.OnQuestStarted -= h);
    }

    internal static IDisposable QuestCompleted(Action<QuestData> handler)
    {
        var quests = MainGame.Instance.GameSave.questSystemData;
        return On(handler, h => quests.OnQuestCompleted += h, h => quests.OnQuestCompleted -= h);
    }

    internal static IDisposable QuestCanceled(Action<QuestData> handler)
    {
        var quests = MainGame.Instance.GameSave.questSystemData;
        return On(handler, h => quests.OnQuestCanceled += h, h => quests.OnQuestCanceled -= h);
    }

    internal static IDisposable PlayerResourceChanged(Action handler)
    {
        var player = MainGame.PlayerData;
        return On(handler, h => player.OnGameResChanged += h, h => player.OnGameResChanged -= h);
    }

    internal static IDisposable PlayerDropCollected(Action<List<Item>> handler)
    {
        var player = MainGame.PlayerData;
        return On(handler, h => player.OnDropCollected += h, h => player.OnDropCollected -= h);
    }

    internal static IDisposable PlayerItemUsed(Action<Item> handler)
    {
        var player = MainGame.PlayerData;
        return On(handler, h => player.OnItemUsed += h, h => player.OnItemUsed -= h);
    }

    internal static IDisposable PlayerItemsAdded(Action<List<Item>> handler)
    {
        var inventory = MainGame.PlayerData.inventory;
        return On(handler, h => inventory.OnItemsAdd += h, h => inventory.OnItemsAdd -= h);
    }

    internal static IDisposable PlayerItemsRemoved(Action<List<Item>> handler)
    {
        var inventory = MainGame.PlayerData.inventory;
        return On(handler, h => inventory.OnItemsRemove += h, h => inventory.OnItemsRemove -= h);
    }

    internal static IDisposable On(Action handler, Action<Action> add, Action<Action> remove)
    {
        void safe()
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                LogHandlerError(handler, ex);
            }
        }

        add(safe);
        return new TimbnUndo(() => remove(safe));
    }

    internal static IDisposable On<T>(Action<T> handler, Action<Action<T>> add, Action<Action<T>> remove)
    {
        void safe(T arg)
        {
            try
            {
                handler(arg);
            }
            catch (Exception ex)
            {
                LogHandlerError(handler, ex);
            }
        }

        add(safe);
        return new TimbnUndo(() => remove(safe));
    }

    internal static IDisposable On<T1, T2>(Action<T1, T2> handler, Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove)
    {
        void safe(T1 first, T2 second)
        {
            try
            {
                handler(first, second);
            }
            catch (Exception ex)
            {
                LogHandlerError(handler, ex);
            }
        };

        add(safe);
        return new TimbnUndo(() => remove(safe));
    }

    private static void LogHandlerError(Delegate handler, Exception ex) =>
        TimbnCorePlugin.Logger.LogError($"{nameof(TimbnGameEvents)}|{handler.Method.DeclaringType?.FullName}.{handler.Method.Name} threw: {ex}");
}
