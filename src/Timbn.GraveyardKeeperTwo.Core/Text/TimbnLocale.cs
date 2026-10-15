using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Writes the text plugins added into the game's loaded language and puts the shipped text back when it is removed.
/// Text from a plugin's lang files always wins over text added in code for the same key.
/// </summary>
internal static class TimbnLocale
{
    internal const string DefaultLanguage = LLBase.DEFAULT_LANGUAGE;

    private static readonly AccessTools.FieldRef<LL?> _currentLang =
        AccessTools.StaticFieldRefAccess<LL?>(AccessTools.Field(typeof(LLBase), "currentLang"));

    private static readonly AccessTools.FieldRef<LLBase, List<string>> _txtIds =
        AccessTools.FieldRefAccess<LLBase, List<string>>("txtIds");

    private static readonly AccessTools.FieldRef<LLBase, List<string>> _txts =
        AccessTools.FieldRefAccess<LLBase, List<string>>("txts");

    private static readonly Dictionary<string, List<Entry>> _entries = [];
    private static readonly HashSet<string> _warnedKeys = [];
    private static readonly Dictionary<string, Func<string, string>> _fillers = [];
    private static int _scratchSlot;

    internal static IDisposable Add(string key, string text) => Add(new Dictionary<string, string> { [key] = text });

    internal static IDisposable Add(IReadOnlyDictionary<string, string> texts) =>
        Add(texts.Select(pair => new Entry(pair.Key, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [DefaultLanguage] = pair.Value }, fromFile: false)));

    internal static IDisposable AddFromFiles(IReadOnlyDictionary<string, Dictionary<string, string>> textsByKey) =>
        Add(textsByKey.Select(pair => new Entry(pair.Key, pair.Value, fromFile: true)));

    internal static string FileText(string key) =>
        FileTextOrNull(key) ?? throw new ArgumentException($"The text key '{key}' has no line in the plugin's lang/{DefaultLanguage}.txt.");

    internal static string? FileTextOrNull(string key) =>
        _entries.TryGetValue(key, out var stack) && stack.FindLast(e => e.FromFile) is { } entry ? entry.TextFor(DefaultLanguage) : null;

    internal static string? Scratch(string text)
    {
        if (_currentLang() is not { } lang)
            return null;

        var key = $"timbn_scratch_line_{_scratchSlot++ % 8}";
        Write(lang, new Entry(key, new Dictionary<string, string> { [DefaultLanguage] = text }, fromFile: false));
        return key;
    }

    internal static IDisposable Fill(IReadOnlyCollection<string> keys, Func<string, string> fill)
    {
        foreach (var key in keys)
            _fillers[key] = fill;

        Rewrite(keys);
        return new TimbnUndo(() =>
        {
            foreach (var key in keys)
            {
                if (_fillers.TryGetValue(key, out var current) && current == fill)
                    _fillers.Remove(key);
            }

            Rewrite(keys);
        });
    }

    internal static void Rewrite(IEnumerable<string> keys)
    {
        if (_currentLang() is not { } lang)
            return;

        foreach (var key in keys)
        {
            if (_entries.TryGetValue(key, out var stack) && stack.Count > 0)
                Write(lang, Current(stack));
        }
    }

    internal static void WarnIfMissing(string key)
    {
        if (!LLBase.IsCurrentLangLoaded || LLBase.HasL(key) || !_warnedKeys.Add(key))
            return;

        TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnLocale)}|The key '{key}' has no text in the current language, so the game shows the key itself. Add it with Text.Add or a lang file.");
    }

    private static IDisposable Add(IEnumerable<Entry> entries)
    {
        var added = entries.ToList();
        foreach (var entry in added)
        {
            if (!_entries.TryGetValue(entry.Key, out var stack))
                _entries[entry.Key] = stack = [];

            stack.Add(entry);
        }

        if (_currentLang() is { } lang)
        {
            foreach (var entry in added)
                Write(lang, Current(_entries[entry.Key]));
        }

        return new TimbnUndo(() =>
        {
            foreach (var entry in added)
                Remove(entry);
        });
    }

    internal static void ApplyTo(LLBase lang)
    {
        foreach (var stack in _entries.Values)
            Write(lang, Current(stack));
    }

    private static Entry Current(List<Entry> stack) => stack.FindLast(e => e.FromFile) ?? stack[^1];

    private static void Write(LLBase lang, Entry entry)
    {
        var raw = entry.TextFor(lang.id);
        if (_fillers.TryGetValue(entry.Key, out var fill))
            raw = fill(raw);

        var text = TimbnLocaleMarkup.Parse(lang, entry.Key, raw, out var nested, out var replacement);
        lang.dictionary[entry.Key] = text;
        lang.idsToMetaInfo[entry.Key] = nested;
        if (replacement is null)
            lang.replacementIdMetaInfo.Remove(entry.Key);
        else
            lang.replacementIdMetaInfo[entry.Key] = replacement;
    }

    private static void Remove(Entry entry)
    {
        if (!_entries.TryGetValue(entry.Key, out var stack))
            return;

        var wasCurrent = Current(stack) == entry;
        if (!stack.Remove(entry) || !wasCurrent)
            return;

        var lang = _currentLang();
        if (stack.Count > 0)
        {
            if (lang != null)
                Write(lang, Current(stack));

            return;
        }

        _entries.Remove(entry.Key);
        if (lang == null)
            return;

        var index = _txtIds(lang).IndexOf(entry.Key);
        if (index == -1)
        {
            lang.dictionary.Remove(entry.Key);
            lang.idsToMetaInfo.Remove(entry.Key);
            lang.replacementIdMetaInfo.Remove(entry.Key);
            return;
        }

        lang.dictionary[entry.Key] = _txts(lang)[index];
        lang.idsToMetaInfo[entry.Key] = lang.nestedLocalesMetaInfos[index];
        var shipped = lang.replacementKeysMetaInfoList.Find(r => r.id == entry.Key);
        if (shipped != null)
            lang.replacementIdMetaInfo[entry.Key] = shipped;
        else
            lang.replacementIdMetaInfo.Remove(entry.Key);
    }

    private sealed class Entry
    {
        private readonly IReadOnlyDictionary<string, string> _texts;

        public Entry(string key, IReadOnlyDictionary<string, string> texts, bool fromFile)
        {
            Key = key;
            _texts = texts;
            FromFile = fromFile;
        }

        public string Key { get; }

        public bool FromFile { get; }

        public string TextFor(string language)
        {
            if (_texts.TryGetValue(language, out var text) || _texts.TryGetValue(DefaultLanguage, out text))
                return text;

            return _texts.Values.FirstOrDefault() ?? Key;
        }
    }
}
