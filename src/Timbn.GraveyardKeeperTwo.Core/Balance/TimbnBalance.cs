using LazyBearTechnology;
using System.Collections;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Helpers for the game's balance tables. A mod adds and edits definitions through its Balance property. This class
/// holds the static pieces around that, like checking whether the balance has loaded, swapping item icons and
/// building game expressions.
/// </summary>
public static class TimbnBalance
{
    private static readonly AccessTools.FieldRef<GameBalance?> _instance =
        AccessTools.StaticFieldRefAccess<GameBalance?>(AccessTools.Field(typeof(GameBalance), "instance"));

    private static readonly AccessTools.FieldRef<GameBalanceBase, List<Type>> _types =
        AccessTools.FieldRefAccess<GameBalanceBase, List<Type>>("types");

    private static readonly AccessTools.FieldRef<GameBalanceBase, List<Dictionary<string, int>>> _cache =
        AccessTools.FieldRefAccess<GameBalanceBase, List<Dictionary<string, int>>>("cache");

    private static readonly Action<GameBalance> _createQuestsCache = CacheBuilder("CreateQuestsCache");
    private static readonly Action<GameBalance> _createCraftCache = CacheBuilder("CreateCraftCache");
    private static readonly Action<GameBalance> _createAutopsyCraftsCache = CacheBuilder("CreateAutopsyCraftsCache");
    private static readonly Action<GameBalance> _createCraftGroupsCache = CacheBuilder("CreateCraftGroupsCache");
    private static readonly Action<GameBalance> _createCraftInItemsCache = CacheBuilder("CreateCraftInItemsCache");
    private static readonly Action<GameBalance> _createWorkbenchExtensionsCache = CacheBuilder("CreateWorkbenchExtensionsCache");
    private static readonly Action<GameBalance> _createBuildCache = CacheBuilder("CreateBuildCache");
    private static readonly Action<GameBalance> _createCustomBuildAreaCache = CacheBuilder("CreateCustomBuildAreaIdToWgoIdsCache");

    private static readonly AccessTools.FieldRef<ObjectLinkedToDefinition<WGODef>, string> _worldObjectCachedId =
        AccessTools.FieldRefAccess<ObjectLinkedToDefinition<WGODef>, string>("cachedId");

    private static readonly AccessTools.FieldRef<ItemDef, string> _itemCustomIcon = AccessTools.FieldRefAccess<ItemDef, string>("customIcon");
    private static readonly AccessTools.FieldRef<ItemDef, bool> _itemPerksCached = AccessTools.FieldRefAccess<ItemDef, bool>("perksCached");

    private static readonly List<Entry> _entries = [];

    // Removed during an F6 reload and still in the game until the reloaded plugins have added theirs.
    private static readonly List<Entry> _held = [];
    private static bool _holding;
    private static readonly List<IEdit> _edits = [];

    // Copies waiting at a balance load for a template another plugin adds. Only filled while a load resolves them.
    private static readonly HashSet<BalanceBaseObject> _deferred = [];
    private static bool _resolving;

    /// <summary>The balance if the game has loaded it, or null. Unlike GameBalance.Me it never forces a load.</summary>
    public static GameBalance? Loaded => _instance();

    internal static IDisposable Add(BalanceBaseObject definition) => Add(definition, null, null);

    internal static IDisposable Add(BalanceBaseObject definition, Action<GameBalance>? prepare)
    {
        Func<GameBalance, bool>? ready = prepare is null
            ? null
            : balance =>
            {
                prepare(balance);
                return true;
            };
        return Add(definition, ready, null);
    }

    internal static IDisposable Add(BalanceBaseObject definition, Func<GameBalance, bool>? prepare, Action<GameBalance>? removed) =>
        Add(definition, prepare, removed, true);

    // For adding many at once. The caller rebuilds the derived caches itself afterward.
    internal static IDisposable AddWithoutRefresh(BalanceBaseObject definition) => Add(definition, null, null, false);

    private static IDisposable Add(BalanceBaseObject definition, Func<GameBalance, bool>? prepare, Action<GameBalance>? removed, bool refresh)
    {
        var entry = new Entry(definition, prepare, removed);
        _entries.Add(entry);
        if (Loaded is { } balance && Insert(balance, entry))
        {
            if (refresh)
                RefreshDerivedCaches(balance, definition.GetType());

            RefreshLoadedObjects(definition);
        }

        return new TimbnUndo(() => Remove(entry));
    }

    internal static IDisposable AddCopy<T>(string templateId, string id, Action<T>? edit) where T : BalanceBaseObject, new() =>
        AddCopy<T>(_ => templateId, id, edit is null ? null : (_, definition) => edit(definition));

    // A false from accept keeps the definition out of the balance, and the caller says why.
    internal static IDisposable AddCopy<T>(Func<GameBalance, string> templateIdOf, string id, Action<GameBalance, T>? edit, Func<GameBalance, bool>? accept = null) where T : BalanceBaseObject, new()
    {
        var definition = new T { id = id };
        return Add(definition, balance =>
        {
            if (accept != null && !accept(balance))
                return false;

            var templateId = templateIdOf(balance);
            var template = balance.GetDataOrNull<T>(templateId);
            if (template == null && _resolving && IsPending<T>(balance, templateId))
            {
                _deferred.Add(definition);
                return false;
            }

            if (template == null)
            {
                TimbnCorePlugin.Logger.LogError($"{nameof(TimbnBalance)}|There is no {typeof(T).Name} '{templateId}' to copy for '{id}'.");
                return false;
            }

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), definition);
            definition.id = id;
            edit?.Invoke(balance, definition);
            // The copy is the same object on every load and its perk cache is not serialized, so it would keep the old use effects.
            if (definition is ItemDef item)
                RefreshUseEffects(item);

            return true;
        }, null);
    }

    internal static IDisposable AddItem(string templateItemId, string id, string name, string description, string iconId, Action<ItemDef>? edit)
    {
        List<IDisposable> undo = [];
        var texts = TimbnStationRules.Texts((id, name), (TimbnStationRules.DescriptionKey(id), description));
        if (texts.Count > 0)
            undo.Add(TimbnLocale.Add(texts));

        undo.Add(AddCopy<ItemDef>(templateItemId, id, item =>
        {
            if (iconId.Length > 0)
                SetIcon(item, iconId);

            edit?.Invoke(item);
        }));
        return TimbnUndo.All(undo);
    }

    /// <summary>
    /// Builds a game expression that holds a plain text value, for a field that names an id but is typed as an
    /// expression, such as WGODef.replaceToWgoOnDie, WGODef.customAssetId or CraftDef.customCraftResultIcon. The game
    /// stores those as pure string values, and <c>new LazyExpression("grave_top_stn_1")</c> would try to evaluate the id
    /// as a formula and come out empty.
    /// </summary>
    /// <param name="value">The text, usually an id.</param>
    /// <returns>An expression that evaluates to the text.</returns>
    public static LazyExpression TextExpression(string value) => TimbnExpression.PureText(value);

    /// <summary>
    /// Builds a game expression from a formula the way the balance parses its own, for an expression field such as
    /// ItemDef.onUseExpressions, ExpressionGameRes.expression or CraftDef.onCraftEndExpressions. Unlike
    /// <c>new LazyExpression(formula)</c>, a #perk or $resource reference in it works, so
    /// <c>TimbnBalance.Expression("20+2*$food_eng_buff")</c> reads the player's food buff.
    /// </summary>
    /// <param name="formula">The formula, for example <c>AddPerk("buff_talent_blue_1")</c> or <c>3+2*#perk_cook_2</c>.</param>
    /// <returns>The expression.</returns>
    /// <exception cref="ArgumentException">The formula is empty.</exception>
    public static LazyExpression Expression(string formula) => TimbnExpression.Formula(formula);

    internal static IDisposable Edit<T>(string id, Action<T> apply, Action<T> revert, ManualLogSource logger) where T : BalanceBaseObject
    {
        var edit = new DefinitionEdit<T>(id, apply, revert, logger);
        _edits.Add(edit);
        if (Loaded is { } balance && edit.Apply(balance))
            RefreshDerivedCaches(balance, typeof(T));

        return new TimbnUndo(() =>
        {
            _edits.Remove(edit);
            if (edit.Revert() && Loaded is { } loaded)
                RefreshDerivedCaches(loaded, typeof(T));
        });
    }

    // True when a registered but not yet inserted definition will provide the template.
    private static bool IsPending<T>(GameBalance balance, string id) where T : BalanceBaseObject =>
        _entries.Any(entry => entry.Definition is T && entry.Definition.id == id && !balance.GetDataCollection(typeof(T)).Contains(entry.Definition));

    // Edits go in as soon as their definition is there, so a copy takes the edited template the way it did when
    // plugins registered one at a time, even when it copies another plugin's definition that a third one edits. A
    // copy of another plugin's definition waits until that one is in, whatever order they registered in.
    internal static void OnBalanceLoaded(GameBalance balance)
    {
        var touched = new HashSet<Type>();
        ApplyReadyEdits(balance, touched);

        var pending = _entries.ToList();
        _resolving = true;
        try
        {
            while (pending.Count > 0)
            {
                var progress = false;
                List<Entry> waiting = [];
                foreach (var entry in pending)
                {
                    if (Insert(balance, entry))
                    {
                        touched.Add(entry.Definition.GetType());
                        progress = true;
                        ApplyReadyEdits(balance, touched);
                    }
                    else if (_deferred.Remove(entry.Definition))
                        waiting.Add(entry);
                }

                if (waiting.Count == 0)
                    break;

                if (!progress)
                {
                    // Nothing provides these templates, so let each one report it.
                    _resolving = false;
                    foreach (var entry in waiting)
                        Insert(balance, entry);

                    break;
                }

                pending = waiting;
            }
        }
        finally
        {
            _resolving = false;
            _deferred.Clear();
        }

        // Every edit runs before the caches are rebuilt, so a changed craft or quest shows up in them.
        foreach (var edit in _edits.ToList())
        {
            if (edit.Apply(balance))
                touched.Add(edit.DefinitionType);
        }

        if (touched.Remove(typeof(ItemDef)))
            touched.Add(typeof(CraftDef));

        foreach (var type in touched)
            RefreshDerivedCaches(balance, type);
    }

    private static void ApplyReadyEdits(GameBalance balance, HashSet<Type> touched)
    {
        foreach (var edit in _edits.ToList().Where(edit => edit.CanApply(balance)))
        {
            if (edit.Apply(balance))
                touched.Add(edit.DefinitionType);
        }
    }

    internal static void RefreshQuestCaches(GameBalance balance) => _createQuestsCache(balance);

    internal static HashSet<string> AddedIds<T>() where T : BalanceBaseObject =>
        [.. _entries.Where(entry => entry.Definition is T).Select(entry => entry.Definition.id)];

    internal static HashSet<string> AllAddedIds() => [.. _entries.Select(entry => entry.Definition.id)];

    /// <summary>Whether a mod added the definition through Core, counting one held over an F6 reload.</summary>
    internal static bool IsAdded(BalanceBaseObject definition) =>
        _entries.Any(entry => entry.Definition == definition) || _held.Any(entry => entry.Definition == definition);

    // Rebuilds which station makes what and which items belong to which group, and has every station in the loaded
    // save pick up the new crafts.
    internal static void RefreshCraftCaches()
    {
        if (Loaded is not { } balance)
            return;

        _createCraftGroupsCache(balance);
        _createCraftCache(balance);
        _createAutopsyCraftsCache(balance);
        balance.craftInItemsCache.Clear();
        balance.craftInItemsCacheShownInTooltips.Clear();
        _createCraftInItemsCache(balance);

        foreach (var wgo in TimbnWorld.Objects())
            wgo.CraftComponent?.ResetCraftsFromBalanceCache();
    }

    /// <summary>
    /// Changes an item's icon to any sprite the game has or one added with Sprites.AddEmbeddedPng. It sets the
    /// item's iconId and also its custom icon, so the two always match.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// var original = "";
    /// Balance.Edit<ItemDef>(
    ///     "fish_roach",
    ///     fish => { original = TimbnBalance.GetIcon(fish); TimbnBalance.SetIcon(fish, "timbn_roach"); },
    ///     fish => TimbnBalance.SetIcon(fish, original));
    /// ]]></code>
    /// </example>
    /// <param name="item">The item to change.</param>
    /// <param name="iconId">The sprite name.</param>
    public static void SetIcon(ItemDef item, string iconId)
    {
        item.iconId = iconId;
        _itemCustomIcon(item) = iconId;
    }

    /// <summary>
    /// Reads an item's custom icon, or its iconId when it has none. Save it before <see cref="SetIcon"/> to put the
    /// icon back later.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The sprite name the item is drawn with.</returns>
    public static string GetIcon(ItemDef item) => _itemCustomIcon(item) is { Length: > 0 } icon ? icon : item.iconId;

    // The game remembers which buffs an item gives the first time it looks, so a change to onUseExpressions is not
    // seen until this clears that.
    internal static void RefreshUseEffects(ItemDef item) => _itemPerksCached(item) = false;

    private static Action<GameBalance> CacheBuilder(string name) =>
        AccessTools.MethodDelegate<Action<GameBalance>>(AccessTools.Method(typeof(GameBalance), name));

    private static bool Insert(GameBalance balance, Entry entry)
    {
        var definition = entry.Definition;
        var index = _types(balance).IndexOf(definition.GetType());
        if (index == -1)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnBalance)}|The balance has no table for {definition.GetType().Name}.");
            return false;
        }

        var list = balance.GetDataCollection(definition.GetType());
        if (list.Contains(definition))
            return false;

        var existing = FindById(list, definition.id);
        if (existing != -1 && !_entries.Any(e => e != entry && e.Definition == list[existing]) && !_held.Any(e => e.Definition == list[existing]))
        {
            // Forgotten, not just refused, or the vanilla id would count as the mod's and its items would be taken
            // out of every save.
            _entries.Remove(entry);
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnBalance)}|The game already has a {definition.GetType().Name} with id '{definition.id}', so it is not added.");
            return false;
        }

        if (entry.Prepare is { } prepare && !prepare(balance))
            return false;

        if (existing == -1)
        {
            list.Add(definition);
            _cache(balance)[index][definition.id] = list.Count - 1;
            return true;
        }

        // A reloaded plugin's copy takes the place of its held one, which then has nothing left to clean up.
        var replaced = list[existing];
        _held.RemoveAll(held => held.Definition == replaced);
        list[existing] = definition;
        return true;
    }

    /// <summary>
    /// Keeps the definitions of mods removed from now on in the game until <see cref="ReleaseHeldRemovals"/>. ScriptEngine
    /// destroys the old plugins a frame before it adds the new ones, and the game would meet a queued craft or a placed
    /// station with no definition in that frame.
    /// </summary>
    internal static void HoldRemovals() => _holding = true;

    /// <summary>Removes every held definition no reloaded mod added again, like an unload would have.</summary>
    internal static void ReleaseHeldRemovals()
    {
        _holding = false;
        var held = _held.ToList();
        _held.Clear();
        foreach (var entry in held)
            Drop(entry);
    }

    private static void Remove(Entry entry)
    {
        _entries.Remove(entry);
        if (_holding)
        {
            _held.Add(entry);
            return;
        }

        Drop(entry);
    }

    private static void Drop(Entry entry)
    {
        if (Loaded is not { } balance)
            return;

        var definition = entry.Definition;
        var index = _types(balance).IndexOf(definition.GetType());
        if (index == -1)
            return;

        var list = balance.GetDataCollection(definition.GetType());
        var at = list.IndexOf(definition);
        if (at == -1)
            return;

        list.RemoveAt(at);
        var cache = _cache(balance)[index];
        cache.Clear();
        for (var i = 0; i < list.Count; i++)
            cache[((BalanceBaseObject)list[i]).id] = i;

        RefreshDerivedCaches(balance, definition.GetType());
        TimbnSafe.Run(() => entry.Removed?.Invoke(balance), $"{nameof(TimbnBalance)}|Cleaning up after {definition.GetType().Name} '{definition.id}'");
    }

    private static int FindById(IList list, string id)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (((BalanceBaseObject)list[i]).id == id)
                return i;
        }

        return -1;
    }

    private static void RefreshDerivedCaches(GameBalance balance, Type type)
    {
        if (type == typeof(QuestDef))
        {
            TimbnQuestRuntime.RefreshCaches(balance);
        }
        else if (type == typeof(CraftDef) || type == typeof(ItemDef))
        {
            RefreshCraftCaches();
            // The craft window reads this for every extension row, so a craft with an extensionNeedId is missing without it.
            if (type == typeof(CraftDef))
                _createWorkbenchExtensionsCache(balance);
        }
        else if (type == typeof(WGODef))
        {
            _createWorkbenchExtensionsCache(balance);
        }
        else if (type == typeof(BuildingDef))
        {
            _createBuildCache(balance);
            _createCustomBuildAreaCache(balance);
        }
    }

    private static void RefreshLoadedObjects(BalanceBaseObject definition)
    {
        if (definition is not WGODef || !TimbnGame.IsInGame)
            return;

        // A world object keeps the definition object it first read until its id changes, so a station built before a
        // hot reload would keep the old copy. Init is also where a station copies its tick length from the definition.
        foreach (var wgo in TimbnWorld.Objects(wgo => wgo.id == definition.id).ToList())
        {
            _worldObjectCachedId(wgo) = "";
            wgo.CraftComponent?.Init(wgo);
        }
    }

    private interface IEdit
    {
        Type DefinitionType { get; }

        bool CanApply(GameBalance balance);

        bool Apply(GameBalance balance);

        bool Revert();
    }

    private sealed class DefinitionEdit<T>(string id, Action<T> apply, Action<T> revert, ManualLogSource logger) : IEdit
        where T : BalanceBaseObject
    {
        private T? _applied;

        public Type DefinitionType => typeof(T);

        public bool CanApply(GameBalance balance) => balance.GetDataOrNull<T>(id) != null;

        public bool Apply(GameBalance balance)
        {
            var definition = balance.GetDataOrNull<T>(id);
            if (definition == null)
            {
                logger.LogWarning($"{nameof(TimbnBalance)}|The balance has no {typeof(T).Name} '{id}' to change.");
                return false;
            }

            if (ReferenceEquals(definition, _applied))
                return false;

            _applied = definition;
            Run(apply, definition, "changing");
            return true;
        }

        public bool Revert()
        {
            var applied = _applied;
            _applied = null;
            if (applied == null || Loaded?.GetDataOrNull<T>(id) != applied)
                return false;

            Run(revert, applied, "restoring");
            return true;
        }

        private void Run(Action<T> action, T definition, string doing)
        {
            TimbnSafe.Run(() => action(definition), logger, $"{nameof(TimbnBalance)}|{doing} {typeof(T).Name} '{id}'");
            if (definition is ItemDef item)
                RefreshUseEffects(item);
        }
    }

    private sealed class Entry
    {
        public Entry(BalanceBaseObject definition, Func<GameBalance, bool>? prepare, Action<GameBalance>? removed)
        {
            Definition = definition;
            Prepare = prepare;
            Removed = removed;
        }

        public BalanceBaseObject Definition { get; }

        public Func<GameBalance, bool>? Prepare { get; }

        public Action<GameBalance>? Removed { get; }
    }
}
