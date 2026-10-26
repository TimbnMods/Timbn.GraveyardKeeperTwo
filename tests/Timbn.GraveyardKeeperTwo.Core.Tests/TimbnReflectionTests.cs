using HarmonyLib;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnReflectionTests
{
    [Fact]
    public void AnythingTheGameRenamedGivesNull()
    {
        Assert.Null(TimbnReflection.TryField<Sample, int>("_renamedByAnUpdate"));
        Assert.Null(TimbnReflection.TryDelegate<Func<Sample, int, int>>(() => null, "Sample.Gone"));
        Assert.Null(TimbnReflection.TryDelegate<Func<Sample, int, int>>(() => throw new InvalidOperationException(), "Sample.Broken"));
        Assert.Null(TimbnReflection.TryDelegate<Func<Sample, string, string>>(() => AccessTools.Method(typeof(Sample), "Double"), "Sample.Double"));
    }

    private sealed class Sample
    {
        private int Double(int value) => value * 2;
    }
}
