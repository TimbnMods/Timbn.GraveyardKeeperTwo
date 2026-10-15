using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSavesTests
{
    [Fact]
    public void ASecondEntryWithTheSameIdIsRefused()
    {
        using var first = TimbnSaves.Register(new TimbnSaveData<Sample>("saves.tests.same", null));

        var ex = Assert.Throws<InvalidOperationException>(() => TimbnSaves.Register(new TimbnSaveData<Sample>("saves.tests.same", null)));
        Assert.Contains("saves.tests.same", ex.Message);
    }

    [Fact]
    public void DisposingFreesTheId()
    {
        var first = TimbnSaves.Register(new TimbnSaveData<Sample>("saves.tests.freed", null));
        first.Dispose();

        using var second = TimbnSaves.Register(new TimbnGlobalData<Sample>("saves.tests.freed", null));
    }

    [Fact]
    public void DifferentIdsLiveSideBySide()
    {
        using var first = TimbnSaves.Register(new TimbnSaveData<Sample>("saves.tests.one", null));
        using var second = TimbnSaves.Register(new TimbnSaveData<Sample>("saves.tests.two", null));
    }

    [Fact]
    public void RegisteringAtTheMainMenuLeavesTheDataAlone()
    {
        var data = new TimbnSaveData<Sample>("saves.tests.untouched", null);
        data.Current.Days = 7;

        using var handle = TimbnSaves.Register(data);

        Assert.Equal(7, data.Current.Days);
        Assert.Null(TimbnSaves.CurrentPath);
    }

    [Fact]
    public void TheSerializerReplacesCollectionsInsteadOfAppending()
    {
        var sample = JObject.Parse("{\"Names\":[\"a\"]}").ToObject<Sample>(TimbnSaves.Serializer);

        Assert.Equal(["a"], sample!.Names);
    }

    public sealed class Sample
    {
        public int Days { get; set; }

        public List<string> Names { get; set; } = ["default"];
    }
}
