namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnModOwnedTests
{
    private static readonly HashSet<string> _game = ["flour", "millstone", "empty"];

    private static bool Exists(string id) => _game.Contains(id);

    [Fact]
    public void TakesWhatAModAddedAndWhatHasNoDefinition()
    {
        var owned = new TimbnModOwned(["timbn_pie"], Exists);

        Assert.True(owned.Takes("timbn_pie"));
        Assert.True(owned.Takes("gone_mod_item"));
        Assert.False(owned.Takes("flour"));
    }

    [Fact]
    public void AModCopyOfAGameIdIsStillTheMods()
    {
        var owned = new TimbnModOwned(["millstone"], Exists);

        Assert.True(owned.Takes("millstone"));
    }

    [Fact]
    public void IgnoresEmptyAndPlaceholderIds()
    {
        var owned = new TimbnModOwned(["empty"], Exists, "empty");

        Assert.False(owned.Takes(null));
        Assert.False(owned.Takes(""));
        Assert.False(owned.Takes("empty"));
    }

    [Fact]
    public void MissingOnlyTakesWhatHasNoDefinition()
    {
        var owned = TimbnModOwned.Missing(Exists, "empty");

        Assert.True(owned.Takes("gone_mod_item"));
        Assert.False(owned.Takes("flour"));
        Assert.False(owned.Takes("empty"));
    }
}
