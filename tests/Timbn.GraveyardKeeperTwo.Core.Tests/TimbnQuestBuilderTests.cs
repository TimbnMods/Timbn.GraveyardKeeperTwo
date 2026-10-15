namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestBuilderTests
{
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
    public void ThenLinesGetDoneKeys()
    {
        var quest = Build(q => q
            .ReadyOn(TimbnQuestEvent.EnteredWorldZone("vineyard_basement"))
            .DoneLines(then => then.PlayerSay("Nice place.")));

        Assert.Equal("Nice place.", quest.Texts["q_done_1"]);
    }

    [Fact]
    public void ThenOnATalkQuestThrows() =>
        Assert.Throws<ArgumentException>(() => Build(q => q
            .Talk(talk => talk.Accept("Here."))
            .DoneLines(then => then.Say("Too late."))));

    [Fact]
    public void ATalkThatOpensWithThePlayerKnowsItsOpeningLine()
    {
        var quest = Build(q => q.Talk(talk => talk.PlayerSay("About the wine.").Say("Yes?").Accept("Here.")));
        var plain = Build(q => q.Talk(talk => talk.Say("Yes?").Accept("Here.")));
        var onlyAccept = Build(q => q.Talk(talk => talk.Accept("Here.")));

        Assert.Equal("q_talk_1", quest.TalkOf!.Opening!.Value);
        Assert.Null(plain.TalkOf!.Opening);
        Assert.True(onlyAccept.TalkOf!.IsOnlyAccept);
        Assert.False(quest.TalkOf.IsOnlyAccept);
    }

    [Fact]
    public void OfferNeedsAnAccept() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.Offer(talk => talk.Say("Hi.")).ReadyByCode()));

    [Fact]
    public void QuestThatCanNeverFinishThrows() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.Description("Stuck.")));

    [Fact]
    public void NeedsNeedATalk() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.Needs(need => need.Item("flitch")).ReadyByCode()));

    [Fact]
    public void ATalkNeedsAnAccept() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.Talk(talk => talk.Say("Here you go."))));

    [Fact]
    public void AnAcceptWithNoTextHandsOverTheStepsReward()
    {
        var quest = Build(q => q
            .Reward(reward => reward.Item("grape_wine:3"))
            .Talk(talk => talk.PlayerSay("I'm here for my wine.").Say("Here you go.").Accept()));

        Assert.True(quest.TalkOf!.HasAccept);
        Assert.Null(quest.TalkOf.AcceptKey);
        Assert.False(quest.TalkOf.IsOnlyAccept);
        Assert.False(quest.RewardsOf.IsEmpty);
    }

    [Fact]
    public void AnAcceptWithNoTextNeedsLinesAndNoAnswersOrCost()
    {
        Assert.Throws<ArgumentException>(() => Build(q => q.Talk(talk => talk.Accept())));
        Assert.Throws<ArgumentException>(() => Build(q => q.Talk(talk => talk.Say("Here.").Accept().Decline("No."))));
        Assert.Throws<ArgumentException>(() => Build(q => q.Talk(talk => talk.Say("Here.").Accept().Resume("Back?"))));
        Assert.Throws<ArgumentException>(() => Build(q => q.Cost(cost => cost.Money(5)).Talk(talk => talk.Say("Here.").Accept())));
    }

    [Fact]
    public void TwoAcceptsThrow() =>
        Assert.Throws<InvalidOperationException>(() => Build(q => q.Talk(talk => talk.Say("Here.").Accept().Accept("Again."))));

    [Fact]
    public void ACostNeedsATalk() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.Cost(cost => cost.Money(5)).ReadyByCode()));

    [Fact]
    public void WaitingForCodeAndAnEventThrows() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.ReadyByCode().ReadyOn(TimbnQuestEvent.Slept())));

    [Fact]
    public void DoneReplacesTheDescriptionOnceTheQuestIsDone()
    {
        var quest = Build(q => q.Description("Smoke the bats.").DoneDescription("The bats are gone.").ReadyByCode());

        Assert.Equal("Smoke the bats.", quest.Texts["quest_open_q_d"]);
        Assert.Equal("The bats are gone.", quest.Texts["quest_closed_q_d"]);
    }

    [Fact]
    public void TheTalkIsWithTheGiverUnlessAnotherNpcIsNamed()
    {
        Assert.Equal("npc_larry", Build(q => q.Talk(talk => talk.Accept("Here."))).TalkNpc);
        Assert.Equal("npc_herm", Build(q => q.TalksWith("npc_herm").Talk(talk => talk.Accept("Here."))).TalkNpc);
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

    [Fact]
    public void ContinuingTwoQuestsThrows() =>
        Assert.Throws<InvalidOperationException>(() => Build(q => q.ContinuesFrom(Quest("one")).ContinuesFrom(Quest("two")).ReadyByCode()));

    [Fact]
    public void PhaseIsOneOfTheGamesFiveRings()
    {
        Assert.Equal(3, Build(q => q.Phase(3).ReadyByCode()).RingPhase);
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(q => q.Phase(6).ReadyByCode()));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(q => q.Phase(0).ReadyByCode()));
    }

    [Fact]
    public void LinesAfterAnswersThrow() =>
        Assert.Throws<InvalidOperationException>(() => Build(q => q
            .Offer(talk => talk.Accept("Yes.").Say("Too late."))
            .ReadyByCode()));

    [Fact]
    public void NeedsAndCostsBecomeTheGamesRequirements()
    {
        var wants = new TimbnWants();
        new TimbnCost(wants).Item("grape_wine:1", 3);
        new TimbnNeeds(wants).Item("flitch", 2).Money(100).OnWeekday(TimbnWeekday.Pride).OrderCompleted("quest_order_iron");

        Assert.Collection(
            wants.ToRequirements(null),
            r =>
            {
                Assert.Equal(QuestPhraseRequirement.Requirement.Price, r.requirement);
                Assert.Equal(QuestPhraseRequirement.Entity.Item, r.entity);
                Assert.Equal("grape_wine:1", r.itemCount.itemId);
                Assert.Equal(3, r.itemCount.count);
            },
            r =>
            {
                Assert.Equal(QuestPhraseRequirement.Requirement.Lock, r.requirement);
                Assert.Equal("flitch", r.itemCount.itemId);
            },
            r =>
            {
                Assert.Equal(QuestPhraseRequirement.Entity.GameResAtom, r.entity);
                Assert.Equal("money", r.gameResAtom.type);
                Assert.Equal(100f, r.gameResAtom.value);
            },
            r =>
            {
                Assert.Equal(QuestPhraseRequirement.Entity.Day, r.entity);
                Assert.Equal(TimbnClock.WeekdayId(TimbnWeekday.Pride), r.dayNumber);
            },
            r =>
            {
                Assert.Equal(QuestPhraseRequirement.Entity.Order, r.entity);
                Assert.Equal("quest_order_iron", r.order);
            });
    }

    [Fact]
    public void CostsAndRewardsFillTheAnswer()
    {
        var answer = new TimbnAnswer("q_talk_accept");
        var wants = new TimbnWants();
        new TimbnCost(wants).Item("grape_wine:1", 3);
        wants.ApplyTo(answer, null);
        var rewards = new TimbnRewards();
        rewards.Money(300).Item("grape_wine:2").Reputation(10).ApplyTo(answer, "npc_larry_REP");

        var data = Assert.IsType<AnswerData>(answer.ToData());
        Assert.Equal("grape_wine:1", Assert.Single(data.costRes.items).itemId);
        Assert.Equal("grape_wine:2", Assert.Single(data.rewardRes.items).itemId);
        Assert.Equal(300f, Assert.Single(data.rewardRes.gameRes.List, a => a.type == "money").value);
        Assert.Equal(10f, Assert.Single(data.fakeRewardRes.gameRes.List, a => a.type == "npc_larry_REP").value);
    }

    [Fact]
    public void ShownRewardsAreDrawnButNotGiven()
    {
        var answer = new TimbnAnswer("q_trade");
        new TimbnRewards().ShowItem("flitch", 2).ShowResource("money", 5).Reputation(3).ApplyTo(answer, "npc_larry_REP");

        var data = Assert.IsType<AnswerData>(answer.ToData());
        Assert.Null(data.rewardRes);
        Assert.Equal(2, Assert.Single(data.fakeRewardRes.items, i => i.itemId == "flitch").count);
        Assert.Equal(5f, Assert.Single(data.fakeRewardRes.gameRes.List, a => a.type == "money").value);
        Assert.Equal(3f, Assert.Single(data.fakeRewardRes.gameRes.List, a => a.type == "npc_larry_REP").value);
    }

    [Fact]
    public void AReputationNeedIsALockOnTheNpcsReputation()
    {
        var wants = new TimbnWants();
        new TimbnNeeds(wants).Reputation(30);
        var answer = new TimbnAnswer("q_talk_accept");
        wants.ApplyTo(answer, "village_REP");

        var requirement = Assert.Single(wants.ToRequirements("village_REP"));
        Assert.Equal(QuestPhraseRequirement.Requirement.Lock, requirement.requirement);
        Assert.Equal("village_REP", requirement.gameResAtom.type);
        Assert.Equal(30f, requirement.gameResAtom.value);
        Assert.Equal(30f, Assert.Single(answer.ToData()!.lockRes.gameRes.List, a => a.type == "village_REP").value);
        Assert.Empty(wants.ToRequirements(null));
    }

    [Fact]
    public void ReputationIsLeftOffWithoutAnNpcReputation()
    {
        var answer = new TimbnAnswer("q_talk_accept");
        new TimbnRewards().Reputation(10).ApplyTo(answer, null);

        Assert.Null(answer.ToData());
    }

    [Fact]
    public void QuestEventsMatchTheGamesTrigger()
    {
        Assert.True(TimbnQuestEvent.Slept().Matches(GlobalEventsSystem.Event.Type.AfterSleep, ""));
        Assert.True(TimbnQuestEvent.Slept().Matches(GlobalEventsSystem.Event.Type.AfterSleep, null!));
        Assert.True(TimbnQuestEvent.Crafted("candle").Matches(GlobalEventsSystem.Event.Type.CraftFinish, "candle"));
        Assert.False(TimbnQuestEvent.Crafted("candle").Matches(GlobalEventsSystem.Event.Type.CraftFinish, "torch"));
        Assert.False(TimbnQuestEvent.Crafted("candle").Matches(GlobalEventsSystem.Event.Type.CraftStart, "candle"));
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

    [Fact]
    public void ExpressionsAndHooksBelongToTheStageThatHasThem()
    {
        var quest = Build(q => q
            .AddStage("one", "One.", stage => stage
                .OnStartExpression("AddPerk(\"perk_x\")")
                .OnDoneExpression("RevealTech(\"tech_x\")")
                .OnDoneExpression("$money += 50")
                .OnStart(() => { })
                .OnEnd(() => { })
                .ReadyByCode()));

        var step = Assert.Single(quest.Steps);
        Assert.Equal(["AddPerk(\"perk_x\")"], step.StartExpressions.Select(expression => expression.Text));
        Assert.Equal(["RevealTech(\"tech_x\")", "$money += 50"], step.DoneExpressions.Select(expression => expression.Text));
        Assert.NotNull(step.WhenStarted);
        Assert.NotNull(step.WhenEnded);
    }

    [Fact]
    public void TheQuestsOwnStepPartsMakeItTheFirstStep()
    {
        var quest = Build(q => q
            .Offer(talk => talk.Say("Wine?").Accept("Sure."))
            .Description("Ask Herm.")
            .TalksWith("npc_herm")
            .Needs(need => need.Reputation(30))
            .Cost(cost => cost.Money(200))
            .Talk(talk => talk.PlayerSay("Larry wants wine.").Accept("Here."))
            .AddStage("bring", "Bring Larry the wine.", stage => stage.Talk(talk => talk.Accept("Your wine.")))
            .DoneDescription("Larry got his wine."));

        Assert.Equal(["q", "q_bring"], quest.Steps.Select(step => step.Id));
        Assert.Equal(["q", "bring"], quest.StageNames);
        Assert.Same(quest, quest.Steps[0]);
        Assert.Equal("q", quest.Steps[1].Continued);
        Assert.Equal("Ask Herm.", quest.Texts["quest_closed_q_d"]);
        Assert.Equal("Larry got his wine.", quest.Steps[1].Texts["quest_closed_q_bring_d"]);
    }

    [Fact]
    public void ExpressionsOnTheQuestMakeItTheFirstStep()
    {
        var quest = Build(q => q
            .OnStartExpression("AddPerk(\"perk_x\")")
            .ReadyByCode()
            .AddStage("one", "One.", stage => stage.ReadyByCode()));

        Assert.Equal(["q", "q_one"], quest.Steps.Select(step => step.Id));
        Assert.Single(quest.Steps[0].StartExpressions);
    }

    [Fact]
    public void AnEmptyExpressionThrows() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.OnDoneExpression(" ").ReadyByCode()));

    [Fact]
    public void AQuestWithoutStagesIsItsOnlyStep()
    {
        var quest = Build(q => q.ReadyByCode());

        Assert.Same(quest, Assert.Single(quest.Steps));
        Assert.Empty(quest.StageNames);
    }

    [Fact]
    public void StageNamesAreKeySafeAndUnique()
    {
        Assert.Throws<ArgumentException>(() => Build(q => q.AddStage("Smoke Bats", "Smoke.", stage => stage.ReadyByCode())));
        Assert.Throws<ArgumentException>(() => Build(q => q
            .AddStage("one", "One.", stage => stage.ReadyByCode())
            .AddStage("one", "Again.", stage => stage.ReadyByCode())));
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("decline")]
    [InlineData("talk_accept")]
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
    public void StagesWhoseKeysOverlapThrow() =>
        Assert.Throws<ArgumentException>(() => Build(q => q
            .AddStage("one", "One.", stage => stage.ReadyByCode())
            .AddStage("two", "Two.", stage => stage.Talk(talk => talk.Accept("Here.")))
            .AddStage("two_talk_accept", "Again.", stage => stage.ReadyByCode())));

    [Fact]
    public void AStageNamedLikeAnAnswerIsFineWhenNothingElseMakesThatKey()
    {
        var quest = Build(q => q
            .AddStage("ask", "Ask Herm.", stage => stage.ReadyByCode())
            .AddStage("accept", "Give it.", stage => stage.ReadyByCode()));

        Assert.Equal(["q", "q_accept"], quest.Steps.Select(step => step.Id));
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

    [Fact]
    public void AQuestWithoutStagesOrASaveReadsItsOwnStatus()
    {
        Assert.Equal((QuestStatus.Completed, -1), TimbnQuest.Read([QuestStatus.Completed]));
        Assert.Equal((QuestStatus.InProgress, 0), TimbnQuest.Read([QuestStatus.InProgress]));
        Assert.Equal(((QuestStatus?)null, -1), TimbnQuest.Read([null, null]));
    }

    [Fact]
    public void ARepeatableQuestCannotBeFollowedOrContinued()
    {
        var repeatable = RepeatableQuest("daily");

        Assert.Throws<ArgumentException>(() => Build(q => q.OfferAfter(repeatable).ReadyByCode()));
        Assert.Throws<ArgumentException>(() => Build(q => q.ContinuesFrom(repeatable).ReadyByCode()));
    }

    [Fact]
    public void OfferAfterAQuestUsesItsLastStep()
    {
        var quest = Build(q => q.OfferAfter(Quest("one")).ReadyByCode());

        Assert.Equal(["one"], quest.Parents);
        Assert.Null(quest.Continued);
    }

    [Fact]
    public void AContinuedQuestComesFirstAmongTheParents()
    {
        var quest = Build(q => q.OfferAfter("story").ContinuesFrom(Quest("one")).ReadyByCode());

        Assert.Equal(["one", "story"], quest.Parents);
        Assert.Equal("one", quest.Continued);
    }

    [Fact]
    public void AnEmptyOfferExpressionIsRefused() =>
        Assert.Throws<ArgumentException>(() => Build(q => q.OfferWhenExpression(" ").ReadyByCode()));

    [Fact]
    public void AnOfferExpressionBecomesTheOfferCondition()
    {
        var quest = Build(q => q.OfferWhenExpression("$money >= 500").ReadyByCode());

        Assert.NotNull(quest.OfferCondition);
    }

    [Fact]
    public void FlagsStartOffAndAreSetByTheirCalls()
    {
        var plain = Build(q => q.ReadyByCode());
        var flagged = Build(q => q.Setup(_ => true).Repeatable().ExpiresAfter(3).OnCancel(_ => { }).ReadyByCode());

        Assert.False(plain.Repeats);
        Assert.Equal(0, plain.ExpireDays);
        Assert.Null(plain.WhenCanceled);
        Assert.True(flagged.Repeats);
        Assert.Equal(3, flagged.ExpireDays);
        Assert.NotNull(flagged.WhenCanceled);
    }

    [Fact]
    public void AQuestWaitingOnCodeOrAnEventNeedsAMarkAndATalkQuestDoesNot()
    {
        Assert.True(Build(q => q.ReadyByCode()).NeedsMark);
        Assert.True(Build(q => q.ReadyOn(TimbnQuestEvent.Slept())).NeedsMark);
        Assert.False(Build(q => q.Talk(talk => talk.Say("Here.").Accept())).NeedsMark);
    }

    [Fact]
    public void ReadsRunOnlyWhenSomethingComesFromTheRun()
    {
        Assert.False(Build(q => q.ReadyByCode()).ReadsRun);
        Assert.True(Build(q => q.Setup(_ => true).Cost((cost, run) => cost.Item(run.Item("dish"))).Talk(talk => talk.Say("Here.").Accept("Take it."))).ReadsRun);
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
