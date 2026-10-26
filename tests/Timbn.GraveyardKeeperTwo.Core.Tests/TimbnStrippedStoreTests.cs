namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnStrippedStoreTests
{
    private const string _ours = "0778bb11-90a3-4566-a805-be99c5db8edf";
    private const string _theirs = "5f1c2a90-0000-4000-8000-000000000000";

    private static readonly DateTime _morning = new(2026, 10, 8, 9, 0, 0);

    private static TimbnStrippedEntry Entry(string id, params int[] path) =>
        new() { Root = TimbnStrippedEntry.Player, Member = "inventory", Id = id, Path = [.. path] };

    private static void Save(TimbnStrippedStore store, int minute, string playthrough, params TimbnStrippedEntry[] entries) =>
        store.Add($"save at {minute}", _morning.AddMinutes(minute), playthrough, [.. entries]);

    [Fact]
    public void PickTakesTheExactSaveFirst()
    {
        var store = new TimbnStrippedStore();
        Save(store, 1, _ours, Entry("first", 0));
        Save(store, 2, _ours, Entry("second", 0));

        var picked = store.Pick("save at 1", _morning.AddMinutes(1), _ours)!.Value;

        Assert.True(picked.Exact);
        Assert.Equal("first", Assert.Single(picked.Entries).Id);
    }

    [Fact]
    public void ASaveMadeWithoutCoreGetsTheLastSetBeforeIt()
    {
        var store = new TimbnStrippedStore();
        Save(store, 1, _ours, Entry("first", 0));
        Save(store, 2, _ours, Entry("second", 0));

        var picked = store.Pick("save at 30", _morning.AddMinutes(30), _ours)!.Value;

        Assert.False(picked.Exact);
        Assert.Equal("save at 2", picked.Stamp);
        Assert.Equal("second", Assert.Single(picked.Entries).Id);
    }

    [Fact]
    public void AnOlderBackupNeverGetsALaterSet()
    {
        var store = new TimbnStrippedStore();
        Save(store, 10, _ours, Entry("later", 0));

        Assert.Null(store.Pick("save at 5", _morning.AddMinutes(5), _ours));
    }

    [Fact]
    public void NoFallbackWithoutAPlaythroughOrADate()
    {
        var store = new TimbnStrippedStore();
        store.Add("old", null, "", [Entry("old", 0)]);
        Save(store, 1, _ours, Entry("ours", 0));

        Assert.Null(store.Pick("save at 30", null, _ours));
        Assert.Null(store.Pick("save at 30", _morning.AddMinutes(30), ""));
        Assert.Equal("old", Assert.Single(store.Pick("old", null, "")!.Value.Entries).Id);
        Assert.Null(store.Pick("save at 1", _morning.AddMinutes(1), _theirs));
        Assert.Null(store.Pick("save at 30", _morning.AddMinutes(30), _theirs));
    }

    [Fact]
    public void RestoringInOrderRebuildsNestedListsExactly()
    {
        var bag = new Node("bag", [new Node("mod in bag"), new Node("bread")]);
        var original = new List<Node> { new("mod first"), bag, new("stick"), new("mod last") };
        var expected = Flatten(original);

        var stripped = Strip(original, []);
        Assert.Equal(["bag(bread)", "stick"], Flatten(original));

        foreach (var entry in TimbnStrippedStore.RestoreOrder(stripped.Select(taken => taken.Entry)))
        {
            var list = original;
            foreach (var index in entry.Path.Take(entry.Path.Count - 1))
                list = list[index].Children;

            list.Insert(entry.Path[^1], stripped.First(taken => taken.Entry == entry).Node);
        }

        Assert.Equal(expected, Flatten(original));
    }

    private static List<(TimbnStrippedEntry Entry, Node Node)> Strip(List<Node> list, List<int> path)
    {
        List<(TimbnStrippedEntry, Node)> taken = [];
        for (var i = 0; i < list.Count; i++)
        {
            if (!list[i].IsMod)
                taken.AddRange(Strip(list[i].Children, [.. path, i]));
        }

        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (!list[i].IsMod)
                continue;

            taken.Add((Entry(list[i].Name, [.. path, i]), list[i]));
            list.RemoveAt(i);
        }

        return taken;
    }

    private static List<string> Flatten(List<Node> list) =>
        list.Select(node => node.Children.Count == 0 ? node.Name : $"{node.Name}({string.Join(",", Flatten(node.Children))})").ToList();

    private sealed class Node(string name, List<Node>? children = null)
    {
        public string Name { get; } = name;

        public List<Node> Children { get; } = children ?? [];

        public bool IsMod => Name.StartsWith("mod", StringComparison.Ordinal);
    }

    [Fact]
    public void StationsGoBackBeforeWhatWasTakenOutOfThem()
    {
        var craft = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Craft, Scene = "RuinedTemple", Owner = "station", Member = TimbnStrippedEntry.QueueMember, Path = [0], Id = "mod_pie" };
        var itemInStation = new TimbnStrippedEntry { Root = TimbnStrippedEntry.WorldObject, Scene = "RuinedTemple", Owner = "station", Member = "inventory", Path = [0], Id = "mod_item" };
        var zone = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Zone, Scene = "RuinedTemple", Owner = "graveyard", Member = "wgoDataList", Path = [0, 3], Id = "mod_station" };
        var point = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Point, Scene = "RuinedTemple", Owner = "station", Path = [40], Id = "mod_station", Blob = "0" };
        var station = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Station, Scene = "RuinedTemple", Owner = "station", Path = [7], Id = "mod_station" };

        var ordered = TimbnStrippedStore.RestoreOrder([craft, point, itemInStation, zone, station]);

        Assert.Same(station, ordered[0]);
        Assert.Equal([TimbnStrippedEntry.Station, TimbnStrippedEntry.Craft, TimbnStrippedEntry.Point, TimbnStrippedEntry.WorldObject, TimbnStrippedEntry.Zone], ordered.Select(entry => entry.Root));
    }

    [Fact]
    public void ZoneEntriesOfOneListRestoreLowerIndicesFirst()
    {
        var later = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Zone, Scene = "s", Owner = "graveyard", Member = "wgoDataList", Path = [1, 5], Id = "b" };
        var earlier = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Zone, Scene = "s", Owner = "graveyard", Member = "wgoDataList", Path = [1, 2], Id = "a" };
        var otherZone = new TimbnStrippedEntry { Root = TimbnStrippedEntry.Zone, Scene = "s", Owner = "church", Member = "wgoDataList", Path = [0, 9], Id = "c" };

        var ordered = TimbnStrippedStore.RestoreOrder([later, earlier, otherZone]);

        Assert.Equal(["c", "a", "b"], ordered.Select(entry => entry.Id));
    }
}
