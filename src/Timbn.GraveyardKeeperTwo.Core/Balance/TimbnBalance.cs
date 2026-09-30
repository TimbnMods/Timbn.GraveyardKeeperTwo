using LazyBearTechnology;
using System.Collections;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds definitions to the game's balance tables, before or after they load, so GameBalance.Me.GetData
/// finds them. Disposing the result removes the definition.
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

    private static readonly AccessTools.FieldRef<ItemDef, string> _itemCustomIcon = AccessTools.FieldRefAccess<ItemDef, string>("customIcon");
    private static readonly AccessTools.FieldRef<ItemDef, bool> _itemPerksCached = AccessTools.FieldRefAccess<ItemDef, bool>("perksCached");

    private static readonly List<Entry> _entries = [];
    private static readonly List<IEdit> _edits = [];

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

    internal static IDisposable Add(BalanceBaseObject definition, Func<GameBalance, bool>? prepare, Action<GameBalance>? removed)
    {
        var entry = new Entry(definition, prepare, removed);
        _entries.Add(entry);
        if (Loaded is { } balance && Insert(balance, entry))
            RefreshDerivedCaches(balance, definition.GetType());

        return new TimbnUndo(() => Remove(entry));
    }

    internal static IDisposable Edit<T>(string id, Action<T> apply, Action<T> revert, ManualLogSource logger) where T : BalanceBaseObject
    {
        var edit = new DefinitionEdit<T>(id, apply, revert, logger);
        _edits.Add(edit);
        if (Loaded is { } balance)
            edit.Apply(balance);

        return new TimbnUndo(() =>
        {
            _edits.Remove(edit);
            edit.Revert();
        });
    }

    internal static void OnBalanceLoaded(GameBalance balance)
    {
        var touched = new HashSet<Type>();
        foreach (var entry in _entries.ToList())
        {
            if (Insert(balance, entry))
                touched.Add(entry.Definition.GetType());
        }

        if (touched.Remove(typeof(ItemDef)))
            touched.Add(typeof(CraftDef));

        foreach (var type in touched)
            RefreshDerivedCaches(balance, type);

        foreach (var edit in _edits.ToList())
            edit.Apply(balance);
    }

    internal static void RefreshQuestCaches(GameBalance balance) => _createQuestsCache(balance);

    /// <summary>
    /// Rebuilds the game's lookups of which station makes what and which items belong to which group, after a mod
    /// changed crafts or item groups in place, and has every station in the loaded save pick up the new crafts.
    /// Adding a CraftDef or ItemDef with the plugin's Balance.Add already does this.
    /// </summary>
    public static void RefreshCraftCaches()
    {
        if (Loaded is not { } balance)
            return;

        _createCraftGroupsCache(balance);
        _createCraftCache(balance);
        _createAutopsyCraftsCache(balance);
        balance.craftInItemsCache.Clear();
        balance.craftInItemsCacheShownInTooltips.Clear();
        _createCraftInItemsCache(balance);

        foreach (var wgo in TimbnWorld.All())
            wgo.CraftComponent?.ResetCraftsFromBalanceCache();
    }

    /// <summary>
    /// Changes an item's icon to any sprite the game has, or one added with the plugin's Sprites.AddPng. The game
    /// keeps the icon id in two places, so setting iconId alone leaves the old icon in some windows.
    /// </summary>
    /// <example>
    /// <code>
    /// Balance.Edit&lt;ItemDef&gt;("cooked_fish", fish => TimbnBalance.SetIcon(fish, "timbn_grilled_fish"), fish => TimbnBalance.SetIcon(fish, "i_cooked_fish"));
    /// </code>
    /// </example>
    /// <param name="item">The item to change.</param>
    /// <param name="iconId">The sprite name.</param>
    public static void SetIcon(ItemDef item, string iconId)
    {
        item.iconId = iconId;
        _itemCustomIcon(item) = iconId;
    }

    /// <summary>
    /// Reads the id an item's icon is drawn from, the one <see cref="SetIcon"/> writes, which can differ from its
    /// iconId. Save it before changing the icon to put it back later.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The sprite name the item is drawn with.</returns>
    public static string GetIcon(ItemDef item) => _itemCustomIcon(item) is { Length: > 0 } icon ? icon : item.iconId;

    /// <summary>
    /// Makes the game read an item's use effects again. The game remembers which buffs an item gives the first
    /// time it looks, so a change to onUseExpressions is not seen until this is called. The plugin's Balance.Edit
    /// on an ItemDef calls it for you after apply and revert.
    /// </summary>
    /// <param name="item">The item whose use effects changed.</param>
    public static void ForgetPerks(ItemDef item) => _itemPerksCached(item) = false;

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
        if (existing != -1 && !_entries.Any(e => e != entry && e.Definition == list[existing]))
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnBalance)}|The game already has a {definition.GetType().Name} with id '{definition.id}'.");
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

        list[existing] = definition;
        return true;
    }

    private static void Remove(Entry entry)
    {
        _entries.Remove(entry);
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
            _createQuestsCache(balance);
        else if (type == typeof(CraftDef) || type == typeof(ItemDef))
            RefreshCraftCaches();
    }

    private interface IEdit
    {
        void Apply(GameBalance balance);

        void Revert();
    }

    private sealed class DefinitionEdit<T>(string id, Action<T> apply, Action<T> revert, ManualLogSource logger) : IEdit
        where T : BalanceBaseObject
    {
        private T? _applied;

        public void Apply(GameBalance balance)
        {
            var definition = balance.GetDataOrNull<T>(id);
            if (definition == null)
            {
                logger.LogWarning($"{nameof(TimbnBalance)}|The balance has no {typeof(T).Name} '{id}' to change.");
                return;
            }

            if (ReferenceEquals(definition, _applied))
                return;

            _applied = definition;
            Run(apply, definition, "changing");
        }

        public void Revert()
        {
            var applied = _applied;
            _applied = null;
            if (applied != null && Loaded?.GetDataOrNull<T>(id) == applied)
                Run(revert, applied, "restoring");
        }

        private void Run(Action<T> action, T definition, string doing)
        {
            TimbnSafe.Run(() => action(definition), logger, $"{nameof(TimbnBalance)}|{doing} {typeof(T).Name} '{id}'");
            if (definition is ItemDef item)
                ForgetPerks(item);
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
