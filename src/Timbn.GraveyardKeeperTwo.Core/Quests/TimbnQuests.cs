using LazyBearTechnology;
using System.Runtime.CompilerServices;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Registers quests defined in code as real QuestDefs, so the game's own quest system starts, finishes,
/// saves, and lists them.
/// </summary>
public static class TimbnQuests
{
    private static readonly AccessTools.FieldRef<QuestDef, int> _posX = AccessTools.FieldRefAccess<QuestDef, int>("posX");
    private static readonly AccessTools.FieldRef<QuestDef, int> _posY = AccessTools.FieldRefAccess<QuestDef, int>("posY");

    private static readonly AccessTools.FieldRef<ObjectLinkedToDefinition<QuestDef>, string> _cachedId =
        AccessTools.FieldRefAccess<ObjectLinkedToDefinition<QuestDef>, string>("cachedId");

    private static readonly Action<QuestDef> _linkToParents =
        AccessTools.MethodDelegate<Action<QuestDef>>(AccessTools.Method(typeof(QuestDef), "InitParentsAndChildren"));

    private static readonly Dictionary<string, QuestDef> _quests = [];
    private static readonly Dictionary<string, int> _registeredAt = [];
    private static int _registrations;
    private static readonly Dictionary<string, IDisposable> _stubs = [];
    private static readonly Dictionary<QuestDef, int> _gameY = [];
    private static readonly ConditionalWeakTable<QuestDef, StrongBox<int>> _moneyRewards = new();

    /// <summary>
    /// Creates a blank quest with empty start and finish checks. It stays out of the quest tree until it
    /// starts. Start it with <see cref="Start"/>, for example when the player accepts it in a conversation.
    /// </summary>
    /// <param name="id">
    /// The quest id. It is also the localisation key for the quest's name, and quest_open_&lt;id&gt;_d
    /// and quest_closed_&lt;id&gt;_d hold its descriptions.
    /// </param>
    /// <returns>The new definition, ready for <see cref="After"/>, <see cref="FinishOnAnswer"/>, and <see cref="Register"/>.</returns>
    public static QuestDef CreateDefinition(string id) => new()
    {
        id = id,
        isHidden = true,
        hasPosInBalance = true,
        startCheck = new QuestCheck { questId = id },
        finishCheck = new QuestFinishCheck { questId = id },
    };

    /// <summary>
    /// Makes this quest a child of another in the quest tree, joined by a line. It only affects the tree, so the
    /// quest still has to be started.
    /// </summary>
    /// <param name="definition">The quest to link.</param>
    /// <param name="parentId">A quest registered earlier, or one of the game's own.</param>
    /// <returns>The same definition, for chaining.</returns>
    public static QuestDef After(this QuestDef definition, string parentId)
    {
        definition.parents.Add(parentId);
        return definition;
    }

    /// <summary>
    /// Completes the quest when the player picks the given answer in a conversation, the same way the game's own
    /// fetch quests work. The answer shows the items as its price, is greyed out until the player has them, and
    /// takes them when picked. Build the answer with <see cref="TimbnDialog.Answer"/> using the same id.
    /// </summary>
    /// <param name="definition">The quest to finish.</param>
    /// <param name="answerId">The answer's localisation key.</param>
    /// <param name="price">Items the player hands over, built with <see cref="Item"/>. Leave it empty for an answer that costs nothing.</param>
    /// <returns>The same definition, for chaining.</returns>
    public static QuestDef FinishOnAnswer(this QuestDef definition, string answerId, params ItemCount[] price)
    {
        var finish = definition.finishCheck;
        finish.triggerType = GlobalEventsSystem.Event.Type.MultiAnswerSay;
        finish.triggerId = answerId;
        finish.hasTrigger = true;
        finish.phrase = answerId;
        foreach (var item in price)
        {
            finish.phraseReqs.Add(new QuestPhraseRequirement
            {
                requirement = QuestPhraseRequirement.Requirement.Price,
                entity = QuestPhraseRequirement.Entity.Item,
                itemCount = item,
            });
        }

        return definition;
    }

    /// <summary>
    /// Builds an item price for <see cref="FinishOnAnswer"/>. It never touches the balance, so it works before
    /// the balance loads.
    /// </summary>
    /// <param name="itemId">The item id, with its quality suffix where the item has one, such as grape_wine:2.</param>
    /// <param name="count">How many the player has to hand over.</param>
    public static ItemCount Item(string itemId, int count) => new() { itemId = itemId, count = count };

    /// <summary>
    /// Pays the player money when they hand the quest in through its FinishOnAnswer answer. The answer shows
    /// the coins as its reward next to the price, and the game pays them when the answer is picked.
    /// </summary>
    /// <param name="definition">The quest to reward.</param>
    /// <param name="amount">The money in the game's raw units, where 100 is one silver coin and 10,000 one gold coin.</param>
    /// <returns>The same definition, for chaining.</returns>
    public static QuestDef RewardMoney(this QuestDef definition, int amount)
    {
        _moneyRewards.Remove(definition);
        _moneyRewards.Add(definition, new StrongBox<int>(amount));
        return definition;
    }

    internal static int MoneyRewardOf(QuestDef definition) =>
        _moneyRewards.TryGetValue(definition, out var reward) ? reward.Value : 0;

    internal static IDisposable Register(QuestDef definition)
    {
        FillBlanks(definition);
        if (!_registeredAt.ContainsKey(definition.id))
            _registeredAt[definition.id] = _registrations++;

        var balance = TimbnBalance.Add(definition, b =>
        {
            Relayout(b, definition);
            _linkToParents(definition);
        });
        _quests[definition.id] = definition;
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
            balance.Dispose();
            RelayoutIfLoaded();
        });
    }

    private static void RelayoutIfLoaded()
    {
        if (TimbnBalance.Loaded is { } balance)
            Relayout(balance, null);
    }

    internal static void OnQuestStarted(QuestData quest)
    {
        if (_quests.ContainsKey(quest.id))
            RelayoutIfLoaded();
    }

    /// <summary>The quest's status in the loaded save, or null when no save is loaded or it has no such quest.</summary>
    public static QuestStatus? StatusOf(string id) =>
        LoadedQuests?.questCollection.questsCache.TryGetValue(id, out var data) == true ? data.status : null;

    /// <summary>
    /// Starts a quest in the loaded save. Does nothing when no save is loaded.
    /// </summary>
    /// <param name="id">The quest id.</param>
    public static void Start(string id) => LoadedQuests?.StartQuest(id);

    internal static void Complete(string id) => LoadedQuests?.CompleteQuest(id);

    /// <summary>True when the quest is in progress and the player meets its finish requirements, such as a FinishOnAnswer price.</summary>
    public static bool CanFinish(string id) =>
        StatusOf(id) == QuestStatus.InProgress
        && GameBalance.Me.GetDataOrNull<QuestDef>(id)?.finishCheck.IsReadyToFinish() == true;

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

        var oursById = ours.ToDictionary(q => q.id);
        var game = balance.questDefs.Where(q => !oursById.ContainsKey(q.id)).ToList();
        foreach (var quest in game)
        {
            if (!_gameY.ContainsKey(quest))
                _gameY[quest] = quest.TreePos.y;
        }

        var gameById = new Dictionary<string, QuestDef>();
        foreach (var quest in game)
            gameById[quest.id] = quest;

        var cache = saved ?? LoadedQuests?.questCollection.questsCache;
        bool shown(QuestDef q) => cache is not null && cache.TryGetValue(q.id, out var data) && !data.isHidden;
        QuestDef? parentOf(QuestDef q) =>
            q.parents.Count > 0 && oursById.TryGetValue(q.parents[0], out var p) && p != q && shown(p) ? p : null;

        var offTree = 0;
        var kids = new Dictionary<QuestDef, List<QuestDef>>();
        List<QuestDef> roots = [];
        foreach (var quest in ours.OrderBy(Order))
        {
            if (quest.parents.Count > 0 && gameById.TryGetValue(quest.parents[0], out var anchor))
                LinkToCell(quest, anchor, game);

            if (!shown(quest))
            {
                SetPosition(quest, new Vector2Int(0, -1 - offTree++));
                continue;
            }

            if (parentOf(quest) is not { } parent)
            {
                roots.Add(quest);
                continue;
            }

            if (!kids.TryGetValue(parent, out var list))
                kids[parent] = list = [];

            list.Add(quest);
        }

        var width = game.Where(q => q.hasPosInBalance).Select(q => q.TreePos.x).DefaultIfEmpty(0).Max() + 1;
        var band = new HashSet<Vector2Int>();
        List<(QuestDef Root, QuestDef Anchor)> anchored = [];
        foreach (var root in roots)
        {
            if (root.parents.Count > 0 && gameById.TryGetValue(root.parents[0], out var anchor))
            {
                anchored.Add((root, anchor));
                continue;
            }

            var shape = Shape(root, kids, out var shapeWidth);
            Place(shape, FindSpot(shape, shapeWidth, band, width, 0, 0), band);
        }

        var shift = band.Count == 0 ? 0 : band.Max(c => c.y) + 2;
        foreach (var quest in game)
            _posY(quest) = _gameY[quest] + shift;

        List<QuestDef> placed = [];
        foreach (var (root, anchor) in anchored.OrderBy(a => a.Anchor.TreePos.y))
        {
            var shape = Shape(root, kids, out var shapeWidth);
            var height = shape.Values.Max(c => c.y) + 1;
            if (!TryNextTo(shape, shapeWidth, anchor.TreePos, 2, game, gameById, band, placed, width, out var origin))
            {
                foreach (var quest in game.Concat(placed))
                {
                    if (quest.TreePos.y > anchor.TreePos.y)
                        _posY(quest) += height;
                }

                if (!TryNextTo(shape, shapeWidth, anchor.TreePos, 1, game, gameById, band, placed, width, out origin))
                {
                    var blocked = BlockedCells(game, gameById);
                    blocked.UnionWith(band);
                    blocked.UnionWith(placed.Select(q => q.TreePos));
                    origin = FindSpot(shape, shapeWidth, blocked, width, anchor.TreePos.y + 1, anchor.TreePos.x);
                }
            }

            Place(shape, origin, []);
            placed.AddRange(shape.Keys);
        }

        TimbnBalance.RefreshQuestCaches(balance);
    }

    private static void LinkToCell(QuestDef root, QuestDef anchor, List<QuestDef> game)
    {
        foreach (var mate in game)
        {
            if (mate.hasPosInBalance && mate.TreePos == anchor.TreePos && !root.parents.Contains(mate.id))
                root.parents.Add(mate.id);
        }
    }

    private static bool TryNextTo(
        Dictionary<QuestDef, Vector2Int> shape,
        int shapeWidth,
        Vector2Int parent,
        int maxRows,
        List<QuestDef> game,
        Dictionary<string, QuestDef> gameById,
        HashSet<Vector2Int> band,
        List<QuestDef> placed,
        int width,
        out Vector2Int origin)
    {
        var occupied = new HashSet<Vector2Int>(game.Select(q => q.TreePos));
        occupied.UnionWith(band);
        occupied.UnionWith(placed.Select(q => q.TreePos));
        var blocked = BlockedCells(game, gameById);
        blocked.UnionWith(occupied);
        for (var dy = 1; dy <= maxRows; dy++)
        {
            if (dy > 1 && occupied.Contains(new Vector2Int(parent.x, parent.y + dy - 1)))
                break;

            foreach (var dx in new[] { 0, 1, -1 })
            {
                var start = new Vector2Int(parent.x + dx, parent.y + dy);
                if (start.x < 0 || start.x + shapeWidth > width)
                    continue;

                if (shape.Values.Any(cell => blocked.Contains(start + cell)))
                    continue;

                origin = start;
                return true;
            }
        }

        origin = default;
        return false;
    }

    private static Dictionary<QuestDef, Vector2Int> Shape(QuestDef root, Dictionary<QuestDef, List<QuestDef>> kids, out int width)
    {
        var shape = new Dictionary<QuestDef, Vector2Int>();
        var next = 0;
        void layout(QuestDef node, int depth)
        {
            shape[node] = Vector2Int.zero;
            var children = kids.TryGetValue(node, out var list) ? list.Where(k => !shape.ContainsKey(k)).ToList() : [];
            if (children.Count == 0)
            {
                shape[node] = new Vector2Int(next++, depth);
                return;
            }

            foreach (var child in children)
                layout(child, depth + 1);

            shape[node] = new Vector2Int(shape[children[0]].x, depth);
        }

        layout(root, 0);
        width = next;
        return shape;
    }

    private static Vector2Int FindSpot(Dictionary<QuestDef, Vector2Int> shape, int shapeWidth, HashSet<Vector2Int> blocked, int width, int startY, int preferX)
    {
        var starts = Enumerable.Range(0, Math.Max(1, width - shapeWidth + 1))
            .OrderBy(x => Math.Abs(x - preferX))
            .ThenBy(x => x)
            .ToList();
        for (var y = startY; ; y++)
        {
            foreach (var x in starts)
            {
                var origin = new Vector2Int(x, y);
                if (!shape.Values.Any(cell => blocked.Contains(origin + cell)))
                    return origin;
            }
        }
    }

    private static void Place(Dictionary<QuestDef, Vector2Int> shape, Vector2Int origin, HashSet<Vector2Int> taken)
    {
        foreach (var pair in shape)
        {
            SetPosition(pair.Key, origin + pair.Value);
            taken.Add(origin + pair.Value);
        }
    }

    private static int Order(QuestDef definition) => _registeredAt.TryGetValue(definition.id, out var at) ? at : int.MaxValue;

    private static HashSet<Vector2Int> BlockedCells(List<QuestDef> quests, Dictionary<string, QuestDef> byId)
    {
        var blocked = new HashSet<Vector2Int>(quests.Select(q => q.TreePos));
        foreach (var quest in quests.Where(q => q.hasPosInBalance))
        {
            foreach (var parentId in quest.parents)
            {
                if (!byId.TryGetValue(parentId, out var parent) || !parent.hasPosInBalance)
                    continue;

                for (var y = parent.TreePos.y + 1; y < quest.TreePos.y; y++)
                    blocked.Add(new Vector2Int(parent.TreePos.x, y));
            }
        }

        return blocked;
    }

    private static void SetPosition(QuestDef definition, Vector2Int position)
    {
        _posX(definition) = position.x;
        _posY(definition) = position.y;
    }

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
        var stubbed = 0;
        foreach (var data in quests.questCollection.quests)
        {
            if (data is null || GameBalance.Me.GetDataOrNull<QuestDef>(data.id) is not null)
                continue;

            var stub = CreateDefinition(data.id);
            stub.hasPosInBalance = false;
            FillBlanks(stub);
            if (_stubs.TryGetValue(data.id, out var old))
                old.Dispose();

            _stubs[data.id] = TimbnBalance.Add(stub);
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

        if (TimbnBalance.Loaded is { } balance)
            Relayout(balance, null, quests.questCollection.questsCache);
    }

    internal static void OnGameStarted() => RelayoutIfLoaded();

    internal static IDisposable HideForSave(GameSave save)
    {
        var cache = save.questSystemData?.questCollection?.questsCache;
        if (cache is null)
            return new TimbnUndo(() => { });

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
}
