namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestBuilderTests
{
    [Fact]
    public void AGameTextAnswerKeepsItsLinesUnderTheQuestKey()
    {
        var quest = Build(q => q
            .Offer(talk => talk
                .Accept(TimbnGameIds.Texts.DefaultSure, then => then.Say("Cheers!"))
                .Decline(TimbnGameIds.Texts.DefaultNope))
            .ReadyByCode());

        Assert.Equal("Cheers!", quest.Texts["q_accept_1"]);
        Assert.False(quest.Texts.ContainsKey("q_accept"));
        Assert.False(quest.Texts.ContainsKey("q_decline"));
    }

    [Fact]
    public void QuestMakesItsKeys()
    {
        var quest = Build(q => q
            .Description("Bring wine.")
            .Offer(talk => talk
                .Say("Wine?")
                .Accept("Fine.", then => then.Say("Cheers!"))
                .Decline("No."))
            .Talk(talk => talk
                .PlayerSay("Got your wine.")
                .Say("Let's see it.")
                .Accept("Here.", then => then.Say("Lovely."))
                .Decline("Soon.")
                .Resume("Back again?")));

        AssertTexts(
            new Dictionary<string, string>
            {
                ["q"] = "Quest",
                ["quest_open_q_d"] = "Bring wine.",
                ["quest_closed_q_d"] = "Bring wine.",
                ["q_offer_1"] = "Wine?",
                ["q_accept"] = "Fine.",
                ["q_accept_1"] = "Cheers!",
                ["q_decline"] = "No.",
                ["q_talk_1"] = "Got your wine.",
                ["q_talk_2"] = "Let's see it.",
                ["q_talk_accept"] = "Here.",
                ["q_talk_accept_1"] = "Lovely.",
                ["q_talk_decline"] = "Soon.",
                ["q_talk_resume"] = "Back again?",
            },
            quest);
    }

    [Fact]
    public void BranchLinesKeepNumberingInOrder()
    {
        var quest = Build(q => q
            .Offer(talk => talk
                .Say("a")
                .When(() => true, then => then.Say("b"), otherwise => otherwise.Say("c"))
                .Say("d")
                .Accept("Yes."))
            .ReadyByCode());

        Assert.Equal("a", quest.Texts["q_offer_1"]);
        Assert.Equal("b", quest.Texts["q_offer_2"]);
        Assert.Equal("c", quest.Texts["q_offer_3"]);
        Assert.Equal("d", quest.Texts["q_offer_4"]);
    }

    [Fact]
    public void ContinuingAQuestMakesItTheFirstParentAndStartsWithIt()
    {
        var first = Quest("first");
        var quest = Build(q => q.OfferAfter("story").ContinuesFrom(first).ReadyByCode());

        Assert.Equal(["first", "story"], quest.Parents);
        Assert.True(quest.StartsWithContinued);
        Assert.False(Build(q => q.ContinuesFrom(first).StartsOn(TimbnQuestEvent.Slept()).ReadyByCode()).StartsWithContinued);
    }

    private static TimbnQuest Quest(string id)
    {
        var builder = new TimbnQuestBuilder(id, "npc_larry", "Quest");
        builder.ReadyByCode();
        builder.Finish();
        return new TimbnQuest([new TimbnQuestRuntime(builder, () => default)], []);
    }

    [Fact]
    public void StagesBecomeAChainOfGameQuests()
    {
        var quest = Build(q => q
            .Icon("larry")
            .OfferAfter("story")
            .Offer(talk => talk.Say("Find me gold wine?").Accept("Sure."))
            .AddStage("ask", "Ask Herm.", stage => stage.TalksWith("npc_herm").Talk(talk => talk.Accept("Gold wine?")))
            .AddStage("pay", "Pay Herm.", stage => stage
                .TalksWith("npc_herm")
                .Talk(talk => talk
                    .Say("That'll be 2 silver.")
                    .Accept("Here."))
                .Cost(cost => cost.Money(200)))
            .AddStage("bring", "Bring Larry the wine.", stage => stage.Name("Larry's Wine").Talk(talk => talk.Accept("Your wine.")))
            .DoneDescription("Larry got his wine."));

        Assert.Equal(["q", "q_pay", "q_bring"], quest.Steps.Select(step => step.Id));
        Assert.Equal(["ask", "pay", "bring"], quest.StageNames);
        Assert.Null(quest.Steps[0].Continued);
        Assert.Equal("q", quest.Steps[1].Continued);
        Assert.Equal("q_pay", quest.Steps[2].Continued);
        Assert.True(quest.Steps[1].StartsWithContinued);
        Assert.NotNull(quest.Steps[0].OfferTalk);
        Assert.Equal(["story"], quest.Steps[0].Parents);
        Assert.Equal("larry", quest.Steps[0].IconId);
        Assert.Equal("npc_herm", quest.Steps[1].TalkNpc);
        Assert.Equal("npc_larry", quest.Steps[2].TalkNpc);
        Assert.Equal("Ask Herm.", quest.Steps[0].Texts["quest_open_q_d"]);
        Assert.Equal("That'll be 2 silver.", quest.Steps[1].Texts["q_pay_talk_1"]);
        Assert.Equal("Quest", quest.Steps[1].Texts["q_pay"]);
        Assert.Equal("Larry's Wine", quest.Steps[2].Texts["q_bring"]);
        Assert.Equal("Larry got his wine.", quest.Steps[2].Texts["quest_closed_q_bring_d"]);
        Assert.Equal("Ask Herm.", quest.Steps[0].Texts["quest_closed_q_d"]);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("talk_decline")]
    public void AStageNameThatMakesAnotherStagesKeyThrows(string name)
    {
        var error = Assert.Throws<ArgumentException>(() => Build(q => q
            .Offer(talk => talk.Say("Wine?").Accept("Sure.").Decline("No."))
            .AddStage("ask", "Ask Herm.", stage => stage.TalksWith("npc_herm").Talk(talk => talk.Accept("Gold wine?").Decline("Soon.")))
            .AddStage(name, "Pay Herm.", stage => stage.ReadyByCode())));

        Assert.Contains($"'q_{name}'", error.Message);
    }

    [Fact]
    public void AStagedQuestIsInProgressBetweenItsStages()
    {
        Assert.Equal((QuestStatus.Available, -1), TimbnQuest.Read([QuestStatus.Available, QuestStatus.Available, QuestStatus.Available]));
        Assert.Equal((QuestStatus.InProgress, 0), TimbnQuest.Read([QuestStatus.InProgress, QuestStatus.Available, QuestStatus.Available]));
        Assert.Equal((QuestStatus.InProgress, 1), TimbnQuest.Read([QuestStatus.Completed, QuestStatus.Available, QuestStatus.Available]));
        Assert.Equal((QuestStatus.InProgress, 1), TimbnQuest.Read([QuestStatus.Completed, QuestStatus.InProgress, QuestStatus.Available]));
        Assert.Equal((QuestStatus.InProgress, 2), TimbnQuest.Read([QuestStatus.Completed, QuestStatus.Completed, QuestStatus.Available]));
        Assert.Equal((QuestStatus.Completed, -1), TimbnQuest.Read([QuestStatus.Completed, QuestStatus.Completed, QuestStatus.Completed]));
    }

    private static TimbnQuest RepeatableQuest(string id)
    {
        var builder = new TimbnQuestBuilder(id, "npc_larry", "Quest");
        builder.Repeatable().ReadyByCode();
        builder.Finish();
        return new TimbnQuest([new TimbnQuestRuntime(builder, () => default)], [], repeats: true);
    }

    private static TimbnQuestBuilder Build(Action<TimbnQuestBuilder> quest)
    {
        var builder = new TimbnQuestBuilder("q", "npc_larry", "Quest");
        quest(builder);
        builder.Finish();
        return builder;
    }

    private static void AssertTexts(Dictionary<string, string> expected, TimbnQuestBuilder quest) =>
        Assert.Equal(expected.OrderBy(pair => pair.Key, StringComparer.Ordinal), quest.Texts.OrderBy(pair => pair.Key, StringComparer.Ordinal));
}
