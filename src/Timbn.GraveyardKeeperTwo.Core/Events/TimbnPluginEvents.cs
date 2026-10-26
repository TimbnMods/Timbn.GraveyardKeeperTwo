using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Subscribes a plugin to the game's events without any cleanup code. Every subscription is owned by the
/// plugin and removed when it unloads, so an F6 hot reload never leaves an old handler firing. The named events
/// are the ones Core adds something to and the ones mods commonly need, such as a new day or the player using an
/// item, and those that belong to a save follow every save the player loads. Any other game event is one line of
/// <see cref="GameEvent(Action, Action{Action}, Action{Action})"/>,
/// or of <see cref="SaveEvent(Action, Action{Action}, Action{Action})"/> for an event on an object that belongs to
/// the loaded save. Each handler is wrapped so a throw is logged instead of reaching the game. Reach it through the
/// plugin's Events property.
/// </summary>
public sealed class TimbnPluginEvents
{
    private readonly TimbnFrameworkPlugin _owner;
    private readonly List<Func<IDisposable>> _perSave = [];
    private readonly List<Action> _saveClosed = [];
    private readonly TimbnFrameHandlers _frameHandlers;
    private readonly TimbnFrameHandlers _lateHandlers;
    private readonly TimbnFrameHandlers _fixedHandlers;

    internal TimbnPluginEvents(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
        var keep = owner is TimbnCorePlugin;
        _frameHandlers = new(owner.PluginLogger, "An Update", keep);
        _lateHandlers = new(owner.PluginLogger, "A LateUpdate", keep);
        _fixedHandlers = new(owner.PluginLogger, "A FixedUpdate", keep);
    }

    /// <summary>
    /// Runs for every save the player plays, once the scene is loaded and MainGame.Instance, PlayerData and
    /// WorldData are live. It runs straight away when a save is already loaded, which covers a plugin started by a
    /// hot reload mid game, and then each time a save finishes loading.
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
    /// that save. On unload it runs after OnUnload and before Core removes the plugin's other registrations.
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
        var closing = TimbnGameEvents.On(handler, h => TimbnSession.Closing += h, h => TimbnSession.Closing -= h);
        _saveClosed.Add(handler);
        return Own(new TimbnUndo(() =>
        {
            _saveClosed.Remove(handler);
            closing.Dispose();
        }));
    }

    /// <summary>
    /// Runs just before the game writes the save you are playing, ahead of Core taking out the mod content it keeps
    /// out of the file. Use it to put back what a mod changed in the world that the save would otherwise keep, such as
    /// a visitor standing in the graveyard, and bring it back in <see cref="AfterSave"/>. Core strips its own content
    /// after every plugin's handler has run, so nothing a handler leaves behind reaches the file. It runs for every
    /// write of the live save.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.BeforeSave(visits.SendAway);
    /// </code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable BeforeSave(Action handler) =>
        Own(TimbnGameEvents.On(handler, h => TimbnSaveEvents.Before += h, h => TimbnSaveEvents.Before -= h));

    /// <summary>
    /// Runs once the save has been written, or has failed to write, after Core has put its own content back. Use it
    /// to redo what you undid in <see cref="BeforeSave"/>. It runs for every write the handlers of
    /// <see cref="BeforeSave"/> ran for.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.AfterSave(visits.Return);
    /// </code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable AfterSave(Action handler) =>
        Own(TimbnGameEvents.On(handler, h => TimbnSaveEvents.After += h, h => TimbnSaveEvents.After -= h));

    /// <summary>
    /// Runs when a zombie's view has been given its body, head, and skin, right after the view spawns and again
    /// whenever the zombie is restyled. Use it to change how a zombie looks, since the game redraws the view from
    /// its data and would undo a change made at any other time.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.ZombieViewReady((part, zombie) => ZombieTint.Paint(part, ZombieTint.Read(zombie)));
    /// </code>
    /// </example>
    /// <param name="handler">Gets the part of the view that draws the zombie, whose Wgo is the view, and the zombie.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable ZombieViewReady(Action<WgoPart, ZombieWgoData> handler) =>
        Static(handler, h => TimbnPatchedEvents.ZombieViewReady += h, h => TimbnPatchedEvents.ZombieViewReady -= h);

    /// <summary>
    /// Runs when the player falls asleep, right after the game has gathered the loose resources around them and
    /// as the sleep fade begins. It does not run when the bed turns the player away for being rested, see
    /// <see cref="SleepRefused"/>.
    /// </summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SleepStarted(Action handler) =>
        Static(handler, h => TimbnPatchedEvents.SleepStarted += h, h => TimbnPatchedEvents.SleepStarted -= h);

    /// <summary>
    /// Runs when the player tries to sleep with full energy and the bed turns them away, just before the game's own
    /// refusal handling runs.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.SleepRefused(wouldSave => { if (wouldSave) TimbnGame.SaveNow(); });
    /// </code>
    /// </example>
    /// <param name="handler">Gets whether the game would have saved when this sleep ended, which it does unless the sleep was started with its skip save flag.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SleepRefused(Action<bool> handler) =>
        Static(handler, h => TimbnPatchedEvents.SleepRefused += h, h => TimbnPatchedEvents.SleepRefused -= h);

    /// <summary>Runs when the player wakes up, as the screen starts to fade back in and right after the game writes its wake up save.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SleepEnded(Action handler) =>
        Static(handler, h => TimbnPatchedEvents.SleepEnded += h, h => TimbnPatchedEvents.SleepEnded -= h);

    /// <summary>
    /// Runs when the game fires one of its story triggers, the signals its quests and cutscenes wait on, such as a
    /// building being built, an answer being picked, or a fight being won. The game has no event of its own for these,
    /// so Core raises it.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Events.StoryTrigger(GlobalEventsSystem.Event.Type.BuildBuilding, building => Logger.LogInfo($"Built {building}"));
    /// ]]></code>
    /// </example>
    /// <param name="type">The kind of trigger to listen for.</param>
    /// <param name="handler">Gets the trigger's id, such as the building or the answer, empty for triggers that have none.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable StoryTrigger(GlobalEventsSystem.Event.Type type, Action<string> handler) =>
        Static<GlobalEventsSystem.Event.Type, string>(
            (fired, id) =>
            {
                if (fired == type)
                    handler(id);
            },
            h => TimbnPatchedEvents.Triggered += h,
            h => TimbnPatchedEvents.Triggered -= h);

    /// <summary>Runs when the game pauses, such as when the pause menu opens.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GamePaused(Action handler) =>
        Static(handler, h => MainGame.OnGamePaused += h, h => MainGame.OnGamePaused -= h);

    /// <summary>Runs when the game unpauses.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GameUnpaused(Action handler) =>
        Static(handler, h => MainGame.OnGameUnpaused += h, h => MainGame.OnGameUnpaused -= h);

    /// <summary>Runs at the same moment as NewDayStarted, with the day's place in the week instead.</summary>
    /// <param name="handler">Gets the day's place in the six day week.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable NewWeekdayStarted(Action<TimbnWeekday> handler) =>
        Static<int>(
            number =>
            {
                if (TimbnClock.Weekday is { } weekday)
                    handler(weekday);
            },
            h => EnvironmentEngine.OnNewDayStartedWithDayNumber += h,
            h => EnvironmentEngine.OnNewDayStartedWithDayNumber -= h);

    /// <summary>Runs on every clock tick.</summary>
    /// <param name="handler">Gets the time of day, running from 0 to 1 over a day, and whether it is a preview time that is not saved.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable TimeOfDayChanged(Action<float, bool> handler) =>
        Static(handler, h => EnvironmentEngine.OnTimeOfDayChangedEvent += h, h => EnvironmentEngine.OnTimeOfDayChangedEvent -= h);

    /// <summary>Runs when the player is moved by a teleport, which is also how every scene change happens.</summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerTeleported(Action handler) =>
        Static(handler, h => PlayerController.OnPlayerTeleported += h, h => PlayerController.OnPlayerTeleported -= h);

    /// <summary>Runs when a world object's view is created, for every object of a scene as it loads and for any object added to the scene later.</summary>
    /// <param name="handler">Gets the spawned object's view. Its Data property is the world object.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable WorldObjectSpawned(Action<Wgo> handler) =>
        Static(handler, h => Wgo.OnWgoSpawn += h, h => Wgo.OnWgoSpawn -= h);

    /// <summary>Runs when a world object's view is destroyed.</summary>
    /// <param name="handler">Gets the view that is going away.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable WorldObjectDestroyed(Action<Wgo> handler) =>
        Static(handler, h => Wgo.OnWgoDestroy += h, h => Wgo.OnWgoDestroy -= h);

    /// <summary>
    /// Runs when a world object leaves the save for good, such as a chest the player dismantles or a sign a mod takes away,
    /// so a mod can drop what it kept for that object. Unlike <see cref="WorldObjectDestroyed"/>, which runs whenever a view
    /// goes, such as when its scene unloads, this one runs only when the object itself is gone. Changing an object's id,
    /// moving it to another scene, or a mod taking it out with its craft kept to put it back, keeps the same object, so none
    /// of them runs it. A mod that replaces an object with a new one removes the old one.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.WorldObjectRemoved(data => _labels.Remove(data.UniqueId));
    /// </code>
    /// </example>
    /// <param name="handler">Gets the world object that was removed.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable WorldObjectRemoved(Action<WgoData> handler) =>
        Static(handler, h => TimbnPatchedEvents.WorldObjectRemoved += h, h => TimbnPatchedEvents.WorldObjectRemoved -= h);

    /// <summary>Runs when any of the game's windows opens, such as the inventory, a vendor or the main menu.</summary>
    /// <example>
    /// <code>
    /// Events.WindowOpened(window => Logger.LogInfo($"{window.GetType().Name} opened"));
    /// </code>
    /// </example>
    /// <param name="handler">Gets the window that opened.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable WindowOpened(Action<LazyWidgetBase> handler) =>
        Static(handler, h => LazyWindowsStackController.OnWindowOpened += h, h => LazyWindowsStackController.OnWindowOpened -= h);

    /// <summary>Runs when the main menu opens, both when the game starts and each time the player leaves a save.</summary>
    /// <example>
    /// <code>
    /// Events.MainMenuOpened(ReloadMyFiles);
    /// </code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable MainMenuOpened(Action handler) =>
        WindowOpened(window =>
        {
            if (window is UIMainMenuWindow)
                handler();
        });

    /// <summary>Runs when the player changes the game's language, so a plugin can drop text it looked up earlier.</summary>
    /// <example>
    /// <code>
    /// Events.LanguageChanged(_itemNames.Clear);
    /// </code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable LanguageChanged(Action handler) =>
        Static(handler, h => GameSettings.OnLanguageChanged += h, h => GameSettings.OnLanguageChanged -= h);

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
        var subscription = Static(handler, h => TimbnPatchedEvents.BalanceLoaded += h, h => TimbnPatchedEvents.BalanceLoaded -= h);
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
    public IDisposable QuestCompleted(Action<QuestData> handler) =>
        SaveEvent(handler, h => MainGame.Instance.GameSave.questSystemData.OnQuestCompleted += h, h => MainGame.Instance.GameSave.questSystemData.OnQuestCompleted -= h);

    /// <summary>Runs when the clock wraps into a new day.</summary>
    /// <example>
    /// <code>
    /// Events.NewDayStarted(_ => tally.Current.DaysPassed++);
    /// </code>
    /// </example>
    /// <param name="handler">Gets the day number, counting up from 1 for the whole save.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable NewDayStarted(Action<int> handler) =>
        Static(handler, h => EnvironmentEngine.OnNewDayStarted += h, h => EnvironmentEngine.OnNewDayStarted -= h);

    /// <summary>
    /// Runs when any of the player's resources changes, money, energy, sanity and reputation included. Subscribe once,
    /// and it follows every save the player loads.
    /// </summary>
    /// <param name="handler">Your handler. Read the values you care about with <see cref="TimbnPlayer.GetResource"/> or <see cref="TimbnPlayer.Money"/>.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerResourceChanged(Action handler) =>
        SaveEvent(handler, h => MainGame.PlayerData.OnGameResChanged += h, h => MainGame.PlayerData.OnGameResChanged -= h);

    /// <summary>
    /// Runs when the player researches a technology, or the game unlocks one for them, such as the ones every save
    /// starts with. A mod's own craft or station needs no handler, since <see cref="TimbnCraftBuilder.UnlockedBy"/> and
    /// <see cref="TimbnStationBuilder.UnlockedBy"/> put it on the tech, which also covers a save that had the tech
    /// already.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Events.TechUnlocked((techId, silent) => { if (!silent) Logger.LogInfo($"The player researched {techId}."); });
    /// ]]></code>
    /// </example>
    /// <param name="handler">
    /// Gets the technology's id and whether the game unlocked it silently, with no popup, which it does for the ones a
    /// save starts with and for ones it puts back on load.
    /// </param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable TechUnlocked(Action<string, bool> handler) =>
        Static(handler, h => KnowledgeSystem.OnTechUnlocked += h, h => KnowledgeSystem.OnTechUnlocked -= h);

    /// <summary>Runs when the player uses an item, such as drinking a potion. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the item used.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerItemUsed(Action<Item> handler) =>
        SaveEvent(handler, h => MainGame.PlayerData.OnItemUsed += h, h => MainGame.PlayerData.OnItemUsed -= h);

    /// <summary>Runs when items go into the player's inventory. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the items added.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerItemsAdded(Action<List<Item>> handler) =>
        SaveEvent(handler, h => MainGame.PlayerData.inventory.OnItemsAdd += h, h => MainGame.PlayerData.inventory.OnItemsAdd -= h);

    /// <summary>Runs when items leave the player's inventory. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the items removed.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerItemsRemoved(Action<List<Item>> handler) =>
        SaveEvent(handler, h => MainGame.PlayerData.inventory.OnItemsRemove += h, h => MainGame.PlayerData.inventory.OnItemsRemove -= h);

    /// <summary>Runs when the player picks up drops. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the items picked up.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable PlayerDropCollected(Action<List<Item>> handler) =>
        SaveEvent(handler, h => MainGame.PlayerData.OnDropCollected += h, h => MainGame.PlayerData.OnDropCollected -= h);

    /// <summary>Runs when a quest is canceled in the loaded save. Subscribe once, and it follows every save the player loads.</summary>
    /// <param name="handler">Gets the quest's save data. Its id is the quest id.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable QuestCanceled(Action<QuestData> handler) =>
        SaveEvent(handler, h => MainGame.Instance.GameSave.questSystemData.OnQuestCanceled += h, h => MainGame.Instance.GameSave.questSystemData.OnQuestCanceled -= h);

    /// <summary>
    /// Runs on every frame while a save is loaded, paused or not. It never runs on the main menu. Prefer
    /// <see cref="EveryRealSeconds"/> for work that does not need every frame or should stop while the game is paused. If the
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
    /// Events.EveryRealSeconds(0.5f, () => stuckCarriers.Tick());
    /// </code>
    /// </example>
    /// <param name="seconds">The shortest time between runs. Zero or less runs on every frame.</param>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable EveryRealSeconds(float seconds, Action handler) => Own(_frameHandlers.Add(handler, seconds, whilePaused: false));

    /// <summary>
    /// Runs on every frame after the game's own Update code, while a save is loaded, paused or not. Camera work
    /// belongs here, since the game moves the player during Update and a camera that follows them has to run
    /// afterwards. Otherwise it works like <see cref="Update"/>.
    /// </summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable LateUpdate(Action handler) => Own(_lateHandlers.Add(handler, 0f, whilePaused: true));

    /// <summary>
    /// Runs on every physics step while a save is loaded, paused or not. Forces and velocities belong here, since
    /// the physics engine reads them at that moment. Otherwise it works like <see cref="Update"/>.
    /// </summary>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable FixedUpdate(Action handler) => Own(_fixedHandlers.Add(handler, 0f, whilePaused: true));

    /// <summary>
    /// Subscribes to a static game event that has no named method here. The handler is wrapped so a throw is
    /// logged instead of reaching the game, the subscription is owned by the plugin, and it is removed when the
    /// plugin unloads, so there is nothing to clean up. It hooks the event once, so use it only for static events.
    /// An event on an object that belongs to a save (quests, the player's data) needs
    /// <see cref="SaveEvent(Action, Action{Action}, Action{Action})"/>, which hooks it again for each save.
    /// </summary>
    /// <example>
    /// Log every tech the player unlocks, using the game's static KnowledgeSystem.OnTechUnlocked event, which
    /// passes the tech id and whether the unlock was silent.
    /// <code><![CDATA[
    /// Events.GameEvent<string, bool>(
    ///     (techId, silent) => Logger.LogInfo($"Unlocked {techId}"),
    ///     h => KnowledgeSystem.OnTechUnlocked += h,
    ///     h => KnowledgeSystem.OnTechUnlocked -= h);
    /// ]]></code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <param name="add">Subscribes a handler to the event, such as <c><![CDATA[h => KnowledgeSystem.OnTechUnlocked += h]]></c>.</param>
    /// <param name="remove">Unsubscribes it again, such as <c><![CDATA[h => KnowledgeSystem.OnTechUnlocked -= h]]></c>.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GameEvent(Action handler, Action<Action> add, Action<Action> remove) => Static(handler, add, remove);

    /// <inheritdoc cref="GameEvent(Action, Action{Action}, Action{Action})"/>
    public IDisposable GameEvent<T>(Action<T> handler, Action<Action<T>> add, Action<Action<T>> remove) => Static(handler, add, remove);

    /// <inheritdoc cref="GameEvent(Action, Action{Action}, Action{Action})"/>
    public IDisposable GameEvent<T1, T2>(Action<T1, T2> handler, Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove) =>
        Static(handler, add, remove);

    /// <summary>
    /// Subscribes to a static game event by naming it, so Core does the += and -=. Name the event with nameof, so a
    /// game update that renames it fails the build. The handler has to take exactly the parameters the event passes,
    /// which is checked straight away, and otherwise it works like
    /// <see cref="GameEvent(Action, Action{Action}, Action{Action})"/>.
    /// </summary>
    /// <example>
    /// Log every tech the player unlocks.
    /// <code><![CDATA[
    /// Events.GameEvent(typeof(KnowledgeSystem), nameof(KnowledgeSystem.OnTechUnlocked),
    ///     (string techId, bool silent) => Logger.LogInfo($"Unlocked {techId}"));
    /// ]]></code>
    /// </example>
    /// <param name="type">The game type that declares the event, such as typeof(KnowledgeSystem).</param>
    /// <param name="eventName">The event's name, such as nameof(KnowledgeSystem.OnTechUnlocked).</param>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable GameEvent(Type type, string eventName, Action handler)
    {
        var (add, remove) = TimbnEventHooks.Find<Action>(type, eventName, null);
        return Static(handler, add, remove);
    }

    /// <inheritdoc cref="GameEvent(Type, string, Action)"/>
    public IDisposable GameEvent<T>(Type type, string eventName, Action<T> handler)
    {
        var (add, remove) = TimbnEventHooks.Find<Action<T>>(type, eventName, null);
        return Static(handler, add, remove);
    }

    /// <inheritdoc cref="GameEvent(Type, string, Action)"/>
    public IDisposable GameEvent<T1, T2>(Type type, string eventName, Action<T1, T2> handler)
    {
        var (add, remove) = TimbnEventHooks.Find<Action<T1, T2>>(type, eventName, null);
        return Static(handler, add, remove);
    }

    /// <summary>
    /// Subscribes to an event on an object that belongs to the loaded save, such as the player's data or the save's
    /// quests. Subscribe once in OnAwake. Core hooks the event on every save the player loads, straight away when a
    /// save is already loaded, and unhooks it when the player leaves the save. Otherwise it works like
    /// <see cref="GameEvent(Action, Action{Action}, Action{Action})"/>. Add and remove read the save's object each
    /// time they run.
    /// </summary>
    /// <example>
    /// Log every item the player uses, in every save.
    /// <code><![CDATA[
    /// Events.SaveEvent<Item>(
    ///     item => Logger.LogInfo($"Used {item.id}"),
    ///     h => MainGame.PlayerData.OnItemUsed += h,
    ///     h => MainGame.PlayerData.OnItemUsed -= h);
    /// ]]></code>
    /// </example>
    /// <param name="handler">Your handler.</param>
    /// <param name="add">Subscribes a handler to the save's event, such as <c><![CDATA[h => MainGame.PlayerData.OnItemUsed += h]]></c>.</param>
    /// <param name="remove">Unsubscribes it again, such as <c><![CDATA[h => MainGame.PlayerData.OnItemUsed -= h]]></c>.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SaveEvent(Action handler, Action<Action> add, Action<Action> remove) =>
        PerSave(() => TimbnGameEvents.On(handler, add, remove));

    /// <inheritdoc cref="SaveEvent(Action, Action{Action}, Action{Action})"/>
    public IDisposable SaveEvent<T>(Action<T> handler, Action<Action<T>> add, Action<Action<T>> remove) =>
        PerSave(() => TimbnGameEvents.On(handler, add, remove));

    /// <inheritdoc cref="SaveEvent(Action, Action{Action}, Action{Action})"/>
    public IDisposable SaveEvent<T1, T2>(Action<T1, T2> handler, Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove) =>
        PerSave(() => TimbnGameEvents.On(handler, add, remove));

    /// <summary>
    /// Subscribes to an event on an object that belongs to the loaded save by naming it, so Core does the += and -=.
    /// The object is read from <paramref name="source"/> each time the event is hooked, and otherwise it works like
    /// <see cref="SaveEvent(Action, Action{Action}, Action{Action})"/> and <see cref="GameEvent(Type, string, Action)"/>.
    /// </summary>
    /// <example>
    /// Log every item the player uses, in every save.
    /// <code><![CDATA[
    /// Events.SaveEvent(() => MainGame.PlayerData, nameof(PlayerData.OnItemUsed),
    ///     (Item item) => Logger.LogInfo($"Used {item.id}"));
    /// ]]></code>
    /// </example>
    /// <param name="source">Reads the save's object, such as <c><![CDATA[() => MainGame.PlayerData]]></c>.</param>
    /// <param name="eventName">The event's name, such as nameof(PlayerData.OnItemUsed).</param>
    /// <param name="handler">Your handler.</param>
    /// <returns>A handle that unsubscribes early when disposed. You don't need to keep it.</returns>
    public IDisposable SaveEvent<TSource>(Func<TSource> source, string eventName, Action handler)
    {
        var (add, remove) = TimbnEventHooks.Find<Action>(typeof(TSource), eventName, () => source());
        return PerSave(() => TimbnGameEvents.On(handler, add, remove));
    }

    /// <inheritdoc cref="SaveEvent{TSource}(Func{TSource}, string, Action)"/>
    public IDisposable SaveEvent<TSource, T>(Func<TSource> source, string eventName, Action<T> handler)
    {
        var (add, remove) = TimbnEventHooks.Find<Action<T>>(typeof(TSource), eventName, () => source());
        return PerSave(() => TimbnGameEvents.On(handler, add, remove));
    }

    /// <inheritdoc cref="SaveEvent{TSource}(Func{TSource}, string, Action)"/>
    public IDisposable SaveEvent<TSource, T1, T2>(Func<TSource> source, string eventName, Action<T1, T2> handler)
    {
        var (add, remove) = TimbnEventHooks.Find<Action<T1, T2>>(typeof(TSource), eventName, () => source());
        return PerSave(() => TimbnGameEvents.On(handler, add, remove));
    }

    internal void RunFrameHandlers() => _frameHandlers.Run();

    internal void RunLateHandlers() => _lateHandlers.Run();

    internal void RunFixedHandlers() => _fixedHandlers.Run();

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

    private IDisposable Static(Action handler, Action<Action> add, Action<Action> remove) => Own(TimbnGameEvents.On(handler, add, remove));

    private IDisposable Static<T>(Action<T> handler, Action<Action<T>> add, Action<Action<T>> remove) => Own(TimbnGameEvents.On(handler, add, remove));

    private IDisposable Static<T1, T2>(Action<T1, T2> handler, Action<Action<T1, T2>> add, Action<Action<T1, T2>> remove) =>
        Own(TimbnGameEvents.On(handler, add, remove));

    private IDisposable Own(IDisposable subscription) => _owner.Subscriptions.Add(subscription);

    private void RunSafely(Action handler) =>
        TimbnSafe.Run(handler, _owner.PluginLogger, $"{nameof(TimbnPluginEvents)}|{TimbnSafe.Describe(handler)}");

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
