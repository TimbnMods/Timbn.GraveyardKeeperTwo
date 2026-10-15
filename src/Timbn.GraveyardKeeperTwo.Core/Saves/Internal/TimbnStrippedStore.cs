using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>One piece of mod content kept out of the game's save, with where it was and its saved form.</summary>
internal sealed class TimbnStrippedEntry
{
    public const string Player = "player";
    public const string WorldObject = "wgo";
    public const string Vendor = "vendor";
    public const string Drop = "drop";
    public const string Perk = "perk";
    public const string Craft = "craft";
    public const string Station = "station";
    public const string Zone = "zone";
    public const string Link = "link";
    public const string Worker = "worker";
    public const string Activity = "activity";
    public const string Point = "point";
    public const string Part = "part";
    public const string Filter = "filter";
    public const string Pinned = "pinned";
    public const string Quest = "quest";

    public const string QueueMember = "craftElementsQueue";
    public const string LastStartedMember = "lastStartedCraftWithRequirements";

    public string Root { get; set; } = "";

    public string Id { get; set; } = "";

    public string Scene { get; set; } = "";

    public string Owner { get; set; } = "";

    public string Member { get; set; } = "";

    public List<int> Path { get; set; } = [];

    public string Blob { get; set; } = "";

    public TimbnPerkEnding? Ending { get; set; }

    public TimbnCraftQueueState? Queue { get; set; }

    public TimbnWorkerAttachment? Attachment { get; set; }

    public bool IsItem => Root is Player or WorldObject or Vendor;

    public bool IsStationPart => Root is Station or Zone or Link or Worker or Activity or Point;
}

/// <summary>What a buff does when it ends, kept with it so the buff can still end properly once its mod is gone.</summary>
internal sealed class TimbnPerkEnding
{
    public string SetResources { get; set; } = "";

    public string AddResources { get; set; } = "";

    public List<string> Expressions { get; set; } = [];
}

/// <summary>A station's craft queue state as it was before its mod crafts came out, so the queue can pick up where it was.</summary>
internal sealed class TimbnCraftQueueState
{
    public int CurrentIndex { get; set; }

    public int Status { get; set; }

    public bool HasPreFinishUpdate { get; set; }

    public int PrevQueueCount { get; set; }
}

/// <summary>Which station a zombie worked at and as what, kept while the station is out of the save.</summary>
internal sealed class TimbnWorkerAttachment
{
    public string Station { get; set; } = "";

    public int ZombieType { get; set; }

    public string DockParent { get; set; } = "";

    // Whether the zombie was locked when the station came out, so it is locked again when the station goes back.
    public bool Locked { get; set; }
}

/// <summary>
/// The stripped content of the last few saves of a slot, each under the save date the game wrote in its info file and
/// the playthrough it belongs to. A load restores the set of the exact save being loaded. When there is none because
/// the last saves were made without Core, it falls back to the newest older set of the same playthrough.
/// </summary>
internal sealed class TimbnStrippedStore
{
    internal const int Kept = 4;

    private readonly List<StrippedSet> _sets = [];
    private readonly List<JObject> _unreadable = [];

    public bool IsEmpty => _sets.Count == 0;

    public IReadOnlyList<string> Stamps => _sets.Select(set => set.Stamp).ToList();

    public static TimbnStrippedStore FromJson(JToken? token)
    {
        var store = new TimbnStrippedStore();
        if (token?["sets"] is not JArray sets)
            return store;

        foreach (var set in sets.OfType<JObject>())
        {
            var stamp = (string?)set["stamp"];
            if (string.IsNullOrEmpty(stamp))
                continue;

            List<TimbnStrippedEntry> entries;
            try
            {
                entries = set["entries"]?.ToObject<List<TimbnStrippedEntry>>() ?? [];
            }
            catch (Exception)
            {
                // Kept as it was and written back, so a set this version cannot read is never lost.
                store._unreadable.Add((JObject)set.DeepClone());
                continue;
            }

            DateTime? savedAt = DateTime.TryParse((string?)set["savedAt"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ? parsed : null;
            store._sets.Add(new StrippedSet(stamp!, savedAt, (string?)set["playthrough"] ?? "", entries));
        }

        return store;
    }

    public JObject ToJson() => new()
    {
        ["sets"] = new JArray(_sets.Select(set => new JObject
        {
            ["stamp"] = set.Stamp,
            ["savedAt"] = set.SavedAt?.ToString("o", CultureInfo.InvariantCulture),
            ["playthrough"] = set.Playthrough,
            ["entries"] = JArray.FromObject(set.Entries),
        }).Concat(_unreadable.Select(set => set.DeepClone()))),
    };

    /// <summary>Keeps a save's set as the newest, replacing one with the same date, and drops the oldest past <see cref="Kept"/>.</summary>
    public void Add(string stamp, DateTime? savedAt, string playthrough, List<TimbnStrippedEntry> entries)
    {
        _sets.RemoveAll(set => set.Stamp == stamp);
        _sets.Insert(0, new StrippedSet(stamp, savedAt, playthrough, entries));
        if (_sets.Count > Kept)
            _sets.RemoveRange(Kept, _sets.Count - Kept);
    }

    public List<TimbnStrippedEntry>? Find(string stamp) => _sets.FirstOrDefault(set => set.Stamp == stamp)?.Entries;

    /// <summary>
    /// Picks the set for the save being loaded. The set of that exact save wins. Without one, the newest set of the same
    /// playthrough saved before it is used, which is the last save made with Core before saves made without it. Never
    /// a newer set, since loading an older backup must not bring back items from later, and never another
    /// playthrough's.
    /// </summary>
    public (List<TimbnStrippedEntry> Entries, string Stamp, bool Exact)? Pick(string stamp, DateTime? loadedAt, string playthrough)
    {
        var exact = _sets.FirstOrDefault(set => set.Stamp == stamp && !OtherPlaythrough(set, playthrough));
        if (exact is not null)
            return (exact.Entries, exact.Stamp, true);

        if (loadedAt is not { } loaded || string.IsNullOrEmpty(playthrough))
            return null;

        var older = _sets
            .Where(set => set.Playthrough == playthrough && set.SavedAt is { } saved && saved < loaded)
            .OrderByDescending(set => set.SavedAt)
            .FirstOrDefault();
        return older is null ? null : (older.Entries, older.Stamp, false);
    }

    private static bool OtherPlaythrough(StrippedSet set, string playthrough) =>
        set.Playthrough.Length > 0 && playthrough.Length > 0 && set.Playthrough != playthrough;

    /// <summary>
    /// Orders entries so putting each back at its recorded index rebuilds the lists exactly. Stations go first, since
    /// a craft or an item stripped out of one later can only go back once the station is in the world again, then
    /// every container in turn, shallow places first, and lower indices first within one list.
    /// </summary>
    public static List<TimbnStrippedEntry> RestoreOrder(IEnumerable<TimbnStrippedEntry> entries) =>
        entries
            .OrderBy(entry => entry.Root == TimbnStrippedEntry.Station ? 0 : 1)
            .ThenBy(entry => entry.Root, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scene, StringComparer.Ordinal)
            .ThenBy(entry => entry.Owner, StringComparer.Ordinal)
            .ThenBy(entry => entry.Member, StringComparer.Ordinal)
            .ThenBy(entry => entry.Path.Count)
            .ThenBy(entry => entry.Path, PathComparer.Instance)
            .ToList();

    private sealed class StrippedSet(string stamp, DateTime? savedAt, string playthrough, List<TimbnStrippedEntry> entries)
    {
        public string Stamp { get; } = stamp;

        public DateTime? SavedAt { get; } = savedAt;

        public string Playthrough { get; } = playthrough;

        public List<TimbnStrippedEntry> Entries { get; } = entries;
    }

    private sealed class PathComparer : IComparer<List<int>>
    {
        public static readonly PathComparer Instance = new();

        public int Compare(List<int>? x, List<int>? y)
        {
            x ??= [];
            y ??= [];
            for (var i = 0; i < Math.Min(x.Count, y.Count); i++)
            {
                var compared = x[i].CompareTo(y[i]);
                if (compared != 0)
                    return compared;
            }

            return x.Count.CompareTo(y.Count);
        }
    }
}
