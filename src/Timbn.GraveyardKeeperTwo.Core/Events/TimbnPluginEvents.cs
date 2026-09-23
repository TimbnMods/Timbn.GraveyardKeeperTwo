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

    internal TimbnPluginEvents(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Runs when a save has finished loading and gameplay starts, once the scene is loaded and
    /// MainGame.Instance, PlayerData, and WorldData are live. It does not run for a save that is already
    /// loaded when the plugin starts (a hot reload), so check TimbnGame.IsInGame in OnAwake for that case.
    /// </summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GameStarted(Action handler) => Own(TimbnGameEvents.GameStarted(handler));

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

    internal void AttachToSave()
    {
        foreach (var subscribe in _perSave.ToList())
            subscribe();
    }

    private IDisposable Own(IDisposable subscription) => _owner.Subscriptions.Add(subscription);

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
