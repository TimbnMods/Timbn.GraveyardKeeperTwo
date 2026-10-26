namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnModsListTests
{
    private const string _core = "core";

    private static TimbnModsMod Mod(string guid) => new(guid.ToUpperInvariant(), "1.0.0", guid, false, null!, null, []);

    private static TimbnModsList Build(string[] guids, Dictionary<string, string> groups) =>
        BuildGroups(guids, groups.ToDictionary(pair => pair.Key, pair => TimbnModGroup.Of(pair.Value, null)));

    private static TimbnModsList BuildGroups(string[] guids, Dictionary<string, TimbnModGroup> groups) =>
        TimbnModsList.Build([.. guids.Select(Mod)], guid => groups.GetValueOrDefault(guid), _core);

    [Fact]
    public void CoreComesFirstThenGroupsThenTheRest()
    {
        var list = Build(["alpha", "core", "beta", "gamma"], new() { ["beta"] = "G", ["gamma"] = "G" });

        Assert.Equal(["core", "beta", "gamma", "alpha"], list.Mods.Select(mod => mod.Guid));
        Assert.Equal(["CORE", "G", "ALPHA"], list.Entries.Select(entry => entry.Title));
    }

    [Fact]
    public void GroupsKeepTheOrderTheirFirstModsHadAndEachGroupStaysTogether()
    {
        var list = Build(["a1", "b1", "a2", "b2"], new() { ["a1"] = "A", ["a2"] = "A", ["b1"] = "B", ["b2"] = "B" });

        Assert.Equal(["a1", "a2", "b1", "b2"], list.Mods.Select(mod => mod.Guid));
        Assert.Equal(["A", "B"], list.Entries.Select(entry => entry.Title));
    }

    [Fact]
    public void AModThatOnlySharesTheNameStaysOutOfAGroupWithAnId()
    {
        var list = BuildGroups(["a1", "a2", "copy"], new Dictionary<string, TimbnModGroup>
        {
            ["a1"] = TimbnModGroup.Of("A", "id-1"),
            ["a2"] = TimbnModGroup.Of("A", "ID-1"),
            ["copy"] = TimbnModGroup.Of("A", null),
        });

        Assert.Equal(["A", "COPY"], list.Entries.Select(entry => entry.Title));
        Assert.Equal([0, 1], list.Entries[0].Members);
    }

    [Fact]
    public void AGroupWithAnIdShowsTheFirstModsName()
    {
        var list = BuildGroups(["a1", "a2"], new Dictionary<string, TimbnModGroup>
        {
            ["a1"] = TimbnModGroup.Of("First", "id-1"),
            ["a2"] = TimbnModGroup.Of("Second", "id-1"),
        });

        Assert.Equal(["First"], list.Entries.Select(entry => entry.Title));
    }
}
