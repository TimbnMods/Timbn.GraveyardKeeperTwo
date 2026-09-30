namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLocaleMarkupTests
{
    [Fact]
    public void PlainTextHasNoMetadata()
    {
        var text = TimbnLocaleMarkup.Parse("key", "Hello there", out var nested, out var replacement);

        Assert.Equal("Hello there", text);
        Assert.False(nested.hasMetaInfo);
        Assert.Null(replacement);
    }

    [Fact]
    public void SpriteTagsBecomeTextMeshProSprites()
    {
        var text = TimbnLocaleMarkup.Parse("key", "Pay (*money*) now", out _, out _);

        Assert.Equal("Pay <sprite name=\"money\"> now", text);
    }

    [Fact]
    public void NestedKeysAreCutOutAndRememberedByPosition()
    {
        var text = TimbnLocaleMarkup.Parse("key", "Bring #(flitch) and #(stone).", out var nested, out _);

        Assert.Equal("Bring  and .", text);
        Assert.True(nested.hasMetaInfo);
        Assert.Equal(["flitch", "stone"], nested.localeIDsToInsert);
        Assert.Equal([6, 11], nested.idsToInsert);
    }

    [Fact]
    public void ReplacementKeysAreCutOutAndCarryTheKey()
    {
        var text = TimbnLocaleMarkup.Parse("key", "Hello @(player_name)!", out _, out var replacement);

        Assert.Equal("Hello !", text);
        Assert.NotNull(replacement);
        Assert.Equal("key", replacement!.id);
        Assert.Equal(["player_name"], replacement.keys);
        Assert.Equal([6], replacement.keysIndexes);
    }
}
