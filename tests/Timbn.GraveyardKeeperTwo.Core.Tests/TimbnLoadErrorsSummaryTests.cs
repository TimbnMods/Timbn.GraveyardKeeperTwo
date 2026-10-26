using BepInEx;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLoadErrorsSummaryTests : IDisposable
{
    private const string _closing = "BepInEx/LogOutput.log has the details.";

    private static readonly Dictionary<string, PluginInfo> _noPlugins = [];

    public TimbnLoadErrorsSummaryTests() => TimbnLoadErrors.Clear();

    public void Dispose() => TimbnLoadErrors.Clear();

    private static string[] Lines(IEnumerable<string> errors) =>
        (TimbnLoadErrors.Describe(errors, _noPlugins) ?? "").Split("\n\n");

    [Fact]
    public void AnIgnoredErrorAloneGivesNoPopup() =>
        Assert.Null(TimbnLoadErrors.Describe(["Plugin [Mod A] targets a wrong version of BepInEx (6.0.0) and might not work until you update"], _noPlugins));

    [Fact]
    public void LinesComeInOrderDependenciesThenStartsThenFeaturesThenProblems()
    {
        TimbnLoadErrors.AddProblem("A problem.");
        TimbnLoadErrors.AddFeatureFailure("Mod", "Feature");
        TimbnLoadErrors.AddStartFailure("Other");

        var lines = Lines(["Could not load [Mod A 1.0.0] because it is incompatible with: other.mod"]);

        Assert.StartsWith("Mod A 1.0.0 can't run", lines[0]);
        Assert.StartsWith("Other is off", lines[1]);
        Assert.StartsWith("Mod: Feature is off", lines[2]);
        Assert.Equal("A problem.", lines[3]);
        Assert.Equal(_closing, lines[4]);
    }

    [Fact]
    public void MoreThanSixLinesAreCountedInstead()
    {
        for (var i = 1; i <= 9; i++)
            TimbnLoadErrors.AddProblem($"Problem {i}.");

        var lines = Lines([]);

        Assert.Equal(8, lines.Length);
        Assert.Equal("Problem 6.", lines[5]);
        Assert.Equal("And 3 more.", lines[6]);
        Assert.Equal(_closing, lines[7]);
    }
}
