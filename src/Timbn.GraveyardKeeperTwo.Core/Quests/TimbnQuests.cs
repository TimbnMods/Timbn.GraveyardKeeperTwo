using BepInEx.Bootstrap;
using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Registers the quests plugins add as real QuestDefs, so the game's own quest system starts, finishes, saves and
/// lists them. It also reads the status of any quest, the game's own included.
/// </summary>
public static class TimbnQuests
{
    private static readonly AccessTools.FieldRef<QuestDef, int> _posX = AccessTools.FieldRefAccess<QuestDef, int>("posX");
    private static readonly AccessTools.FieldRef<QuestDef, int> _posY = AccessTools.FieldRefAccess<QuestDef, int>("posY");

    private static readonly AccessTools.FieldRef<ObjectLinkedToDefinition<QuestDef>, string> _cachedId =
        AccessTools.FieldRefAccess<ObjectLinkedToDefinition<QuestDef>, string>("cachedId");

    private static readonly Dictionary<string, QuestDef> _quests = [];
    private static readonly Dictionary<string, int> _registeredAt = [];
    private static int _registrations;
    private static readonly Dictionary<string, IDisposable> _stubs = [];
    private static readonly Dictionary<QuestDef, int> _gameY = [];
    private static readonly Dictionary<QuestDef, Registration> _registered = [];
    private static readonly List<TimbnQuestRuntime> _runtimes = [];
    private static readonly List<TimbnQuestCycle> _cycles = [];
    private static readonly HashSet<string> _warnedParents = [];
    private static Func<HashSet<string>>? _marks;
    private static Func<Dictionary<string, TimbnQuestRunData>>? _runs;

    internal static QuestDef CreateDefinition(string id) => new()
    {
        id = id,
        isHidden = true,
        hasPosInBalance = true,
        startCheck = new QuestCheck { questId = id },
        finishCheck = new QuestFinishCheck { questId = id },
    };

    internal static QuestDef After(this QuestDef definition, string parentId)
    {
        definition.parents.Add(parentId);
        return definition;
    }

    internal static IDisposable Register(QuestDef definition, string owner, bool continues = false, Func<Color>? lineColor = null)
    {
        if (OtherOwnerOf(definition.id, owner) is { } other)
            throw new InvalidOperationException($"{OwnerName(other)} already adds a quest with the id '{definition.id}'.");

        FillBlanks(definition);
        if (!_registeredAt.ContainsKey(definition.id))
            _registeredAt[definition.id] = _registrations++;

        _registered[definition] = new Registration([.. definition.parents], definition.phase, definition.iconId, continues, lineColor, owner);
        var balance = TimbnBalance.Add(definition, b => Relayout(b, definition));
        _quests[definition.id] = definition;
        AssignFromChains();
        if (_stubs.TryGetValue(definition.id, out var stub))
        {
            _stubs.Remove(definition.id);
            stub.Dispose();
        }

        AttachToLoadedSave(definition);
        RelayoutIfLoaded();
        return new TimbnUndo(() =>
        {
            var replaced = _quests.TryGetValue(definition.id, out var current) && current != definition;
            if (!replaced)
                _quests.Remove(definition.id);

            DetachFromLoadedSave(definition, replaced);
            Unlink(definition);
            if (_registered.TryGetValue(definition, out var registration))
            {
                definition.parents.Clear();
                definition.parents.AddRange(registration.Parents);
                definition.phase = registration.Phase;
                definition.iconId = registration.Icon;
                _registered.Remove(definition);
            }

            AssignFromChains();
            balance.Dispose();
            RelayoutIfLoaded();
        });
    }

    internal static string? OtherOwnerOf(string id, string owner) =>
        _quests.TryGetValue(id, out var current) && _registered.TryGetValue(current, out var registration) && registration.Owner != owner
            ? registration.Owner
            : null;

    internal static string OwnerName(string owner) =>
        Chainloader.PluginInfos.TryGetValue(owner, out var info) && info?.Metadata is { } metadata ? metadata.Name : owner;

    internal static Func<Color>? LineColorOf(string id) =>
        _quests.TryGetValue(id, out var quest) && _registered.TryGetValue(quest, out var registration) ? registration.LineColor : null;

    private static void AssignFromChains()
    {
        var ordered = _quests.Values.OrderBy(Order).ToList();
        var phases = TimbnQuestPhases.Assign([.. ordered.Select(q => _registered.TryGetValue(q, out var registration)
            ? new TimbnQuestPhases.Step(q.id, Continued(registration), registration.Phase)
            : new TimbnQuestPhases.Step(q.id, null, q.phase))]);
        var icons = TimbnQuestIcons.Inherit([.. ordered.Select(q => _registered.TryGetValue(q, out var registration)
            ? new TimbnQuestIcons.Step(q.id, Continued(registration), registration.Icon)
            : new TimbnQuestIcons.Step(q.id, null, q.iconId ?? ""))]);
        foreach (var quest in ordered)
        {
            quest.phase = phases[quest.id];
            quest.iconId = icons[quest.id];
        }
    }

    private static string? Continued(Registration registration) => registration.Continues ? registration.Parents.FirstOrDefault() : null;

    private static void RelayoutIfLoaded()
    {
        if (TimbnBalance.Loaded is { } balance)
            Relayout(balance, null);
    }

    internal static void OnLeftGame()
    {
        foreach (var runtime in _runtimes.ToList())
            TimbnSafe.Run(runtime.Deactivate, $"{nameof(TimbnQuests)}|Ending quest '{runtime.Id}' for the main menu");

        DropStubs();
    }

    /// <summary>Whether the quest has a real definition, the game's or a plugin's, not counting a placeholder for a saved quest whose mod is gone.</summary>
    internal static bool HasDefinition(string id) =>
        !_stubs.ContainsKey(id) && TimbnBalance.Loaded?.GetDataOrNull<QuestDef>(id) is not null;

    private static void DropStubs()
    {
        foreach (var stub in _stubs.Values.ToList())
            TimbnSafe.Run(stub.Dispose, $"{nameof(TimbnQuests)}|Dropping a saved quest's placeholder");

        _stubs.Clear();
    }

    internal static void OnQuestStarted(QuestData quest)
    {
        if (_quests.ContainsKey(quest.id))
            RelayoutIfLoaded();
    }

    /// <summary>
    /// A quest's status in the loaded save, the game's own quests included, such as 6_intro_guards_burial. Null when
    /// no save is loaded or it has no such quest.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .OfferWhen(() => TimbnQuests.StatusOf("6_intro_guards_burial") == QuestStatus.Available)
    /// ]]></code>
    /// </example>
    /// <param name="id">The quest id.</param>
    /// <returns>The status, or null.</returns>
    public static QuestStatus? StatusOf(string id) =>
        LoadedQuests?.questCollection.questsCache.TryGetValue(id, out var data) == true ? data.status : null;

    /// <summary>
    /// Whether the player has started a quest in the loaded save, the game's own quests included, so it is in progress or
    /// done. False when no save is loaded or it has no such quest.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .OfferWhen(() => TimbnQuests.HasStarted("6_intro_guards_burial"))
    /// ]]></code>
    /// </example>
    /// <param name="id">The quest id.</param>
    public static bool HasStarted(string id) => StatusOf(id) is QuestStatus.InProgress or QuestStatus.Completed;

    /// <summary>
    /// Fires one of the game's story triggers, the signals its quests and cutscenes wait on, as if the player had
    /// just built the building or won the fight. Use it to finish a game quest whose step was done before the quest
    /// started. Events.StoryTrigger hears it like any other. Nothing happens at the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.HasBuilt("zmb_choir_unlock_s"))
    ///     TimbnQuests.FireStoryTrigger(GlobalEventsSystem.Event.Type.BuildBuilding, "zmb_choir_unlock");
    /// </code>
    /// </example>
    /// <param name="type">The kind of trigger.</param>
    /// <param name="id">The trigger's id, such as the building or the answer. Leave it out for a kind that has none.</param>
    public static void FireStoryTrigger(GlobalEventsSystem.Event.Type type, string id = "")
    {
        if (TimbnGame.IsInGame)
            GlobalEventsSystem.FireTrigger(type, id);
    }

    internal static void Start(string id) => LoadedQuests?.StartQuest(id);

    internal static bool StartModQuest(string id, string reason) => _runtimes.Find(runtime => runtime.Id == id) is { } found && found.Start(reason);

    internal static void Complete(string id)
    {
        ClearProgress(id);
        LoadedQuests?.CompleteQuest(id);
    }

    internal static bool ResetToAvailable(string id)
    {
        if (LoadedQuests is not { } quests || !quests.questCollection.questsCache.TryGetValue(id, out var data))
            return false;

        ClearProgress(id);
        quests.ResetQuestToAvailable(id);
        data.isHidden = true;
        RelayoutIfLoaded();
        return true;
    }

    private static void ClearProgress(string id)
    {
        _marks?.Invoke().Remove(id);
    }

    internal static void UseReadyMarks(Func<HashSet<string>> marks) => _marks = marks;

    internal static bool IsMarked(string id) => _marks?.Invoke().Contains(id) == true;

    internal static void Mark(string id) => _marks?.Invoke().Add(id);

    internal static void CollectMarkerNpcs(Dictionary<string, Color> into)
    {
        foreach (var runtime in _runtimes)
        {
            if ((runtime.HandInMarkerNpc ?? runtime.OfferMarkerNpc) is { } npc && !into.ContainsKey(npc))
                into[npc] = runtime.MarkerColor;
        }
    }

    internal static IDisposable Track(TimbnQuestRuntime runtime)
    {
        _runtimes.Add(runtime);
        return new TimbnUndo(() => _runtimes.Remove(runtime));
    }

    internal static IDisposable Track(TimbnQuestCycle cycle)
    {
        _cycles.Add(cycle);
        return new TimbnUndo(() => _cycles.Remove(cycle));
    }

    internal static void UseRuns(Func<Dictionary<string, TimbnQuestRunData>> runs) => _runs = runs;

    /// <summary>
    /// Whether the current run of any mod's quest names this NPC, such as the villager who asked for a dish. A quest's
    /// Setup can skip them, so one person never has two quests going at once.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// var villagers = TimbnNpcs.GroupMemberIds(TimbnGameIds.Groups.VillagersDayAll).Where(npc => !TimbnQuests.HasQuestRun(npc)).ToList();
    /// ]]></code>
    /// </example>
    /// <param name="npcId">The NPC, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <returns>True while a run names them. False at the main menu.</returns>
    public static bool HasQuestRun(string npcId) =>
        TimbnGame.IsInGame
        && _runs?.Invoke() is { } runs
        && runs.Any(run => run.Value.Npcs.ContainsValue(npcId) && _cycles.Exists(cycle => cycle.Id == run.Key && !cycle.IsFinished));

    internal static TimbnQuestRunData? RunOf(string id) =>
        TimbnGame.IsInGame && _runs?.Invoke() is { } runs && runs.TryGetValue(id, out var data) ? data : null;

    internal static void SetRun(string id, TimbnQuestRunData? data)
    {
        if (_runs?.Invoke() is not { } runs)
            return;

        if (data is null)
            runs.Remove(id);
        else
            runs[id] = data;
    }

    internal static void OnGameStarted()
    {
        WarnAboutParents();
        // The game rebuilds its quest cache from every quest it has, so every run is put back and every step written
        // before the one rebuild, instead of a rebuild per quest.
        foreach (var cycle in _cycles.ToList())
        {
            TimbnSafe.Run(() =>
            {
                if (cycle.Restore())
                    cycle.RewriteTexts();
            }, $"{nameof(TimbnQuests)}|Setting up the run of quest '{cycle.Id}' for the save");
        }

        var runtimes = _runtimes.ToList();
        foreach (var runtime in runtimes)
            TimbnSafe.Run(() => runtime.Write(), $"{nameof(TimbnQuests)}|Setting up quest '{runtime.Id}' for the save");

        TimbnQuestRuntime.RefreshCaches();
        foreach (var runtime in runtimes)
            TimbnSafe.Run(runtime.ActivateIfStarted, $"{nameof(TimbnQuests)}|Starting quest '{runtime.Id}' for the save");

        TimbnDialog.Refresh();
        StartContinuedQuests();
    }

    internal static void OnNewDay()
    {
        using var batch = TimbnQuestRuntime.Batch();
        foreach (var cycle in _cycles.ToList())
            TimbnSafe.Run(cycle.OnNewDay, $"{nameof(TimbnQuests)}|Checking the run of quest '{cycle.Id}' on a new day");
    }

    private static void WarnAboutParents()
    {
        foreach (var (quest, registration) in _registered)
        {
            foreach (var parentId in registration.Parents)
            {
                var known = _quests.ContainsKey(parentId) || (!_stubs.ContainsKey(parentId) && TimbnBalance.Loaded?.GetDataOrNull<QuestDef>(parentId) is not null);
                if (!known && _warnedParents.Add($"{quest.id}/{parentId}"))
                    TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnQuests)}|Quest '{quest.id}' comes after '{parentId}', which is not a quest in the game or any loaded mod, so it is never offered.");
            }

            if (WaitsOnItself(quest) && _warnedParents.Add(quest.id))
                TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnQuests)}|Quest '{quest.id}' comes after quests that lead back to it, so it is never offered.");
        }
    }

    private static bool WaitsOnItself(QuestDef quest)
    {
        HashSet<string> seen = [];
        var waiting = new Stack<string>(OwnParents(quest));
        while (waiting.Count > 0)
        {
            var id = waiting.Pop();
            if (id == quest.id)
                return true;

            if (!seen.Add(id) || !_quests.TryGetValue(id, out var parent))
                continue;

            foreach (var parentId in OwnParents(parent))
                waiting.Push(parentId);
        }

        return false;
    }

    internal static void StartContinuedQuests()
    {
        foreach (var runtime in _runtimes.ToList())
            TimbnSafe.Run(runtime.StartWithContinued, $"{nameof(TimbnQuests)}|Starting quest '{runtime.Id}' after the one it continues");
    }

    internal static void CheckReadyWhen()
    {
        foreach (var runtime in _runtimes.ToList())
            runtime.CheckReadyWhen();
    }

    internal static void OnGameEvent(GlobalEventsSystem.Event.Type type, string id)
    {
        if (type != GlobalEventsSystem.Event.Type.AfterSleep)
            Raise(type, id);
    }

    internal static void OnWakingUp() => Raise(GlobalEventsSystem.Event.Type.AfterSleep, "");

    private static void Raise(GlobalEventsSystem.Event.Type type, string id)
    {
        if (_runtimes.Count == 0 || !TimbnGame.IsInGame)
            return;

        foreach (var runtime in _runtimes.ToList())
            TimbnSafe.Run(() => runtime.OnGameEvent(type, id), $"{nameof(TimbnQuests)}|Quest '{runtime.Id}' on {type}");
    }

    private static QuestSystemData? LoadedQuests
    {
        get
        {
            var quests = MainGame.Instance?.GameSave?.questSystemData;
            return quests?.questCollection?.questsCache is null ? null : quests;
        }
    }

    private static void FillBlanks(QuestDef definition)
    {
        definition.iconId ??= string.Empty;
        definition.wgoNpcId ??= string.Empty;
        definition.repVisualisationRes ??= new GameRes();
        definition.startCheck ??= new QuestCheck { questId = definition.id };
        definition.finishCheck ??= new QuestFinishCheck { questId = definition.id };
        foreach (var check in new QuestCheck[] { definition.startCheck, definition.finishCheck })
            check.triggerId ??= string.Empty;

        definition.finishCheck.phrase ??= string.Empty;
    }

    private static void Relayout(GameBalance balance, QuestDef? adding, Dictionary<string, QuestData>? saved = null)
    {
        var ours = _quests.Values.Where(q => q.id != adding?.id).ToList();
        if (adding is not null)
            ours.Add(adding);

        ours = [.. ours.OrderBy(Order)];
        var oursById = ours.ToDictionary(q => q.id);
        var game = balance.questDefs.Where(q => !oursById.ContainsKey(q.id)).ToList();
        var inBalance = new HashSet<QuestDef>(balance.questDefs);

        // Stubs are new QuestDefs on every load, so the old ones would pile up here.
        foreach (var gone in _gameY.Keys.Where(q => !inBalance.Contains(q)).ToList())
            _gameY.Remove(gone);

        foreach (var quest in game)
        {
            if (!_gameY.ContainsKey(quest))
                _gameY[quest] = quest.TreePos.y;
        }

        var cache = saved ?? LoadedQuests?.questCollection.questsCache;
        bool started(QuestDef q) =>
            cache is not null && cache.TryGetValue(q.id, out var data) && data.status is QuestStatus.InProgress or QuestStatus.Completed;

        var plan = TimbnQuestLayout.Arrange(
            [.. ours.Select(q => new TimbnQuestLayout.Quest(q.id, OwnParents(q), default, started: started(q), continues: _registered.TryGetValue(q, out var r) && r.Continues))],
            [.. game.Select(q => new TimbnQuestLayout.Quest(q.id, q.parents, new TimbnQuestLayout.Cell(q.TreePos.x, _gameY[q]), q.hasPosInBalance))]);

        foreach (var quest in ours)
        {
            var cell = plan.Cells[quest.id];
            _posX(quest) = cell.X;
            _posY(quest) = cell.Y;
        }

        foreach (var quest in game)
            _posY(quest) = plan.Cells[quest.id].Y;

        // The game quests are back where they started, so the next load can take the fast path again.
        if (ours.Count == 0)
            _gameY.Clear();

        if (cache is not null)
        {
            foreach (var (id, hidden) in plan.Hidden)
            {
                if (cache.TryGetValue(id, out var data))
                    data.isHidden = hidden;
            }
        }

        var byId = new Dictionary<string, QuestDef>();
        foreach (var quest in game.Concat(ours))
            byId[quest.id] = quest;

        foreach (var quest in ours)
        {
            Unlink(quest);
            quest.parents.Clear();
            quest.parents.AddRange(OwnParents(quest));
            if (plan.ExtraParents.TryGetValue(quest.id, out var extra))
                quest.parents.AddRange(extra);

            Link(quest, byId);
        }

        TimbnQuestRuntime.RefreshCaches(balance);
    }

    private static void Link(QuestDef quest, Dictionary<string, QuestDef> byId)
    {
        foreach (var parentId in quest.parents)
        {
            if (!byId.TryGetValue(parentId, out var parent) || parent == quest)
                continue;

            if (!parent.childDefinitionList.Contains(quest))
                parent.childDefinitionList.Add(quest);

            if (!quest.parentDefinitionList.Contains(parent))
                quest.parentDefinitionList.Add(parent);
        }
    }

    private static List<string> OwnParents(QuestDef definition) =>
        _registered.TryGetValue(definition, out var registration) ? registration.Parents : [.. definition.parents];

    private static int Order(QuestDef definition) => _registeredAt.TryGetValue(definition.id, out var at) ? at : int.MaxValue;

    private static void Unlink(QuestDef definition)
    {
        foreach (var parent in definition.parentDefinitionList)
            parent.childDefinitionList.Remove(definition);

        definition.parentDefinitionList.Clear();
    }

    private static void AttachToLoadedSave(QuestDef definition)
    {
        if (LoadedQuests is not { } quests)
            return;

        if (!quests.questCollection.questsCache.TryGetValue(definition.id, out var data))
        {
            quests.AddQuestData(definition.id);
            return;
        }

        _cachedId(data) = string.Empty;
        data.isHidden = HiddenFor(definition, data);
        var events = GlobalEventsSystem.Me;
        if (data.status == QuestStatus.Awaiting && definition.startCheck.hasTrigger)
            events.AddEvent(definition.startCheck);
        else if (data.status == QuestStatus.InProgress && definition.finishCheck.hasTrigger)
            events.AddEvent(definition.finishCheck);
    }

    private static void DetachFromLoadedSave(QuestDef definition, bool replaced)
    {
        if (LoadedQuests is not { } quests || !quests.questCollection.questsCache.TryGetValue(definition.id, out var data))
            return;

        var events = GlobalEventsSystem.Me;
        if (definition.startCheck.hasTrigger)
            events.RemoveEvent(definition.startCheck);
        if (definition.finishCheck.hasTrigger)
            events.RemoveEvent(definition.finishCheck);

        if (!replaced)
            data.isHidden = true;
    }

    internal static void StubOrphans(QuestSystemData quests)
    {
        DropStubs();
        var stubbed = 0;
        foreach (var data in quests.questCollection.quests)
        {
            if (data is null || GameBalance.Me.GetDataOrNull<QuestDef>(data.id) is not null)
                continue;

            var stub = CreateDefinition(data.id);
            stub.hasPosInBalance = false;
            FillBlanks(stub);
            // Relayout below rebuilds the quest cache once for all of them.
            _stubs[data.id] = TimbnBalance.AddWithoutRefresh(stub);
            _cachedId(data) = string.Empty;
            data.isHidden = true;
            stubbed++;
        }

        if (stubbed == 0)
            return;

        Relayout(GameBalance.Me, null);
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnQuests)}|Kept {stubbed} saved quests with no definition as hidden placeholders.");
    }

    internal static void OnQuestsPrepared(QuestSystemData quests)
    {
        foreach (var definition in _quests.Values)
        {
            if (quests.questCollection.questsCache.TryGetValue(definition.id, out var data))
                data.isHidden = HiddenFor(definition, data);
        }

        // With no mod quests and no game quest ever moved, the layout changes nothing but still costs about 40 ms on load.
        if (_quests.Count == 0 && _gameY.Count == 0)
            return;

        if (TimbnBalance.Loaded is { } balance)
            Relayout(balance, null, quests.questCollection.questsCache);
    }

    internal static IDisposable HideForSave(GameSave save)
    {
        var cache = save.questSystemData?.questCollection?.questsCache;
        if (cache is null)
            return TimbnUndo.None;

        List<QuestData> hidden = [];
        foreach (var definition in _quests.Values)
        {
            if (cache.TryGetValue(definition.id, out var data) && !data.isHidden)
            {
                data.isHidden = true;
                hidden.Add(data);
            }
        }

        return new TimbnUndo(() =>
        {
            foreach (var data in hidden)
                data.isHidden = false;
        });
    }

    private static bool HiddenFor(QuestDef definition, QuestData data) =>
        definition.isHidden && data.status is not (QuestStatus.InProgress or QuestStatus.Completed);

    private sealed class Registration
    {
        internal Registration(List<string> parents, int phase, string icon, bool continues, Func<Color>? lineColor, string owner)
        {
            Parents = parents;
            Phase = phase;
            Icon = icon;
            Continues = continues;
            LineColor = lineColor;
            Owner = owner;
        }

        internal string Owner { get; }

        internal List<string> Parents { get; }

        internal int Phase { get; }

        internal string Icon { get; }

        internal bool Continues { get; }

        internal Func<Color>? LineColor { get; }
    }
}
