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
    public void FindsTheSetOfTheSameSave()
    {
        var store = new TimbnStrippedStore();
        Save(store, 1, _ours, Entry("potion", 0));
        Save(store, 2, _ours, Entry("drink", 1));

        Assert.Equal("potion", Assert.Single(store.Find("save at 1")!).Id);
        Assert.Equal("drink", Assert.Single(store.Find("save at 2")!).Id);
        Assert.Null(store.Find("save at 9"));
    }

    [Fact]
    public void KeepsOnlyTheNewestSets()
    {
        var store = new TimbnStrippedStore();
        for (var minute = 1; minute <= TimbnStrippedStore.Kept + 2; minute++)
            Save(store, minute, _ours);

        Assert.Equal(["save at 6", "save at 5", "save at 4", "save at 3"], store.Stamps);
    }

    [Fact]
    public void SavingTwiceInOneSecondKeepsTheLaterSet()
    {
        var store = new TimbnStrippedStore();
        Save(store, 1, _ours, Entry("old", 0));
        Save(store, 1, _ours, Entry("new", 0));

        Assert.Equal(["save at 1"], store.Stamps);
        Assert.Equal("new", Assert.Single(store.Find("save at 1")!).Id);
    }

    [Fact]
    public void RoundTripsThroughJson()
    {
        var store = new TimbnStrippedStore();
        var entry = new TimbnStrippedEntry { Root = TimbnStrippedEntry.WorldObject, Scene = "RuinedTemple", Owner = "guid", Member = "inventory", Path = [2, 0], Id = "potion", Blob = "AAEC" };
        store.Add("a", _morning, _ours, [entry]);

        var read = TimbnStrippedStore.FromJson(store.ToJson());

        var kept = Assert.Single(read.Find("a")!);
        Assert.Equal(TimbnStrippedEntry.WorldObject, kept.Root);
        Assert.Equal("RuinedTemple", kept.Scene);
        Assert.Equal("guid", kept.Owner);
        Assert.Equal([2, 0], kept.Path);
        Assert.Equal("AAEC", kept.Blob);
        Assert.False(read.Pick("later", _morning.AddMinutes(5), _ours)!.Value.Exact);
    }

    [Fact]
    public void MissingOrBrokenSectionGivesAnEmptyStore()
    {
        Assert.True(TimbnStrippedStore.FromJson(null).IsEmpty);
        Assert.True(TimbnStrippedStore.FromJson(new Newtonsoft.Json.Linq.JObject { ["sets"] = "nope" }).IsEmpty);
    }

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
    public void AnotherPlaythroughNeverGetsOurSets()
    {
        var store = new TimbnStrippedStore();
        Save(store, 1, _ours, Entry("ours", 0));

        Assert.Null(store.Pick("save at 1", _morning.AddMinutes(1), _theirs));
        Assert.Null(store.Pick("save at 30", _morning.AddMinutes(30), _theirs));
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

    [Fact]
    public void RoundTripsCraftAndWorkerStateThroughJson()
    {
        var store = new TimbnStrippedStore();
        var craft = new TimbnStrippedEntry
        {
            Root = TimbnStrippedEntry.Craft,
            Scene = "RuinedTemple",
            Owner = "station",
            Member = TimbnStrippedEntry.QueueMember,
            Path = [2],
            Id = "mod_pie",
            Blob = "AAEC",
            Queue = new TimbnCraftQueueState { CurrentIndex = 2, Status = 1, HasPreFinishUpdate = true, PrevQueueCount = 3 },
        };
        var worker = new TimbnStrippedEntry
        {
            Root = TimbnStrippedEntry.Worker,
            Scene = "RuinedTemple",
            Owner = "zombie",
            Id = "mod_station",
            Attachment = new TimbnWorkerAttachment { Station = "guid", ZombieType = 1, DockParent = "guid", Locked = true },
        };
        store.Add("a", _morning, _ours, [craft, worker]);

        var read = TimbnStrippedStore.FromJson(store.ToJson()).Find("a")!;

        var keptCraft = read.Single(entry => entry.Root == TimbnStrippedEntry.Craft);
        Assert.Equal(2, keptCraft.Queue!.CurrentIndex);
        Assert.Equal(1, keptCraft.Queue.Status);
        Assert.True(keptCraft.Queue.HasPreFinishUpdate);
        Assert.Equal(3, keptCraft.Queue.PrevQueueCount);
        Assert.Null(keptCraft.Attachment);
        var keptWorker = read.Single(entry => entry.Root == TimbnStrippedEntry.Worker);
        Assert.Equal("guid", keptWorker.Attachment!.Station);
        Assert.Equal(1, keptWorker.Attachment.ZombieType);
        Assert.True(keptWorker.Attachment.Locked);
        Assert.True(keptWorker.IsStationPart);
        Assert.Null(keptWorker.Queue);
    }

    [Fact]
    public void KeepsASetItCannotReadWhenWritingBack()
    {
        var json = Newtonsoft.Json.Linq.JObject.Parse(
            "{\"sets\":[{\"stamp\":\"good\",\"entries\":[]},{\"stamp\":\"bad\",\"entries\":\"not a list\"}]}");

        var store = TimbnStrippedStore.FromJson(json);
        var written = store.ToJson();

        Assert.Equal(["good"], store.Stamps);
        Assert.Equal(2, ((Newtonsoft.Json.Linq.JArray)written["sets"]!).Count);
        Assert.Equal("not a list", (string?)written["sets"]![1]!["entries"]);
    }
}
