using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnLocale
{
    private static readonly AccessTools.FieldRef<LL?> _currentLang =
        AccessTools.StaticFieldRefAccess<LL?>(AccessTools.Field(typeof(LLBase), "currentLang"));

    private static readonly AccessTools.FieldRef<LLBase, List<string>> _txtIds =
        AccessTools.FieldRefAccess<LLBase, List<string>>("txtIds");

    private static readonly AccessTools.FieldRef<LLBase, List<string>> _txts =
        AccessTools.FieldRefAccess<LLBase, List<string>>("txts");

    private static readonly Dictionary<string, Entry> _entries = [];

    internal static IDisposable Add(string key, string text) => Add(new Dictionary<string, string> { [key] = text });

    internal static IDisposable Add(IReadOnlyDictionary<string, string> texts)
    {
        List<Entry> added = [];
        foreach (var pair in texts)
        {
            var entry = new Entry(pair.Key, pair.Value);
            _entries[pair.Key] = entry;
            added.Add(entry);
        }

        if (_currentLang() is { } lang)
        {
            foreach (var entry in added)
                Write(lang, entry);
        }

        return new TimbnUndo(() =>
        {
            foreach (var entry in added)
                Remove(entry);
        });
    }

    internal static void ApplyTo(LLBase lang)
    {
        foreach (var entry in _entries.Values)
            Write(lang, entry);
    }

    private static void Write(LLBase lang, Entry entry)
    {
        lang.dictionary[entry.Key] = entry.Text;
        lang.idsToMetaInfo[entry.Key] = new NestedLocalesMetaInfo();
        lang.replacementIdMetaInfo.Remove(entry.Key);
    }

    private static void Remove(Entry entry)
    {
        if (!_entries.TryGetValue(entry.Key, out var current) || current != entry)
            return;

        _entries.Remove(entry.Key);
        if (_currentLang() is not { } lang)
            return;

        var index = _txtIds(lang).IndexOf(entry.Key);
        if (index == -1)
        {
            lang.dictionary.Remove(entry.Key);
            lang.idsToMetaInfo.Remove(entry.Key);
            return;
        }

        lang.dictionary[entry.Key] = _txts(lang)[index];
        lang.idsToMetaInfo[entry.Key] = lang.nestedLocalesMetaInfos[index];
        var replacement = lang.replacementKeysMetaInfoList.Find(r => r.id == entry.Key);
        if (replacement != null)
            lang.replacementIdMetaInfo[entry.Key] = replacement;
    }

    private sealed class Entry
    {
        public Entry(string key, string text)
        {
            Key = key;
            Text = text;
        }

        public string Key { get; }

        public string Text { get; }
    }
}
