using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnLocale
{
    internal const string DefaultLanguage = LLBase.DEFAULT_LANGUAGE;

    private static readonly AccessTools.FieldRef<LL?> _currentLang =
        AccessTools.StaticFieldRefAccess<LL?>(AccessTools.Field(typeof(LLBase), "currentLang"));

    private static readonly AccessTools.FieldRef<LLBase, List<string>> _txtIds =
        AccessTools.FieldRefAccess<LLBase, List<string>>("txtIds");

    private static readonly AccessTools.FieldRef<LLBase, List<string>> _txts =
        AccessTools.FieldRefAccess<LLBase, List<string>>("txts");

    private static readonly Dictionary<string, Entry> _entries = [];

    internal static IDisposable Add(string key, string text) => Add(new Dictionary<string, string> { [key] = text });

    internal static IDisposable Add(IReadOnlyDictionary<string, string> texts) =>
        Add(texts.Select(pair => new Entry(pair.Key, new Dictionary<string, string> { [DefaultLanguage] = pair.Value })));

    internal static IDisposable Add(IReadOnlyDictionary<string, Dictionary<string, string>> textsByKey) =>
        Add(textsByKey.Select(pair => new Entry(pair.Key, pair.Value)));

    private static IDisposable Add(IEnumerable<Entry> entries)
    {
        var added = entries.ToList();
        foreach (var entry in added)
            _entries[entry.Key] = entry;

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
        lang.dictionary[entry.Key] = entry.TextFor(lang.id);
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
        private readonly IReadOnlyDictionary<string, string> _texts;

        public Entry(string key, IReadOnlyDictionary<string, string> texts)
        {
            Key = key;
            _texts = texts;
        }

        public string Key { get; }

        public string TextFor(string language)
        {
            if (_texts.TryGetValue(language, out var text) || _texts.TryGetValue(DefaultLanguage, out text))
                return text;

            return _texts.Values.FirstOrDefault() ?? Key;
        }
    }
}
