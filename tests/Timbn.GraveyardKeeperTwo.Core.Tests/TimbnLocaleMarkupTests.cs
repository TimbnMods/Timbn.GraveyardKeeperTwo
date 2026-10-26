using LazyBearTechnology;
using System.Runtime.CompilerServices;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLocaleMarkupTests
{
    private static readonly LLBase _lang = (LLBase)RuntimeHelpers.GetUninitializedObject(typeof(LL));

    [Fact]
    public void SpriteTagsBecomeTextMeshProSprites()
    {
        var text = TimbnLocaleMarkup.Parse(_lang, "key", "Pay (*slv*) “Hi„ ・ now", out _, out _);

        Assert.Equal("Pay <sprite name=\"slv\"> \"Hi\" ⊙ now", text);
    }

    [Fact]
    public void NestedKeysAreCutOutAndRememberedByPosition()
    {
        var text = TimbnLocaleMarkup.Parse(_lang, "key", "Bring #(flitch) and #(stone).", out var nested, out _);

        Assert.Equal("Bring  and .", text);
        Assert.True(nested.hasMetaInfo);
        Assert.Equal(["flitch", "stone"], nested.localeIDsToInsert);
        Assert.Equal([6, 11], nested.idsToInsert);
    }

    [Fact]
    public void ReplacementKeysAreCutOutAndCarryTheKey()
    {
        var text = TimbnLocaleMarkup.Parse(_lang, "key", "Hello @(player_name)!", out _, out var replacement);

        Assert.Equal("Hello !", text);
        Assert.NotNull(replacement);
        Assert.Equal("key", replacement!.id);
        Assert.Equal(["player_name"], replacement.keys);
        Assert.Equal([6], replacement.keysIndexes);
    }
}
