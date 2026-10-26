namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnTalkTests
{
    [Fact]
    public void LinesAndAnswersAreNumberedInTheOrderTheyAreWritten()
    {
        var texts = Record(talk => talk
            .PlayerSay("Lost my boards.")
            .Say("Here's a thought.")
            .Ask(ask => ask
                .Answer("Got any?", then => then.Say("Take these.").PlayerSay("Thanks."))
                .Answer("Never mind.", then => then.Say("Suit yourself.")))
            .Say("Bye."));

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["t_1"] = "Lost my boards.",
                ["t_2"] = "Here's a thought.",
                ["t_3"] = "Got any?",
                ["t_4"] = "Take these.",
                ["t_5"] = "Thanks.",
                ["t_6"] = "Never mind.",
                ["t_7"] = "Suit yourself.",
                ["t_8"] = "Bye.",
            },
            texts);

        var branched = Record(talk => talk
            .When(() => true, then => then.Say("Yes."), otherwise => otherwise.Say("No."))
            .Say("After."));

        Assert.Equal(["t_1", "t_2", "t_3"], branched.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal("No.", branched["t_2"]);
    }

    [Fact]
    public void AKeyNamesTheLineBeforeItWithoutMovingTheNumbers()
    {
        var texts = Record(talk => talk
            .Say("Hello.")
            .Say("Got any boards?").Named("ask")
            .Ask(ask => ask
                .Answer("Yes.", key: "yes")
                .Answer("No."))
            .Say("Bye."));

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["t_1"] = "Hello.",
                ["t_ask"] = "Got any boards?",
                ["t_yes"] = "Yes.",
                ["t_2"] = "No.",
                ["t_3"] = "Bye.",
            },
            texts);
    }

    [Fact]
    public void AChildScriptCountsOnItsOwnPrefix()
    {
        Dictionary<string, string> texts = [];
        var script = new TimbnScript("t", texts);
        new TimbnLines(script).Say("One.");
        new TimbnLines(script.Child("t_accept")).Say("Inner.");
        new TimbnLines(script).Say("Two.");

        Assert.Equal(["t_1", "t_2", "t_accept_1"], texts.Keys.OrderBy(key => key, StringComparer.Ordinal));

        Dictionary<string, string> shared = [];
        var parent = new TimbnScript("t", shared);
        new TimbnLines(parent).Say("One.");
        new TimbnLines(parent.Nested()).Say("Inner.");
        new TimbnLines(parent).Say("Two.");

        Assert.Equal("Inner.", shared["t_2"]);
        Assert.Equal("Two.", shared["t_3"]);
    }

    [Fact]
    public void ReplayPlaysTheRecordedStepsInOrderAndCanSkipTheFirst()
    {
        List<string> log = [];
        var script = new TimbnScript("t", []);
        new TimbnLines(script).RunCode(() => log.Add("a")).RunCode(() => log.Add("b")).RunCode(() => log.Add("c"));

        var whole = new TimbnConversation(null!, "t");
        script.Replay(whole);
        whole.Run(() => log.Add("done"));
        var rest = new TimbnConversation(null!, "t");
        script.Replay(rest, skip: 2);
        rest.Run(() => log.Add("done"));

        Assert.Equal(["a", "b", "c", "done", "c", "done"], log);
    }

    [Fact]
    public void AGameTextLineAddsNoTextAndKeepsTheNumbers()
    {
        var texts = Record(talk => talk
            .Say("Hello.")
            .Ask(ask => ask.Answer(TimbnGameIds.Texts.DefaultNope))
            .Say("Bye."));

        Assert.Equal(new Dictionary<string, string> { ["t_1"] = "Hello.", ["t_3"] = "Bye." }, texts);
    }

    private static Dictionary<string, string> Record(Action<TimbnLines> talk)
    {
        Dictionary<string, string> texts = [];
        talk(new TimbnLines(new TimbnScript("t", texts)));
        return texts;
    }
}
