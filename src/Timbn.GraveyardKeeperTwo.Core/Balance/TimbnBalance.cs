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

    private static readonly AccessTools.FieldRef<GameBalanceBase, List<IList>> _datas =
        AccessTools.FieldRefAccess<GameBalanceBase, List<IList>>("datas");

    private static readonly AccessTools.FieldRef<GameBalanceBase, List<Type>> _types =
        AccessTools.FieldRefAccess<GameBalanceBase, List<Type>>("types");

    private static readonly AccessTools.FieldRef<GameBalanceBase, List<Dictionary<string, int>>> _cache =
        AccessTools.FieldRefAccess<GameBalanceBase, List<Dictionary<string, int>>>("cache");

    private static readonly Action<GameBalance> _createQuestsCache =
        AccessTools.MethodDelegate<Action<GameBalance>>(AccessTools.Method(typeof(GameBalance), "CreateQuestsCache"));

    private static readonly List<Entry> _entries = [];

    /// <summary>The balance if the game has loaded it, or null. Unlike GameBalance.Me it never forces a load.</summary>
    public static GameBalance? Loaded => _instance();

    internal static IDisposable Add(BalanceBaseObject definition) => Add(definition, null);

    internal static IDisposable Add(BalanceBaseObject definition, Action<GameBalance>? prepare)
    {
        var entry = new Entry(definition, prepare);
        _entries.Add(entry);
        if (Loaded is { } balance)
        {
            Insert(balance, entry);
            RefreshDerivedCaches(balance, definition.GetType());
        }

        return new TimbnUndo(() => Remove(entry));
    }

    internal static void OnBalanceLoaded(GameBalance balance)
    {
        var touched = new HashSet<Type>();
        foreach (var entry in _entries)
        {
            if (Insert(balance, entry))
                touched.Add(entry.Definition.GetType());
        }

        foreach (var type in touched)
            RefreshDerivedCaches(balance, type);
    }

    internal static void RefreshQuestCaches(GameBalance balance) => _createQuestsCache(balance);

    private static bool Insert(GameBalance balance, Entry entry)
    {
        var definition = entry.Definition;
        var index = _types(balance).IndexOf(definition.GetType());
        if (index == -1)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnBalance)}|The balance has no table for {definition.GetType().Name}.");
            return false;
        }

        var list = _datas(balance)[index];
        if (list.Contains(definition))
            return false;

        entry.Prepare?.Invoke(balance);
        var existing = FindById(list, definition.id);
        if (existing == -1)
        {
            list.Add(definition);
            _cache(balance)[index][definition.id] = list.Count - 1;
            return true;
        }

        if (!_entries.Any(e => e != entry && e.Definition == list[existing]))
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnBalance)}|The game already has a {definition.GetType().Name} with id '{definition.id}'.");
            return false;
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

        var list = _datas(balance)[index];
        var at = list.IndexOf(definition);
        if (at == -1)
            return;

        list.RemoveAt(at);
        var cache = _cache(balance)[index];
        cache.Clear();
        for (var i = 0; i < list.Count; i++)
            cache[((BalanceBaseObject)list[i]).id] = i;

        RefreshDerivedCaches(balance, definition.GetType());
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
    }

    private sealed class Entry
    {
        public Entry(BalanceBaseObject definition, Action<GameBalance>? prepare)
        {
            Definition = definition;
            Prepare = prepare;
        }

        public BalanceBaseObject Definition { get; }

        public Action<GameBalance>? Prepare { get; }
    }
}
