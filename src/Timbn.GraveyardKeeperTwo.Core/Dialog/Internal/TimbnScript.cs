using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Records a conversation once, with its keys and text, so each talk replays it into a fresh conversation.</summary>
internal sealed class TimbnScript
{
    private static readonly Regex _keyName = new("^[a-z][a-z0-9_]*$");

    private readonly Counter _counter;

    internal TimbnScript(string prefix, Dictionary<string, string> texts)
    {
        _counter = new Counter(prefix);
        Texts = texts;
    }

    private TimbnScript(Counter counter, Dictionary<string, string> texts)
    {
        _counter = counter;
        Texts = texts;
    }

    internal Dictionary<string, string> Texts { get; }

    internal List<Action<TimbnConversation>> Steps { get; } = [];

    internal TimbnLineKey Line(string text) => new(Named($"{_counter.Prefix}_{++_counter.Count}", text), _counter.Count);

    internal TimbnLineKey NamedLine(string name, string text) => new(Named(KeyFor(name), text), 0);

    internal TimbnLineKey FileLine(string name) => new(FileNamed(KeyFor(name)), 0);

    internal string FileNamed(string key) => Named(key, TimbnLocale.FileText(key));

    internal void Rename(TimbnLineKey line, string name)
    {
        if (TimbnGameTexts.IsKey(line.Value))
            return;

        var key = KeyFor(name);
        var text = Texts[line.Value];
        Named(key, text);
        Texts.Remove(line.Value);
        if (line.Number != 0 && line.Number == _counter.Count)
            _counter.Count--;

        line.Value = key;
    }

    internal string Named(string key, string text)
    {
        if (TimbnGameTexts.IsKey(text))
            return text;

        if (Texts.ContainsKey(key))
            throw new InvalidOperationException($"The key '{key}' is made twice. Give each talk and quest its own id.");

        Texts[key] = text;
        return key;
    }

    internal TimbnScript Nested() => new(_counter, Texts);

    private string KeyFor(string name) =>
        _keyName.IsMatch(name)
            ? $"{_counter.Prefix}_{name}"
            : throw new ArgumentException($"The key name '{name}' can only hold lowercase letters, digits and underscores, and starts with a letter.");

    internal TimbnScript Child(string prefix) => new(prefix, Texts);

    internal void Replay(TimbnConversation conversation, int skip = 0)
    {
        foreach (var step in Steps.Skip(skip))
            step(conversation);
    }

    private sealed class Counter
    {
        public Counter(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; }

        public int Count { get; set; }
    }
}
