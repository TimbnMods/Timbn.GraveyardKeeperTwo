using HarmonyLib;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnReflectionTests
{
    [Fact]
    public void AMissingFieldGivesNull() =>
        Assert.Null(TimbnReflection.TryField<Sample, int>("_renamedByAnUpdate"));

    [Fact]
    public void AFoundMethodBecomesADelegate()
    {
        var doubled = TimbnReflection.TryDelegate<Func<Sample, int, int>>(() => AccessTools.Method(typeof(Sample), "Double"), "Sample.Double");

        Assert.NotNull(doubled);
        Assert.Equal(6, doubled!(new Sample(), 3));
    }

    [Fact]
    public void AMethodThatIsNotFoundGivesNull() =>
        Assert.Null(TimbnReflection.TryDelegate<Func<Sample, int, int>>(() => null, "Sample.Gone"));

    [Fact]
    public void AFinderThatThrowsGivesNull() =>
        Assert.Null(TimbnReflection.TryDelegate<Func<Sample, int, int>>(() => throw new InvalidOperationException(), "Sample.Broken"));

    [Fact]
    public void AMethodWithTheWrongShapeGivesNull() =>
        Assert.Null(TimbnReflection.TryDelegate<Func<Sample, string, string>>(() => AccessTools.Method(typeof(Sample), "Double"), "Sample.Double"));

    private sealed class Sample
    {
        private int Double(int value) => value * 2;
    }
}
