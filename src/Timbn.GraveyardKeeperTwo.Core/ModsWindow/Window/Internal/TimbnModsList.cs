namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>One line of the mod list, a mod on its own or a group of them. Members are indexes into the list's mods.</summary>
internal sealed class TimbnModsListEntry
{
    public TimbnModsListEntry(string title, string subtitle, string? group, IReadOnlyList<int> members)
    {
        Title = title;
        Subtitle = subtitle;
        Group = group;
        Members = members;
    }

    public string Title { get; }

    public string Subtitle { get; }

    /// <summary>The group's key, or null for a mod on its own.</summary>
    public string? Group { get; }

    public IReadOnlyList<int> Members { get; }

    public int FirstMod => Members[0];
}

/// <summary>What the window shows, the chosen list entry and, for a group, the mod opened from its page.</summary>
internal readonly struct TimbnModsView
{
    public TimbnModsView(int entry, int? member)
    {
        Entry = entry;
        Member = member;
    }

    public int Entry { get; }

    public int? Member { get; }

    public TimbnModsView WithMember(int? member) => new(Entry, member);
}

/// <summary>
/// The mods in the order the Mods window lists them and the entries that go on its list. Core comes first, then each
/// group, then every other mod, and the order the catalog gave is kept inside each of those. A group is one entry that
/// holds all its mods, and a group with a single mod is that mod on its own. It holds no game state so it can be tested.
/// </summary>
internal sealed class TimbnModsList
{
    private TimbnModsList(IReadOnlyList<TimbnModsMod> mods, IReadOnlyList<TimbnModsListEntry> entries)
    {
        Mods = mods;
        Entries = entries;
    }

    public IReadOnlyList<TimbnModsMod> Mods { get; }

    public IReadOnlyList<TimbnModsListEntry> Entries { get; }

    // The mod whose settings show. On a group's page it is the group's first mod, which nothing reads.
    public int ModIndex(TimbnModsView view) => view.Member ?? Entries[view.Entry].FirstMod;

    public bool IsGroupPage(TimbnModsView view) => view.Member == null && Entries[view.Entry].Group != null;

    internal static TimbnModsList Build(IReadOnlyList<TimbnModsMod> catalog, Func<string, TimbnModGroup?> groupOf, string coreGuid)
    {
        // Core always stands first on its own, so a group it names would split around it.
        var groups = catalog.Select(mod => mod.Guid == coreGuid ? null : groupOf(mod.Guid)).ToList();
        var sizes = groups.Where(group => group != null).GroupBy(group => group!.Key).ToDictionary(set => set.Key, set => set.Count());

        // A group shows under the name its first mod gave.
        var names = new Dictionary<string, string>();
        foreach (var group in groups.Where(group => group != null))
            names.TryAdd(group!.Key, group.Name);

        string? Grouped(int i) => groups[i] is { } group && sizes[group.Key] > 1 ? group.Key : null;

        var firstOfGroup = new Dictionary<string, int>();
        for (var i = 0; i < catalog.Count; i++)
        {
            if (Grouped(i) is { } group)
                firstOfGroup.TryAdd(group, i);
        }

        // Core, then the groups where their first mod stood, then the rest, each in the catalog's order.
        var order = Enumerable.Range(0, catalog.Count)
            .OrderBy(i => catalog[i].Guid == coreGuid ? 0 : Grouped(i) != null ? 1 : 2)
            .ThenBy(i => Grouped(i) is { } group ? firstOfGroup[group] : 0)
            .ThenBy(i => i)
            .ToList();

        var mods = order.Select(i => catalog[i]).ToList();

        // A group's mods are next to each other by now, so the group takes as many mods as it has.
        var entries = new List<TimbnModsListEntry>();
        var position = 0;
        while (position < order.Count)
        {
            if (Grouped(order[position]) is not { } group)
            {
                entries.Add(new TimbnModsListEntry(mods[position].Name, mods[position].Version, null, [position]));
                position++;
                continue;
            }

            var members = Enumerable.Range(position, sizes[group]).ToList();
            entries.Add(new TimbnModsListEntry(names[group], string.Format(TimbnModsText.ModCount, members.Count), group, members));
            position += members.Count;
        }

        return new TimbnModsList(mods, entries);
    }
}
