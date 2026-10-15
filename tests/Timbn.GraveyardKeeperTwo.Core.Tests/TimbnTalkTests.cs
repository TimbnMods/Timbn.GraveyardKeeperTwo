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
    }

    [Fact]
    public void BothBranchesOfWhenShareTheNumbering()
    {
        var texts = Record(talk => talk
            .When(() => true, then => then.Say("Yes."), otherwise => otherwise.Say("No."))
            .Say("After."));

        Assert.Equal(["t_1", "t_2", "t_3"], texts.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal("No.", texts["t_2"]);
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
    public void AKeyNeedsALineBeforeItAndAGoodName()
    {
        Assert.Throws<InvalidOperationException>(() => Record(talk => talk.Named("ask")));
        Assert.Throws<InvalidOperationException>(() => Record(talk => talk.Say("Hi.").WaitRealSeconds(1f).Named("ask")));
        Assert.Throws<ArgumentException>(() => Record(talk => talk.Say("Hi.").Named("1st")));
        Assert.Throws<InvalidOperationException>(() => Record(talk => talk.Say("Hi.").Named("ask").Say("Again.").Named("ask")));
    }

    [Fact]
    public void AskWithNoAnswersThrows() =>
        Assert.Throws<ArgumentException>(() => Record(talk => talk.Ask(_ => { })));

    [Fact]
    public void TheOpeningIsAPlayerLineOnlyWhenItComesFirst()
    {
        var first = new TimbnLines(new TimbnScript("t", []));
        first.PlayerSay("Hello?").Say("Yes?");
        var later = new TimbnLines(new TimbnScript("t", []));
        later.Say("Yes?").PlayerSay("Hello?");

        Assert.Equal("t_1", first.Opening?.Value);
        Assert.Null(later.Opening);
    }

    [Fact]
    public void ANamedOpeningKeepsItsNewKey()
    {
        var lines = new TimbnLines(new TimbnScript("t", []));
        lines.PlayerSay("Hello?").Named("hello");

        Assert.Equal("t_hello", lines.Opening?.Value);
    }

    [Fact]
    public void LinesOfAnotherNpcShareTheNumbering()
    {
        var texts = Record(talk => talk.SayAs("npc_herm", "Hmph.").Say("Ignore him."));

        Assert.Equal("Hmph.", texts["t_1"]);
        Assert.Equal("Ignore him.", texts["t_2"]);
    }

    [Fact]
    public void AStepThatIsNoLineCannotBeNamed() =>
        Assert.Throws<InvalidOperationException>(() => Record(talk => talk.RunCode(() => { }).Named("code")));

    [Fact]
    public void AnExpressionCannotBeEmpty()
    {
        Assert.Throws<ArgumentException>(() => Record(talk => talk.RunExpression(" ")));
        Assert.Throws<ArgumentException>(() => Record(talk => talk.When("", then => then.Say("Yes."))));
    }

    [Fact]
    public void NamedRightAfterAskHasNoLineToName() =>
        Assert.Throws<InvalidOperationException>(() => Record(talk => talk.Ask(ask => ask.Answer("Yes.")).Named("ask")));

    [Fact]
    public void AnAnswerKeyIsValidatedLikeANamedLine()
    {
        Assert.Throws<ArgumentException>(() => Record(talk => talk.Ask(ask => ask.Answer("Yes.", key: "Yes"))));
        Assert.Throws<InvalidOperationException>(() => Record(talk => talk.Ask(ask => ask.Answer("Yes.", key: "yes").Answer("Sure.", key: "yes"))));
    }

    [Fact]
    public void TwoScriptsSharingTextsCannotMakeTheSameKey()
    {
        Dictionary<string, string> texts = [];
        new TimbnLines(new TimbnScript("t", texts)).Say("One.");

        var ex = Assert.Throws<InvalidOperationException>(() => new TimbnLines(new TimbnScript("t", texts)).Say("Two."));
        Assert.Contains("t_1", ex.Message);
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
    }

    [Fact]
    public void ANestedScriptSharesTheCounter()
    {
        Dictionary<string, string> texts = [];
        var script = new TimbnScript("t", texts);
        new TimbnLines(script).Say("One.");
        new TimbnLines(script.Nested()).Say("Inner.");
        new TimbnLines(script).Say("Two.");

        Assert.Equal("Inner.", texts["t_2"]);
        Assert.Equal("Two.", texts["t_3"]);
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
    public void WhenBranchesReplayIntoTheConversation()
    {
        List<string> log = [];
        var script = new TimbnScript("t", []);
        new TimbnLines(script).When(() => false, then => then.RunCode(() => log.Add("yes")), otherwise => otherwise.RunCode(() => log.Add("no")));

        var conversation = new TimbnConversation(null!, "t");
        script.Replay(conversation);
        conversation.Run(() => log.Add("done"));

        Assert.Equal(["no", "done"], log);
    }

    private static Dictionary<string, string> Record(Action<TimbnLines> talk)
    {
        Dictionary<string, string> texts = [];
        talk(new TimbnLines(new TimbnScript("t", texts)));
        return texts;
    }
}
