namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnDialogTests
{
    private static IDisposable Talk(string npcId, string eventId) =>
        TimbnDialog.AddTalk(npcId, eventId, () => true, _ => { });

    [Fact]
    public void ATalkIdIsUniqueAcrossNpcs()
    {
        using var herm = Talk("npc_herm", "dialog_tests_shared");

        var ex = Assert.Throws<InvalidOperationException>(() => Talk("npc_linda", "dialog_tests_shared"));
        Assert.Contains("npc_herm", ex.Message);
    }

    [Fact]
    public void TheSameTalkOnTheSameNpcReplacesTheOld()
    {
        using var first = Talk("npc_herm", "dialog_tests_replaced");
        using var second = Talk("npc_herm", "dialog_tests_replaced");
    }

    [Fact]
    public void AMovingTalkCannotReuseAnyTalkId()
    {
        using var fixedTalk = Talk("npc_herm", "dialog_tests_moving");

        Assert.Throws<InvalidOperationException>(() => TimbnDialog.AddTalk(() => "npc_herm", "dialog_tests_moving", () => true, _ => { }));
    }

    [Fact]
    public void DisposingFreesTheTalkIdForAnotherNpc()
    {
        var herm = Talk("npc_herm", "dialog_tests_freed");
        herm.Dispose();

        using var linda = Talk("npc_linda", "dialog_tests_freed");
    }

    [Fact]
    public void DisposingTheReplacedTalkKeepsTheNewOne()
    {
        var first = Talk("npc_herm", "dialog_tests_kept");
        using var second = Talk("npc_herm", "dialog_tests_kept");
        first.Dispose();

        Assert.Throws<InvalidOperationException>(() => Talk("npc_linda", "dialog_tests_kept"));
    }

    [Fact]
    public void AMenuAnswerComesAndGoes()
    {
        var handle = TimbnDialog.AddMenuAnswer("npc_herm", () => null, _ => { });

        handle.Dispose();
        handle.Dispose();
    }

    [Fact]
    public void SeenTalksLiveInTheSetHandedIn()
    {
        HashSet<string> seen = [];
        TimbnDialog.UseSeenTalks(() => seen);

        Assert.False(TimbnDialog.HasSeen("dialog_tests_once"));
        TimbnDialog.MarkSeen("dialog_tests_once");
        Assert.True(TimbnDialog.HasSeen("dialog_tests_once"));
        Assert.Contains("dialog_tests_once", seen);

        TimbnDialog.ResetSeen("dialog_tests_once");
        Assert.False(TimbnDialog.HasSeen("dialog_tests_once"));
        TimbnDialog.ResetSeen("dialog_tests_once");
    }

    [Fact]
    public void AnIconOtherThanTheSpeechBubbleIsLeftAlone()
    {
        using var talk = Talk("npc_herm", "dialog_tests_icon");

        Assert.Equal("icon_quest", TimbnDialog.IconFor("dialog_tests_icon", "icon_quest"));
    }

    [Fact]
    public void TheSpeechBubbleOfAnUnknownEventIsLeftAlone() =>
        Assert.Equal("icon_speech_bubble", TimbnDialog.IconFor("dialog_tests_not_a_talk", "icon_speech_bubble"));

    [Fact]
    public void AnAnswerWithAKnownPriceCarriesIt()
    {
        var price = new AnswerData();

        var answer = TimbnDialog.Answer("dialog_tests_answer", new Dictionary<string, AnswerData> { ["dialog_tests_answer"] = price });

        Assert.Equal("dialog_tests_answer", answer.id);
        Assert.Same(price, answer.answerData);
    }
}
