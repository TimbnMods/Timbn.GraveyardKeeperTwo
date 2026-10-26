using BepInEx.Logging;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnModEventBusTests
{
    private readonly ManualLogSource _logger = new("Tests");

    // The bus is static, so every test speaks on an id of its own.
    private readonly string _id = $"test_{Guid.NewGuid():N}";

    private sealed class Moved
    {
        public string? OldId { get; set; }

        public string? NewId { get; set; }
    }

    [Fact]
    public void ATypedListenerSitsOutAnEventWithNoPayload()
    {
        var calls = 0;
        using var listening = TimbnModEventBus.Listen<Moved>(_id, _ => calls++, _logger);

        TimbnModEventBus.Publish(_id, null, _logger);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void ANoDataListenerHearsEveryPublish()
    {
        var calls = 0;
        using var listening = TimbnModEventBus.Listen(_id, () => calls++, _logger);

        TimbnModEventBus.Publish(_id, null, _logger);
        TimbnModEventBus.Publish(_id, new { oldId = "a" }, _logger);

        Assert.Equal(2, calls);
    }

    [Fact]
    public void APayloadIsReadIntoTheListenersOwnClassIgnoringCase()
    {
        Moved? heard = null;
        using var listening = TimbnModEventBus.Listen<Moved>(_id, moved => heard = moved, _logger);

        TimbnModEventBus.Publish(_id, new { oldId = "a", newId = "b" }, _logger);

        Assert.Equal("a", heard?.OldId);
        Assert.Equal("b", heard?.NewId);
    }

    [Fact]
    public void APayloadThatDoesNotFitTheClassIsSkipped()
    {
        var calls = 0;
        using var listening = TimbnModEventBus.Listen<Moved>(_id, _ => calls++, _logger);

        TimbnModEventBus.Publish(_id, "just text", _logger);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void APayloadOfTheListenersOwnTypeIsHandedOverAsIs()
    {
        var sent = new Moved { OldId = "a" };
        Moved? heard = null;
        using var listening = TimbnModEventBus.Listen<Moved>(_id, moved => heard = moved, _logger);

        TimbnModEventBus.Publish(_id, sent, _logger);

        Assert.Same(sent, heard);
    }

    [Fact]
    public void TheTextListenerGetsTheJsonAndNullForNoPayload()
    {
        var texts = new List<string>();
        using var listening = TimbnModEventBus.Listen(_id, texts.Add, _logger);

        TimbnModEventBus.Publish(_id, new { x = 1 }, _logger);
        TimbnModEventBus.Publish(_id, null, _logger);

        Assert.Equal(["{\"x\":1}", "null"], texts);
    }

    [Fact]
    public void DisposingTheHandleStopsTheListener()
    {
        var calls = 0;
        var listening = TimbnModEventBus.Listen(_id, () => calls++, _logger);
        TimbnModEventBus.Publish(_id, null, _logger);

        listening.Dispose();
        TimbnModEventBus.Publish(_id, null, _logger);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void AListenerThatThrowsDoesNotStopTheOthers()
    {
        var calls = 0;
        using var throwing = TimbnModEventBus.Listen(_id, () => throw new InvalidOperationException("boom"), _logger);
        using var listening = TimbnModEventBus.Listen(_id, () => calls++, _logger);

        TimbnModEventBus.Publish(_id, null, _logger);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void APublishNestedTooDeepIsDropped()
    {
        var calls = 0;
        using var listening = TimbnModEventBus.Listen(_id, () =>
        {
            calls++;
            TimbnModEventBus.Publish(_id, null, _logger);
        }, _logger);

        TimbnModEventBus.Publish(_id, null, _logger);

        Assert.Equal(8, calls);
    }

    [Fact]
    public void KnownListsAnIdWhileSomethingPublishesOrListensToIt()
    {
        var publishing = TimbnModEventBus.AddPublisher(_id, "mod.a");

        var known = TimbnModEventBus.Known.Single(known => known.Id == _id);
        Assert.Equal(["mod.a"], known.PublishingMods);

        publishing.Dispose();
        Assert.DoesNotContain(TimbnModEventBus.Known, known => known.Id == _id);
    }
}
