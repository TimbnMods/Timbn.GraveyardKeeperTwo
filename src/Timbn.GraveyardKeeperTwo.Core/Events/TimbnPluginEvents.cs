namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Subscribes a plugin to the game's events without any cleanup code. Every subscription is owned by the
/// plugin and removed when it unloads, so an F6 hot reload never leaves an old handler firing. Events that
/// belong to a loaded save (quests, the player's resources and inventory) can be subscribed once in OnAwake
/// and are attached to every save the player loads. Each handler is wrapped so a throw is logged instead of
/// reaching the game. Reach it through the plugin's Events property.
/// </summary>
public sealed class TimbnPluginEvents
{
    private readonly TimbnFrameworkPlugin _owner;
    private readonly List<Func<IDisposable>> _perSave = [];
    private readonly List<Action> _saveClosed = [];
    private readonly TimbnFrameHandlers _frameHandlers;

    internal TimbnPluginEvents(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
        _frameHandlers = new(owner.PluginLogger);
    }

    /// <summary>
    /// Runs when a save has finished loading and gameplay starts, once the scene is loaded and
    /// MainGame.Instance, PlayerData, and WorldData are live. It does not run for a save that is already
    /// loaded when the plugin starts (a hot reload), so check TimbnGame.IsInGame in OnAwake for that case.
    /// </summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GameStarted(Action handler) => Own(TimbnGameEvents.GameStarted(handler));

    /// <summary>
    /// Runs for every save the player plays. It runs straight away when a save is already loaded, which covers a
    /// plugin started by a hot reload mid game, and then on every GameStarted. Use it in place of subscribing to
    /// GameStarted and also checking TimbnGame.IsInGame in OnAwake.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.SaveReady(() => ZombieTint.PaintAll(reset: false));
    /// </code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SaveReady(Action handler)
    {
        var subscription = Own(TimbnGameEvents.GameStarted(handler));
        if (TimbnGame.IsInGame)
            RunSafely(handler);

        return subscription;
    }

    /// <summary>
    /// Runs when the player stops playing a save, whether they return to the main menu or the plugin unloads mid
    /// game, such as on a hot reload. It is the one place to put back what the plugin changed in the world for
    /// that save, in place of doing the same cleanup in both a GoToMainMenu handler and OnDestroyed. On unload it
    /// runs after OnDestroyed and before Core removes the plugin's other registrations.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.SaveClosed(meditation.Stop);
    /// </code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed, without running the handler. You don't need to keep it.</returns>
    public IDisposable SaveClosed(Action handler)
    {
        var goToMainMenu = TimbnGameEvents.GoToMainMenu(handler);
        _saveClosed.Add(handler);
        return Own(new TimbnUndo(() =>
        {
            _saveClosed.Remove(handler);
            goToMainMenu.Dispose();
        }));
    }

    /// <summary>Runs when the game leaves gameplay for the main menu, before the save is unloaded.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GoToMainMenu(Action handler) => Own(TimbnGameEvents.GoToMainMenu(handler));

    /// <summary>Runs when the game pauses, such as when the pause menu opens.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GamePaused(Action handler) => Own(TimbnGameEvents.GamePaused(handler));

    /// <summary>Runs when the game unpauses.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GameUnpaused(Action handler) => Own(TimbnGameEvents.GameUnpaused(handler));

    /// <summary>Runs when the clock wraps into a new day.</summary>
    /// <param name="handler">Gets the day number, counting up from 1 for the whole save.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable NewDayStarted(Action<int> handler) => Own(TimbnGameEvents.NewDayStarted(handler));

    /// <summary>Runs at the same moment as NewDayStarted, with the day's place in the week instead.</summary>
    /// <param name="handler">Gets the position in the six day week, 1 to 6.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable NewWeekdayStarted(Action<int> handler) => Own(TimbnGameEvents.NewWeekdayStarted(handler));

    /// <summary>Runs on every clock tick.</summary>
    /// <param name="handler">
    /// Gets the time of day, running from 0 to 1 over a day, and whether it is a preview time that is not
    /// saved.
    /// </param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable TimeOfDayChanged(Action<float, bool> handler) => Own(TimbnGameEvents.TimeOfDayChanged(handler));

    /// <summary>Runs when the game starts writing a save.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SaveWriteStarted(Action handler) => Own(TimbnGameEvents.SaveWriteStarted(handler));

    /// <summary>Runs once a save has been written.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SaveWriteEnded(Action handler) => Own(TimbnGameEvents.SaveWriteEnded(handler));

    /// <summary>Runs when a save starts reading from disk.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SaveLoadingStarted(Action handler) => Own(TimbnGameEvents.SaveLoadingStarted(handler));

    /// <summary>
    /// Runs when a save has finished reading from disk. The GameSave is filled in, but the scene is not
    /// loaded yet, so GameStarted is usually the better choice.
    /// </summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SaveLoadingEnded(Action handler) => Own(TimbnGameEvents.SaveLoadingEnded(handler));

    /// <summary>Runs when the player is moved by a teleport, which is also how every scene change happens.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerTeleported(Action handler) => Own(TimbnGameEvents.PlayerTeleported(handler));

    /// <summary>Runs when a world object's view spawns, as chunks stream in around the player.</summary>
    /// <param name="handler">Gets the spawned object's view. Its Data property is the world object.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable WorldObjectSpawned(Action<Wgo> handler) => Own(TimbnGameEvents.WorldObjectSpawned(handler));

    /// <summary>Runs when a world object's view is destroyed.</summary>
    /// <param name="handler">Gets the view that is going away.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable WorldObjectDestroyed(Action<Wgo> handler) => Own(TimbnGameEvents.WorldObjectDestroyed(handler));

    /// <summary>
    /// Runs when the player falls asleep, right after the game has gathered the loose resources around them and
    /// before the screen fades out. It does not run when the bed turns the player away for being rested.
    /// </summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SleepStarted(Action handler) =>
        Own(TimbnGameEvents.On(handler, h => TimbnPatchedEvents.SleepStarted += h, h => TimbnPatchedEvents.SleepStarted -= h));

    /// <summary>Runs when the player wakes up, as the screen starts to fade back in and just before the game saves.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SleepEnded(Action handler) =>
        Own(TimbnGameEvents.On(handler, h => TimbnPatchedEvents.SleepEnded += h, h => TimbnPatchedEvents.SleepEnded -= h));

    /// <summary>
    /// Runs when the game fires one of its story triggers, the signals its quests and cutscenes wait on, such as a
    /// building being built, an answer being picked, or a fight being won. The handler gets the trigger's id, such
    /// as the building or the answer, which is empty for triggers that have none.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Events.Trigger(GlobalEventsSystem.Event.Type.BuildBuilding, building => Logger.LogInfo($"Built {building}"));
    /// ]]></code>
    /// </example>
    /// <param name="type">The kind of trigger to listen for.</param>
    /// <param name="handler">Gets the trigger's id.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable Trigger(GlobalEventsSystem.Event.Type type, Action<string> handler) =>
        Own(TimbnGameEvents.On<GlobalEventsSystem.Event.Type, string>(
            (fired, id) =>
            {
                if (fired == type)
                    handler(id);
            },
            h => TimbnPatchedEvents.Triggered += h,
            h => TimbnPatchedEvents.Triggered -= h));

    /// <summary>
    /// Runs with the game's balance, where it keeps every item, craft, perk, and tech, each time it loads. It runs
    /// straight away when the balance is already loaded, then after every later load, once Core has put in the
    /// definitions plugins added. Use it to read the game's definitions. To change one, Balance.Edit also puts it
    /// back when the plugin unloads.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.BalanceLoaded(balance => Logger.LogInfo($"{balance.itemDefs.Count} items"));
    /// </code>
    /// </example>
    /// <param name="handler">Gets the loaded balance.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable BalanceLoaded(Action<GameBalance> handler)
    {
        var subscription = Own(TimbnGameEvents.On(handler, h => TimbnPatchedEvents.BalanceLoaded += h, h => TimbnPatchedEvents.BalanceLoaded -= h));
        if (TimbnBalance.Loaded is { } balance)
            RunSafely(() => handler(balance));

        return subscription;
    }

    /// <summary>Runs when a quest starts in the loaded save. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the quest's save data. Its id is the quest id.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable QuestStarted(Action<QuestData> handler) => PerSave(() => TimbnGameEvents.QuestStarted(handler));

    /// <summary>Runs when a quest completes in the loaded save. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the quest's save data. Its id is the quest id.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable QuestCompleted(Action<QuestData> handler) => PerSave(() => TimbnGameEvents.QuestCompleted(handler));

    /// <summary>Runs when a quest is cancelled in the loaded save. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the quest's save data. Its id is the quest id.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable QuestCanceled(Action<QuestData> handler) => PerSave(() => TimbnGameEvents.QuestCanceled(handler));

    /// <summary>
    /// Runs when any of the player's resources changes, money, energy, sanity, and reputation included.
    /// Subscribe once, and it follows every save the player loads.
    /// </summary>
    /// <param name="handler">Your handler. Read the values you care about from MainGame.PlayerData.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerResourceChanged(Action handler) => PerSave(() => TimbnGameEvents.PlayerResourceChanged(handler));

    /// <summary>Runs when the player picks up drops. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the items picked up.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerDropCollected(Action<List<Item>> handler) => PerSave(() => TimbnGameEvents.PlayerDropCollected(handler));

    /// <summary>Runs when the player uses an item, such as drinking a potion. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the item used.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerItemUsed(Action<Item> handler) => PerSave(() => TimbnGameEvents.PlayerItemUsed(handler));

    /// <summary>Runs when items go into the player's inventory. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the items added.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerItemsAdded(Action<List<Item>> handler) => PerSave(() => TimbnGameEvents.PlayerItemsAdded(handler));

    /// <summary>Runs when items leave the player's inventory. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the items removed.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerItemsRemoved(Action<List<Item>> handler) => PerSave(() => TimbnGameEvents.PlayerItemsRemoved(handler));

    /// <summary>
    /// Runs on every frame while a save is loaded, paused or not. It never runs on the main menu. Prefer
    /// <see cref="Every"/> for work that does not need every frame or should stop while the game is paused. If the
    /// handler throws, the error is logged once and the handler is stopped instead of failing again every frame.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.Update(() => stuckCarriers.Tick());
    /// </code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable Update(Action handler) => Own(_frameHandlers.Add(handler, 0f, whilePaused: true));

    /// <summary>
    /// Runs at most once every <paramref name="seconds"/> while a save is loaded and the game is not paused, and it
    /// starts on the first frame after it is added. The seconds are real time, so they do not speed up while the
    /// player sleeps. It never runs on the main menu. If the handler throws, the error is logged once and the
    /// handler is stopped.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.Every(0.5f, () => stuckCarriers.Tick());
    /// </code>
    /// </example>
    /// <param name="seconds">The shortest time between runs. Zero or less runs on every frame.</param>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable Every(float seconds, Action handler) => Own(_frameHandlers.Add(handler, seconds, whilePaused: false));

    /// <summary>
    /// Subscribes to a static game event that has no named method here. It works exactly like the named
    /// methods. The handler is wrapped so a throw is logged instead of reaching the game, the subscription is
    /// owned by the plugin, and it is removed when the plugin unloads, so there is nothing to clean up. It
    /// hooks the event once, so use it only for static events. An event on an object that belongs to a save
    /// (quests, the player's data) needs a named method, which reattaches it for each save.
    /// </summary>
    /// <example>
    /// Log every tech the player unlocks, using the game's static KnowledgeSystem.OnTechUnlocked event, which
    /// passes the tech id and whether the unlock was silent.
    /// <code><![CDATA[
    /// Events.StaticEvent<string, bool>(
    ///     (techId, silent) => Logger.LogInfo($"Unlocked {techId}"),
    ///     h => KnowledgeSystem.OnTechUnlocked += h,
    ///     h => KnowledgeSystem.OnTechUnlocked -= h);
    /// ]]></code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <param name="add">Subscribes a handler to the event, such as <c><![CDATA[h => KnowledgeSystem.OnTechUnlocked += h]]></c>.</param>
    /// <param name="remove">Unsubscribes it again, such as <c><![CDATA[h => KnowledgeSystem.OnTechUnlocked -= h]]></c>.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable StaticEvent(Action handler, Action<Action> add, Action<Action> remove) =>
        Own(TimbnGameEvents.On(handler, add, remove));

    /// <inheritdoc cref="StaticEvent(Action, Action{Action}, Action{Action})"/>
    public IDisposable StaticEvent<T>(Action<T> handler, Action<Action<T>> add, Action<Action<T>> remove) =>
        Own(TimbnGameEvents.On(handler, add, remove));

    /// <inheritdoc cref="StaticEvent(Action, Action{Action}, Action{Action})"/>
    public IDisposable StaticEvent<T1, T2>(Action<T1, T2> handler, Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove) =>
        Own(TimbnGameEvents.On(handler, add, remove));

    internal void RunFrameHandlers() => _frameHandlers.Run();

    internal void CloseSave()
    {
        if (!TimbnGame.IsInGame)
            return;

        foreach (var handler in _saveClosed.ToList())
            RunSafely(handler);
    }

    internal void AttachToSave()
    {
        foreach (var subscribe in _perSave.ToList())
            subscribe();
    }

    private IDisposable Own(IDisposable subscription) => _owner.Subscriptions.Add(subscription);

    private void RunSafely(Action handler)
    {
        try
        {
            handler();
        }
        catch (Exception ex)
        {
            _owner.PluginLogger.LogError($"{nameof(TimbnPluginEvents)}|{handler.Method.DeclaringType?.FullName}.{handler.Method.Name} threw: {ex}");
        }
    }

    private IDisposable PerSave(Func<IDisposable> subscribe)
    {
        IDisposable? current = null;
        IDisposable attach() => current = _owner.SessionSubscriptions.Add(subscribe());
        Func<IDisposable> entry = attach;
        _perSave.Add(entry);
        if (TimbnGame.IsInGame)
            attach();

        return _owner.Subscriptions.Add(new TimbnUndo(() =>
        {
            _perSave.Remove(entry);
            current?.Dispose();
        }));
    }
}
