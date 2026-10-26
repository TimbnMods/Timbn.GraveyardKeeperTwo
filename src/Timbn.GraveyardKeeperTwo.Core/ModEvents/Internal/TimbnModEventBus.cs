using Newtonsoft.Json;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The ids mods publish and listen to, and the calls between them.</summary>
internal static class TimbnModEventBus
{
    private const int _maxDepth = 8;

    private static readonly Dictionary<string, Entry> _entries = [];
    private static int _depth;

    internal static IReadOnlyList<TimbnModEvent> Known =>
        [.. _entries.Values.Select(entry => new TimbnModEvent(entry.Id, [.. entry.Publishers])).OrderBy(known => known.Id, StringComparer.Ordinal)];

    internal static IDisposable AddPublisher(string id, string mod)
    {
        var entry = EntryFor(id);
        entry.Publishers.Add(mod);
        return new TimbnUndo(() =>
        {
            entry.Publishers.Remove(mod);
            Prune(entry);
        });
    }

    internal static IDisposable Listen<T>(string id, Action<T> handler, ManualLogSource logger) =>
        Add(id, new Listener(
            (json, payload) =>
            {
                if (payload is T same)
                {
                    handler(same);
                    return;
                }

                if (json() is not { } text)
                    return;

                // An event sent with nothing in it has no T to hand over, so a typed listener sits it out.
                var (ok, value) = Read<T>(text, id, logger);
                if (ok && value != null)
                    handler(value);
            },
            logger,
            TimbnSafe.Describe(handler)));

    internal static IDisposable Listen(string id, Action handler, ManualLogSource logger) =>
        Add(id, new Listener((_, _) => handler(), logger, TimbnSafe.Describe(handler)));

    internal static IDisposable Listen(string id, Action<string> handler, ManualLogSource logger) =>
        Add(id, new Listener(
            (json, _) =>
            {
                if (json() is { } text)
                    handler(text);
            },
            logger,
            TimbnSafe.Describe(handler)));

    internal static void Publish(string id, object? payload, ManualLogSource logger)
    {
        if (!_entries.TryGetValue(id, out var entry) || entry.Listeners.Count == 0)
            return;

        if (_depth >= _maxDepth)
        {
            logger.LogWarning($"{nameof(TimbnPluginModEvents)}|The mod event '{id}' was published from inside {_maxDepth} others and was dropped.");
            return;
        }

        // Only a listener that needs the text makes it, once, and a payload that cannot become JSON is the publisher's to hear about.
        var serialized = false;
        string? json = null;
        string? Json()
        {
            if (serialized)
                return json;

            serialized = true;
            try
            {
                json = Serialize(payload);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"{nameof(TimbnPluginModEvents)}|The mod event '{id}' could not be written as JSON, so listeners that read it as text or another type skipped it. {ex.Message}");
            }

            return json;
        }

        _depth++;
        try
        {
            foreach (var listener in entry.Listeners.ToArray())
                TimbnSafe.Run(() => listener.Deliver(Json, payload), listener.Logger, $"{nameof(TimbnPluginModEvents)}|Listener {listener.Name} of '{id}'");
        }
        finally
        {
            _depth--;
        }
    }

    internal static string Serialize(object? payload) => payload == null ? "null" : JsonConvert.SerializeObject(payload);

    internal static (bool Ok, T? Value) Read<T>(string json, string id, ManualLogSource logger)
    {
        try
        {
            return (true, JsonConvert.DeserializeObject<T>(json));
        }
        catch (JsonException ex)
        {
            logger.LogWarning($"{nameof(TimbnPluginModEvents)}|The mod event '{id}' did not fit {typeof(T).Name}, so it was skipped. {ex.Message}");
            return (false, default);
        }
    }

    private static IDisposable Add(string id, Listener listener)
    {
        var entry = EntryFor(id);
        entry.Listeners.Add(listener);
        return new TimbnUndo(() =>
        {
            entry.Listeners.Remove(listener);
            Prune(entry);
        });
    }

    private static Entry EntryFor(string id)
    {
        if (!_entries.TryGetValue(id, out var entry))
            _entries[id] = entry = new Entry(id);

        return entry;
    }

    private static void Prune(Entry entry)
    {
        if (entry.Listeners.Count == 0 && entry.Publishers.Count == 0)
            _entries.Remove(entry.Id);
    }

    private sealed class Entry(string id)
    {
        public string Id { get; } = id;

        public List<string> Publishers { get; } = [];

        public List<Listener> Listeners { get; } = [];
    }

    private sealed class Listener(Action<Func<string?>, object?> deliver, ManualLogSource logger, string name)
    {
        public Action<Func<string?>, object?> Deliver { get; } = deliver;

        public ManualLogSource Logger { get; } = logger;

        public string Name { get; } = name;
    }
}
