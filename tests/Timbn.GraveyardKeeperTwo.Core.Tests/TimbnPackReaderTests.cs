using Newtonsoft.Json.Linq;
using Timbn.GraveyardKeeperTwo.QuestPacks;
using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnPackReaderTests
{
    [Fact]
    public void AQuestGetsThePackPrefixAndItsTextKeys()
    {
        var content = Read("""
            {
              "quests": [
                {
                  "id": "wine",
                  "giver": "npc_larry",
                  "name": "Larry's Thirst",
                  "description": "Bring Larry 3 bottles of wine.",
                  "offer": {
                    "lines": ["Could you fetch us some wine?"],
                    "accept": "Fine.",
                    "decline": { "text": "Not now.", "then": ["Suit yourself."] }
                  },
                  "cost": { "items": { "grape_wine:1": 3 } },
                  "reward": { "money": 300, "reputation": 10 },
                  "talk": {
                    "accept": {
                      "text": "Here's your wine.",
                      "then": ["Lovely."]
                    }
                  }
                }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        var quest = Assert.Single(content.Quests);
        Assert.Equal("pack_wine", quest.Id);
        Assert.Equal("npc_larry", quest.GiverId);
        Assert.Equal("npc_larry", quest.TalkNpc);
        Assert.Equal("Larry's Thirst", quest.Texts["pack_wine"]);
        Assert.Equal("Bring Larry 3 bottles of wine.", quest.Texts["quest_open_pack_wine_d"]);
        Assert.Equal("Could you fetch us some wine?", quest.Texts["pack_wine_offer_1"]);
        Assert.Equal("Fine.", quest.Texts["pack_wine_accept"]);
        Assert.Equal("Here's your wine.", quest.Texts["pack_wine_talk_accept"]);
        Assert.Equal("Lovely.", quest.Texts["pack_wine_talk_accept_1"]);
        Assert.Equal(300, quest.RewardsOf.MoneyTotal);
        Assert.False(quest.CostOf.IsEmpty);
    }

    [Fact]
    public void ATalkReadsItsLinesAnswersResumeAndNeeds()
    {
        var content = Read("""
            {
              "quests": [
                {
                  "id": "boards",
                  "giver": "npc_larry",
                  "name": "Boards",
                  "description": "Buy boards from Herm.",
                  "npc": "npc_herm",
                  "needs": { "items": { "flitch": 1 }, "resources": { "happiness": 1 }, "money": 50, "reputation": 10, "happiness": 2, "onDay": "Pride", "orderCompleted": "quest_order_iron" },
                  "cost": { "money": 200, "items": { "stick": 1 }, "resources": { "happiness": 1 } },
                  "talk": {
                    "lines": [ { "playerSay": "Larry sent me." }, "That'll be 2 silver." ],
                    "accept": { "text": "Here's the silver.", "then": ["Come back on Pride."] },
                    "decline": { "text": "Not now.", "then": ["Suit yourself."] },
                    "resume": "Here to pay?"
                  }
                }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        var quest = Assert.Single(content.Quests);
        Assert.Equal("npc_herm", quest.TalkNpc);
        Assert.False(quest.NeedsOf.IsEmpty);
        Assert.Equal("Larry sent me.", quest.Texts["pack_boards_talk_1"]);
        Assert.Equal("That'll be 2 silver.", quest.Texts["pack_boards_talk_2"]);
        Assert.Equal("Here's the silver.", quest.Texts["pack_boards_talk_accept"]);
        Assert.Equal("Come back on Pride.", quest.Texts["pack_boards_talk_accept_1"]);
        Assert.Equal("Not now.", quest.Texts["pack_boards_talk_decline"]);
        Assert.Equal("Suit yourself.", quest.Texts["pack_boards_talk_decline_1"]);
        Assert.Equal("Here to pay?", quest.Texts["pack_boards_talk_resume"]);
        Assert.False(quest.CostOf.IsEmpty);
        Assert.True(quest.RewardsOf.IsEmpty);
    }

    [Fact]
    public void OfferAcceptAndAskAnswersTakeNeedsAndCost()
    {
        var content = Read("""
            {
              "quests": [
                {
                  "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept",
                  "offer": { "accept": { "text": "Fine.", "needs": { "onDay": "Pride" }, "cost": { "money": 100 } } }
                }
              ],
              "talks": [
                {
                  "id": "trade", "npc": "npc_larry",
                  "lines": [ { "ask": [ { "text": "Trade.", "needs": { "reputation": 5 }, "cost": { "items": { "stick": 1 } }, "reward": { "shows": { "flitch": 2 } } }, "Bye." ] } ]
                }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        Assert.False(Assert.Single(content.Quests).OfferTalk!.AcceptWants!.IsEmpty);
        Assert.Single(content.Talks);
    }

    [Theory]
    [InlineData("\"wants\": { \"items\": { \"grape_wine:1\": 1 } }")]
    [InlineData("\"handIn\": { \"give\": \"Here.\" }")]
    [InlineData("\"intro\": [ \"More, please.\" ]")]
    public void TheOldFieldsAreReportedAsUnknown(string field)
    {
        var content = Read($$"""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept", {{field}} } ] }""");

        Assert.Single(content.Quests);
        Assert.Contains("doesn't belong here", Assert.Single(content.Problems));
    }

    [Theory]
    [InlineData("reward")]
    [InlineData("cost")]
    public void ACostOrRewardOnTheAcceptIsReportedSinceItBelongsToTheStep(string field)
    {
        var content = Read($$"""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "talk": { "accept": { "text": "Here.", "{{field}}": { "money": 5 } } } } ] }""");

        Assert.Single(content.Quests);
        Assert.Contains($"\"{field}\" doesn't belong here", Assert.Single(content.Problems));
    }

    [Fact]
    public void AStepTakesItsCost()
    {
        var content = Read("""
            {
              "quests": [
                {
                  "id": "q", "giver": "npc_larry", "name": "Q",
                  "cost": { "money": 200 },
                  "talk": { "accept": "Here's the silver." },
                  "stages": [
                    { "id": "deliver", "description": "Bring the wine.", "cost": { "items": { "grape_wine:1": 1 } }, "talk": { "accept": "Here." } }
                  ]
                }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        var quest = Assert.Single(content.Quests);
        Assert.Equal(["pack_q", "pack_q_deliver"], quest.Steps.Select(step => step.Id));
        Assert.All(quest.Steps, step => Assert.False(step.CostOf.IsEmpty));
    }

    [Fact]
    public void AcceptTrueFinishesTheStepOnceTheLinesAreDone()
    {
        var content = Read("""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "reward": { "money": 5 }, "talk": { "lines": [ "Here, take this." ], "accept": true } } ] }""");

        Assert.Empty(content.Problems);
        var talk = Assert.Single(content.Quests).TalkOf!;
        Assert.True(talk.HasAccept);
        Assert.Null(talk.AcceptKey);
        Assert.Equal("Here, take this.", Assert.Single(content.Quests).Texts["pack_q_talk_1"]);
    }

    [Theory]
    [InlineData("\"lines\": [ \"Hi.\" ]")]
    [InlineData("\"lines\": [ \"Hi.\" ], \"accept\": false")]
    [InlineData("\"accept\": true")]
    [InlineData("\"lines\": [ \"Hi.\" ], \"accept\": true, \"decline\": \"No.\"")]
    public void ATalkWithNoAcceptOrABadAcceptTrueIsAProblem(string talk)
    {
        var content = Read($$"""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "talk": { {{talk}} } } ] }""");

        Assert.Empty(content.Quests);
        Assert.Contains(content.Problems, problem => problem.Contains("quest 'q'"));
    }

    [Fact]
    public void AcceptTrueWithACostIsAProblem()
    {
        var content = Read("""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "cost": { "money": 5 }, "talk": { "lines": [ "Hi." ], "accept": true } } ] }""");

        Assert.Empty(content.Quests);
        Assert.Contains(content.Problems, problem => problem.Contains("quest 'q'") && problem.Contains("Cost"));
    }

    [Fact]
    public void ATalkStepTakesTheStepsReward()
    {
        var content = Read("""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "talk": { "accept": "Here." }, "reward": { "money": 5 } } ] }""");

        Assert.Empty(content.Problems);
        Assert.Equal(5, Assert.Single(content.Quests).RewardsOf.MoneyTotal);
    }

    [Fact]
    public void AQuestsOwnFieldsAreItsFirstStepAndItsStagesFollow()
    {
        var content = Read("""
            {
              "quests": [
                {
                  "id": "gold", "giver": "npc_larry", "name": "Gold",
                  "description": "Pay Herm.",
                  "npc": "npc_herm",
                  "cost": { "money": 200 },
                  "talk": { "accept": { "text": "Here's the silver." } },
                  "done": "Larry got his gold wine.",
                  "stages": [
                    { "id": "deliver", "description": "Bring Larry the wine.", "reward": { "money": 300 }, "talk": { "accept": { "text": "Here's your wine." } } }
                  ]
                },
                { "id": "next", "giver": "npc_larry", "name": "Next", "readyOn": "slept", "after": "gold" }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        var quest = content.Quests.Single(quest => quest.Id == "pack_gold");
        Assert.Equal(["pack_gold", "pack_gold_deliver"], quest.Steps.Select(step => step.Id));
        Assert.Equal("npc_herm", quest.Steps[0].TalkNpc);
        Assert.Equal("npc_larry", quest.Steps[1].TalkNpc);
        Assert.Equal("Here's the silver.", quest.Texts["pack_gold_talk_accept"]);
        Assert.Equal("Here's your wine.", quest.Steps[1].Texts["pack_gold_deliver_talk_accept"]);
        Assert.Equal("Pay Herm.", quest.Texts["quest_closed_pack_gold_d"]);
        Assert.Equal("Larry got his gold wine.", quest.Steps[1].Texts["quest_closed_pack_gold_deliver_d"]);
        Assert.Equal(["pack_gold_deliver"], content.Quests.Single(quest => quest.Id == "pack_next").Parents);
    }

    [Fact]
    public void ShortIdsMeanThePacksOwnQuestsAndOthersStayAsWritten()
    {
        var content = Read("""
            {
              "quests": [
                { "id": "first", "giver": "npc_larry", "name": "First", "readyOn": "slept", "after": "6_intro_guards_burial" },
                { "id": "second", "giver": "npc_larry", "name": "Second", "readyOn": "slept", "continues": "first", "after": ["other_pack_quest"] }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        Assert.Equal(["6_intro_guards_burial"], content.Quests[0].Parents);
        Assert.Equal("pack_first", content.Quests[1].Continued);
        Assert.Equal(["pack_first", "other_pack_quest"], content.Quests[1].Parents);
    }

    [Fact]
    public void AfterAndContinuesOnAStagedQuestMeanItsLastStage()
    {
        var content = Read("""
            {
              "quests": [
                { "id": "next", "giver": "npc_larry", "name": "Next", "readyOn": "slept", "continues": "gold" },
                { "id": "later", "giver": "npc_larry", "name": "Later", "readyOn": "slept", "after": ["gold", "single"] },
                {
                  "id": "gold", "giver": "npc_larry", "name": "Gold",
                  "stages": [
                    { "id": "ask", "description": "Ask.", "readyOn": "slept" },
                    { "id": "pay", "description": "Pay.", "readyOn": "slept" }
                  ]
                },
                { "id": "single", "giver": "npc_larry", "name": "Single", "stages": [ { "id": "only", "description": "Only.", "readyOn": "slept" } ] }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        Assert.Equal("pack_gold_pay", content.Quests.Single(quest => quest.Id == "pack_next").Continued);
        Assert.Equal(["pack_gold_pay", "pack_single"], content.Quests.Single(quest => quest.Id == "pack_later").Parents);
    }

    [Fact]
    public void ARootWithOnlyAnIconOrDoneLineStillCountsAsTheFirstStage()
    {
        var content = Read("""
            {
              "quests": [
                { "id": "marked", "giver": "npc_larry", "name": "Marked", "icon": "wine_icon", "stages": [ { "id": "only", "description": "Only.", "readyOn": "slept" } ] },
                { "id": "later", "giver": "npc_larry", "name": "Later", "readyOn": "slept", "after": ["marked"] }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        Assert.Equal(["pack_marked_only"], content.Quests.Single(quest => quest.Id == "pack_later").Parents);
    }

    [Fact]
    public void QuestCompletedReadsTheLastStageAndTheOtherQuestRulesTheFirst()
    {
        List<string> starts = [];
        List<string> ends = [];
        var rules = new TimbnPackRules(
            id =>
            {
                starts.Add(id);
                return id;
            },
            id =>
            {
                ends.Add(id);
                return id;
            });

        rules.Condition(new JValue("questCompleted:gold"));
        rules.Condition(new JValue("questStarted:ask"));
        rules.Condition(new JValue("questNotStarted:herm"));
        rules.Action(new JValue("startQuest:wine"));

        Assert.Equal(["gold"], ends);
        Assert.Equal(["ask", "herm", "wine"], starts);
    }

    [Theory]
    [InlineData("\"phase\": 99999999999")]
    [InlineData("\"phase\": 999999999999999999999999999")]
    [InlineData("\"reward\": { \"money\": 99999999999 }")]
    [InlineData("\"reward\": { \"resources\": { \"money\": 999999999999999999999999999 } }")]
    [InlineData("\"offerWhen\": { \"carries\": \"flitch\", \"count\": 999999999999999999999999999 }")]
    public void ANumberTooBigIsAProblemNotACrash(string field)
    {
        var content = Read($$"""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept", {{field}} }, { "id": "fine", "giver": "npc_larry", "name": "Fine", "readyOn": "slept" } ] }""");

        Assert.Equal("pack_fine", Assert.Single(content.Quests).Id);
        Assert.Contains(content.Problems, problem => problem.Contains("quest 'q'") && problem.Contains("too big"));
    }

    [Fact]
    public void ContinuingAQuestFromOutsideThePackIsAProblem()
    {
        var content = Read("""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept", "continues": "6_intro_guards_burial" } ] }""");

        Assert.Empty(content.Quests);
        Assert.Contains(content.Problems, problem => problem.Contains("continues") && problem.Contains("quest 'q'"));
    }

    [Fact]
    public void AnUnknownFieldIsReportedButTheQuestStillLoads()
    {
        var content = Read("""
            {
              "quests": [
                { "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept", "descriptoin": "Typo." }
              ]
            }
            """);

        Assert.Single(content.Quests);
        var problem = Assert.Single(content.Problems);
        Assert.Contains("\"descriptoin\"", problem);
        Assert.Contains("line 3", problem);
    }

    [Fact]
    public void ABrokenFileIsSkippedAndTheOthersLoad()
    {
        var content = new TimbnPackReader("pack", []).Read(
        [
            ("pack/quests/broken.json", """{ "quests": [ { "id": "a", } """),
            ("pack/quests/good.json", """{ "quests": [ { "id": "b", "giver": "npc_larry", "name": "B", "readyOn": "slept" } ] }"""),
        ]);

        Assert.Equal("pack_b", Assert.Single(content.Quests).Id);
        Assert.Contains(content.Problems, problem => problem.StartsWith("pack/quests/broken.json", StringComparison.Ordinal));
    }

    [Fact]
    public void TheBuildersOwnMistakesSkipTheQuestWithItsFileAndId()
    {
        var content = Read("""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "offer": { "lines": ["Hi."] }, "readyOn": "slept" } ] }""");

        Assert.Empty(content.Quests);
        Assert.Contains(content.Problems, problem => problem.Contains("quests.json, quest 'q'") && problem.Contains("Accept"));
    }

    [Fact]
    public void ReadyWhenWaitsForCodeAndKeepsItsCondition()
    {
        var content = Read("""{ "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "readyWhen": { "all": ["hasTech:wood_basic", { "carries": "flitch", "count": 2 }] } } ] }""");

        Assert.Empty(content.Problems);
        Assert.True(content.Quests[0].WaitsForCode);
        Assert.True(content.ReadyWhen.ContainsKey("pack_q"));
    }

    [Fact]
    public void AnUnknownConditionNamesTheChoicesAndTheLine()
    {
        var content = Read("""
            {
              "quests": [
                { "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept",
                  "offerWhen": "hasMoney:5" }
              ]
            }
            """);

        Assert.Empty(content.Quests);
        Assert.Contains(content.Problems, problem => problem.Contains("line 4") && problem.Contains("questStarted"));
    }

    [Fact]
    public void AnUnknownNpcSkipsItsQuestOrTalkAndNamesTheId()
    {
        var content = Read("""
            {
              "quests": [
                { "id": "giver", "giver": "npc_lary", "name": "Q", "readyOn": "slept" },
                { "id": "hand_in", "giver": "npc_larry", "name": "Q", "npc": "npc_hrem", "talk": { "accept": "Here." } }
              ],
              "talks": [
                { "id": "tip", "npc": "npc_larry", "lines": [ { "sayAs": "herm", "text": "Nails!" } ] }
              ]
            }
            """);

        Assert.Empty(content.Quests);
        Assert.Empty(content.Talks);
        Assert.Contains(content.Problems, problem => problem.Contains("quest 'giver'") && problem.Contains("\"npc_lary\""));
        Assert.Contains(content.Problems, problem => problem.Contains("quest 'hand_in'") && problem.Contains("\"npc_hrem\""));
        Assert.Contains(content.Problems, problem => problem.Contains("talk 'tip'") && problem.Contains("\"herm\""));
    }

    [Fact]
    public void ATalkRecordsItsLinesUnderThePrefixedId()
    {
        var content = Read("""
            {
              "talks": [
                {
                  "id": "tip",
                  "npc": "npc_larry",
                  "once": true,
                  "bubbleColor": "#ff0000",
                  "lines": [
                    "Herm sells nails.",
                    { "playerSay": "You have %1 boards.", "values": ["itemCount:flitch"] },
                    { "ask": [ { "text": "Thanks.", "then": ["Any time."] }, "Bye." ] }
                  ]
                }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        var talk = Assert.Single(content.Talks);
        Assert.Equal("pack_tip", talk.Id);
        Assert.True(talk.Once);
        Assert.Equal(new Color(1f, 0f, 0f, 1f), talk.BubbleColor);
        Assert.Equal("Herm sells nails.", talk.Texts["pack_tip_1"]);
        Assert.Equal("You have %1 boards.", talk.Texts["pack_tip_2"]);
        Assert.Equal("Any time.", talk.Texts["pack_tip_4"]);
        Assert.Equal("Bye.", talk.Texts["pack_tip_5"]);
    }

    [Fact]
    public void StagesKeysAndTalksRead()
    {
        var content = Read("""
            {
              "quests": [
                {
                  "id": "wine",
                  "giver": "npc_larry",
                  "name": "Wine",
                  "offer": { "lines": [ { "say": "Wine?", "key": "ask" } ], "accept": "Sure." },
                  "stages": [
                    { "id": "first", "description": "Bring a bottle.", "cost": { "items": { "grape_wine:1": 1 } }, "talk": { "accept": { "text": "Here.", "then": [ "Could you bring 2 more?" ] } } },
                    { "id": "more", "description": "Bring 2 more.", "name": "More Wine", "readyWhen": "hasTech:wood_basic", "then": [ "Plenty." ] }
                  ]
                }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        var quest = Assert.Single(content.Quests);
        Assert.Equal(["pack_wine", "pack_wine_more"], quest.Steps.Select(step => step.Id));
        Assert.Equal("Wine?", quest.Texts["pack_wine_offer_ask"]);
        Assert.Equal("Here.", quest.Steps[0].Texts["pack_wine_talk_accept"]);
        Assert.Equal("Could you bring 2 more?", quest.Steps[0].Texts["pack_wine_talk_accept_1"]);
        Assert.Equal("Plenty.", quest.Steps[1].Texts["pack_wine_more_done_1"]);
        Assert.Equal("More Wine", quest.Steps[1].Texts["pack_wine_more"]);
        Assert.True(content.ReadyWhen.ContainsKey("pack_wine_more"));
    }

    [Fact]
    public void GameExpressionsRead()
    {
        var content = Read("""
            {
              "quests": [
                {
                  "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept",
                  "offerWhen": "expr:$money >= 500",
                  "runOnStart": "AddPerk(\"perk_x\")",
                  "runOnDone": [ "RevealTech(\"tech_x\")", "$money += 50" ],
                  "onDone": [ "run:AddRep(\"npc_larry\", 5)" ],
                  "then": [ { "run": "SetWorldPar(\"q_done\", 1)" }, { "when": "expr:IsQuestCompleted(\"x\")", "then": [ "Yes." ] } ]
                }
              ]
            }
            """);

        Assert.Empty(content.Problems);
        var quest = Assert.Single(content.Quests);
        Assert.Equal(["AddPerk(\"perk_x\")"], quest.StartExpressions.Select(expression => expression.Text));
        Assert.Equal(2, quest.DoneExpressions.Count);
        Assert.NotNull(quest.OfferCondition);
    }

    [Fact]
    public void AnIconNamingAPackSpriteGetsThePrefix()
    {
        var content = new TimbnPackReader("pack", ["wine_icon"]).Read(
        [
            ("quests.json", """{ "quests": [ { "id": "a", "giver": "npc_larry", "name": "A", "readyOn": "slept", "icon": "wine_icon" }, { "id": "b", "giver": "npc_larry", "name": "B", "readyOn": "slept", "icon": "quest_icon_larry_portal" } ] }"""),
        ]);

        Assert.Equal("pack_wine_icon", content.Quests[0].IconId);
        Assert.Equal("quest_icon_larry_portal", content.Quests[1].IconId);
    }

    [Fact]
    public void BadAndRepeatedIdsAreSkipped()
    {
        var content = Read("""{ "quests": [ { "id": "Bad Id", "giver": "npc_larry", "name": "A", "readyOn": "slept" }, { "id": "a", "giver": "npc_larry", "name": "A", "readyOn": "slept" }, { "id": "a", "giver": "npc_larry", "name": "A again", "readyOn": "slept" } ] }""");

        Assert.Equal("pack_a", Assert.Single(content.Quests).Id);
        Assert.Equal(2, content.Problems.Count);
    }

    [Fact]
    public void CommentsAreAllowed()
    {
        var content = Read("""
            {
              // Larry's first quest.
              "quests": [ { "id": "q", "giver": "npc_larry", "name": "Q", "readyOn": "slept" } ]
            }
            """);

        Assert.Empty(content.Problems);
        Assert.Single(content.Quests);
    }

    [Fact]
    public void PackInfoNeedsAnIdOfLowercaseLettersDigitsAndUnderscores()
    {
        var info = TimbnPackReader.ReadInfo("""{ "id": "larrys_wine", "name": "Larry's Wine", "bubbleColor": "9ad0ff" }""");

        Assert.Equal("larrys_wine", info.Id);
        Assert.Equal("Larry's Wine", info.Name);
        Assert.NotNull(info.BubbleColor);
        Assert.Throws<TimbnPackException>(() => TimbnPackReader.ReadInfo("""{ "id": "Larry's Wine" }"""));
        Assert.Throws<TimbnPackException>(() => TimbnPackReader.ReadInfo("""{ "name": "No id" }"""));
    }

    [Theory]
    [InlineData("slept")]
    [InlineData("crafted:candle")]
    [InlineData("walkedInto:vineyard_basement")]
    [InlineData("usedItem:grape_wine:1")]
    public void EventsReadTheGamesOwn(string text) =>
        Assert.NotNull(TimbnPackRules.Event(new JValue(text)));

    [Theory]
    [InlineData("crafted")]
    [InlineData("danced:badly")]
    public void UnknownEventsThrow(string text) =>
        Assert.Throws<TimbnPackException>(() => TimbnPackRules.Event(new JValue(text)));

    [Theory]
    [InlineData("time:18-24")]
    [InlineData("time:22-4")]
    [InlineData("day:pride")]
    [InlineData("resource:money:500")]
    [InlineData("questNotStarted:q")]
    [InlineData("reputation:npc_larry:20")]
    public void ConditionsParse(string text) =>
        Assert.NotNull(new TimbnPackRules(id => id).Condition(new JValue(text)));

    [Theory]
    [InlineData("time:25-2")]
    [InlineData("day:monday")]
    [InlineData("resource:money")]
    [InlineData("carries")]
    [InlineData("reputation:npc_lary:20")]
    public void BadConditionsThrow(string text) =>
        Assert.Throws<TimbnPackException>(() => new TimbnPackRules(id => id).Condition(new JValue(text)));

    [Theory]
    [InlineData("pack.schema.json")]
    [InlineData("quests.schema.json")]
    public void TheSchemasAreValidJson(string file)
    {
        var schema = JObject.Parse(File.ReadAllText(Path.Combine(PacksFolder(), file)));

        Assert.EndsWith($"/Timbn.GraveyardKeeperTwo.QuestPacks/{file}", (string)schema["$id"]!, StringComparison.Ordinal);
    }

    private static string PacksFolder([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "src", "Timbn.GraveyardKeeperTwo.QuestPacks");

    private static TimbnPackContent Read(string json) =>
        new TimbnPackReader("pack", []).Read([("pack/quests/quests.json", json)]);
}
