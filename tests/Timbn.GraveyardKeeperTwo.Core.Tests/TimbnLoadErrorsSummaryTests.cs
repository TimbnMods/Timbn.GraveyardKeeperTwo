using BepInEx;
using System.Collections;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLoadErrorsSummaryTests : IDisposable
{
    private const string _closing = "BepInEx/LogOutput.log has the details.";

    private static readonly Dictionary<string, PluginInfo> _noPlugins = [];

    public TimbnLoadErrorsSummaryTests() => Clear();

    public void Dispose() => Clear();

    private static void Clear()
    {
        foreach (var name in new[] { "_startFailures", "_featureFailures", "_problems" })
        {
            var field = typeof(TimbnLoadErrors).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!;
            ((IList)field.GetValue(null)!).Clear();
        }
    }

    private static string[] Lines(IEnumerable<string> errors) =>
        (TimbnLoadErrors.Describe(errors, _noPlugins) ?? "").Split("\n\n");

    [Fact]
    public void NothingWrongGivesNoPopup() =>
        Assert.Null(TimbnLoadErrors.Describe([], _noPlugins));

    [Fact]
    public void AnIgnoredErrorAloneGivesNoPopup() =>
        Assert.Null(TimbnLoadErrors.Describe(["Plugin [Mod A] targets a wrong version of BepInEx (6.0.0) and might not work until you update"], _noPlugins));

    [Fact]
    public void ADependencyErrorEndsWithThePointerToTheLog()
    {
        var lines = Lines(["Skipping [Timbn Campfire Meditation 1.2.1] because it has a dependency that was not loaded"]);

        Assert.Equal(["Timbn Campfire Meditation 1.2.1 is off because a mod it needs did not load.", _closing], lines);
    }

    [Fact]
    public void AStartFailureNamesThePlugin()
    {
        TimbnLoadErrors.AddStartFailure("Timbn Living Town");

        Assert.Equal(["Timbn Living Town is off because a game update changed something it needs.", _closing], Lines([]));
    }

    [Fact]
    public void FeatureFailuresAreGroupedPerPlugin()
    {
        TimbnLoadErrors.AddFeatureFailure("Timbn Vanilla Tweaks", "Stuck carriers");
        TimbnLoadErrors.AddFeatureFailure("Timbn Zombie Customizer", "Skins");
        TimbnLoadErrors.AddFeatureFailure("Timbn Vanilla Tweaks", "Quiet bells");

        Assert.Equal(
            [
                "Timbn Vanilla Tweaks: Stuck carriers and Quiet bells is off because a game update changed something it needs. The rest of the mod still works.",
                "Timbn Zombie Customizer: Skins is off because a game update changed something it needs. The rest of the mod still works.",
                _closing,
            ],
            Lines([]));
    }

    [Fact]
    public void ProblemsAreListedOnceEach()
    {
        TimbnLoadErrors.AddProblem("Herm has no home point.");
        TimbnLoadErrors.AddProblem("Herm has no home point.");
        TimbnLoadErrors.AddProblem("Linda has no home point.");

        Assert.Equal(["Herm has no home point.", "Linda has no home point.", _closing], Lines([]));
    }

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

    [Fact]
    public void ExactlySixLinesAreAllShown()
    {
        for (var i = 1; i <= 6; i++)
            TimbnLoadErrors.AddProblem($"Problem {i}.");

        var lines = Lines([]);

        Assert.Equal(7, lines.Length);
        Assert.DoesNotContain(lines, line => line.StartsWith("And "));
    }
}
