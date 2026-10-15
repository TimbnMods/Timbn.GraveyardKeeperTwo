using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnDataTests
{
    [Fact]
    public void StartsWithAFreshObject()
    {
        var data = new TimbnSaveData<Sample>("mod.a", null);

        Assert.NotNull(data.Current);
        Assert.Equal(1, data.Current.Days);
        Assert.Equal("mod.a", data.Id);
    }

    [Fact]
    public void LoadingNothingGivesAFreshObject()
    {
        var data = new TimbnSaveData<Sample>("mod.a", null);
        data.Current.Days = 9;

        Entry(data).Load(null);

        Assert.Equal(1, data.Current.Days);
    }

    [Fact]
    public void LoadingASectionFillsTheObject()
    {
        var data = new TimbnSaveData<Sample>("mod.a", null);

        Entry(data).Load(new JObject { ["Days"] = 4, ["Names"] = new JArray("herm") });

        Assert.Equal(4, data.Current.Days);
        Assert.Equal(["herm"], data.Current.Names);
    }

    [Fact]
    public void LoadedCollectionsReplaceTheDefaultsInsteadOfAddingToThem()
    {
        var data = new TimbnSaveData<Sample>("mod.a", null);

        Entry(data).Load(new JObject { ["Names"] = new JArray("linda") });

        Assert.Equal(["linda"], data.Current.Names);
    }

    [Fact]
    public void SerializeRoundTrips()
    {
        var data = new TimbnSaveData<Sample>("mod.a", null);
        data.Current.Days = 12;
        data.Current.Names.Add("larry");

        var token = Entry(data).Serialize();
        var copy = new TimbnSaveData<Sample>("mod.b", null);
        Entry(copy).Load(token);

        Assert.Equal(12, copy.Current.Days);
        Assert.Equal(["default", "larry"], copy.Current.Names);
    }

    [Fact]
    public void ResetGivesAFreshObject()
    {
        var data = new TimbnGlobalData<Sample>("mod.a", null);
        var before = data.Current;
        before.Days = 3;

        Entry(data).Reset();

        Assert.NotSame(before, data.Current);
        Assert.Equal(1, data.Current.Days);
    }

    [Fact]
    public void TheLoadedHandlerGetsTheNewObject()
    {
        Sample? seen = null;
        var data = new TimbnSaveData<Sample>("mod.a", loaded => seen = loaded);

        Entry(data).Load(new JObject { ["Days"] = 2 });

        Assert.Same(data.Current, seen);
        Assert.Equal(2, seen!.Days);
    }

    [Fact]
    public void TheLoadedHandlerRunsOnAnEmptyLoadToo()
    {
        var calls = 0;
        var data = new TimbnSaveData<Sample>("mod.a", _ => calls++);

        Entry(data).Load(null);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void SaveDataHasNoFileAtTheMainMenu() =>
        Assert.Null(new TimbnSaveData<Sample>("mod.a", null).FilePath);

    [Fact]
    public void CoreSaveDataLoadsItsSets()
    {
        var data = new TimbnSaveData<TimbnCoreSaveData>("core", null);

        Entry(data).Load(new JObject
        {
            ["SeenTalks"] = new JArray("herm_hello"),
            ["QuestRuns"] = new JObject { ["q"] = new JObject { ["Day"] = 5, ["Npcs"] = new JObject { ["villager"] = "npc_larry" } } },
        });

        Assert.Equal(["herm_hello"], data.Current.SeenTalks);
        Assert.Empty(data.Current.QuestsReady);
        Assert.Equal(5, data.Current.QuestRuns["q"].Day);
        Assert.Equal("npc_larry", data.Current.QuestRuns["q"].Npcs["villager"]);
    }

    private static ITimbnSaveEntry Entry<T>(TimbnData<T> data) where T : class, new() => data;

    public sealed class Sample
    {
        public int Days { get; set; } = 1;

        public List<string> Names { get; set; } = ["default"];
    }
}
