namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds quests to the game on behalf of a plugin, removed again when the plugin unloads. Reach it through the
/// plugin's Quests property.
/// </summary>
public sealed class TimbnPluginQuests
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginQuests(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a quest. Core registers it in the quest tree, adds its text, shows a speech bubble on the NPC
    /// when there is something to say, runs the offer and talk conversations, and pays its rewards. See <see cref="TimbnQuestBuilder"/> for what a quest can hold. Call it in OnAwake.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// var wine = Quests.Add("my_larry_wine", TimbnGameIds.Npcs.Larry, "Larry's Thirst", quest => quest
    ///     .Description("Larry wants 3 bottles of wine.")
    ///     .Offer(talk => talk
    ///         .Say("Could you fetch us 3 bottles of wine?")
    ///         .Accept("Fine."))
    ///     .Cost(cost => cost.Item("grape_wine:1", 3))
    ///     .Reward(reward => reward.Money(300))
    ///     .Talk(talk => talk.Accept("Here's your wine.", then => then.Say("Lovely."))));
    /// ]]></code>
    /// </example>
    /// <param name="id">The quest id. Keep it stable, since saves keep progress under it.</param>
    /// <param name="npcId">The NPC who gives the quest, from <see cref="TimbnGameIds.Npcs"/>, such as TimbnGameIds.Npcs.Larry.</param>
    /// <param name="name">The quest's name in the quest tree.</param>
    /// <param name="quest">Describes the quest.</param>
    /// <returns>
    /// The quest, for starting it, marking it ready, or chaining another quest after it. It lives until the plugin
    /// unloads, since a quest in a loaded save should not vanish mid save. When another mod already adds a quest
    /// with the same id, this one is skipped and named in the main menu's load error popup.
    /// </returns>
    public TimbnQuest Add(string id, string npcId, string name, Action<TimbnQuestBuilder> quest)
    {
        if (string.IsNullOrEmpty(npcId))
            throw new ArgumentException($"Quest {id} needs an NPC. Use the Add without one and call Giver to read the NPC from the run.", nameof(npcId));

        return Build(new TimbnQuestBuilder(id, npcId, name), quest);
    }

    /// <summary>
    /// Adds a quest whose giver changes from one play to the next, read from its run with <see cref="TimbnQuestBuilder.Giver(Func{TimbnQuestRun, string})"/>.
    /// Everything else works the same as the Add that takes an NPC.
    /// </summary>
    /// <example>
    /// A villager asks for a dish about once a week, and pays its sale price for it.
    /// <code><![CDATA[
    /// var craving = Quests.Add("my_craving", "A Villager's Craving", quest => quest
    ///     .Repeatable()
    ///     .Setup(run =>
    ///     {
    ///         var villagers = TimbnNpcs.GroupMemberIds(TimbnGameIds.Groups.VillagersDayAll);
    ///         if (villagers.Count == 0)
    ///             return false;
    ///
    ///         run.Npc("villager", villagers[UnityEngine.Random.Range(0, villagers.Count)]).Item("dish", "cook_muffin");
    ///         return true;
    ///     })
    ///     .Giver(run => run.NpcId("villager"))
    ///     .Description("Bring {villager} {dish}.")
    ///     .Offer(talk => talk
    ///         .Say("Could you bring me {dish}?")
    ///         .Accept("I'll bring it.")
    ///         .Decline("Not now."))
    ///     .Cost((cost, run) => cost.Item(run.Item("dish")))
    ///     .Reward((reward, run) => reward.Money(TimbnItems.SalePrice(run.Item("dish"))))
    ///     .Talk(talk => talk
    ///         .Accept("Here you go.", then => then.Say("Thank you!"))
    ///         .Decline("Not yet.")));
    ///
    /// Events.NewDayStarted(_ =>
    /// {
    ///     if (UnityEngine.Random.Range(0, 7) == 0)
    ///         craving.NewRun();
    /// });
    /// ]]></code>
    /// </example>
    /// <param name="id">The quest id. Keep it stable, since saves keep progress under it.</param>
    /// <param name="name">The quest's name in the quest tree.</param>
    /// <param name="quest">Describes the quest. It needs a Giver.</param>
    /// <returns>The quest, for rolling and canceling runs.</returns>
    public TimbnQuest Add(string id, string name, Action<TimbnQuestBuilder> quest) => Build(new TimbnQuestBuilder(id, "", name), quest);

    /// <summary>
    /// Adds a quest with its giver read from its run, like the Add that takes a name, with the name only in the plugin's
    /// lang files under the quest id. Pair it with <see cref="TimbnQuestBuilder.Description()"/>,
    /// <see cref="TimbnTalk{TSelf}.Line"/>, and the Accept and Decline that take no text, so all of the quest's text
    /// lives in lang/en.txt and its translations.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// var craving = Quests.Add("my_craving", quest => quest
    ///     .Repeatable()
    ///     .Setup(run => ...)
    ///     .Giver(run => run.NpcId("villager"))
    ///     .Description()
    ///     .Offer(talk => talk
    ///         .Line("ask")
    ///         .Accept(then => then.Line("thanks"))
    ///         .Decline())
    ///     .Cost((cost, run) => cost.Item(run.Item("dish")))
    ///     .Talk(talk => talk
    ///         .Accept(then => then.Line("thanks"))
    ///         .Decline()));
    /// ]]></code>
    /// with lang/en.txt holding my_craving, quest_open_my_craving_d, my_craving_offer_ask, my_craving_accept,
    /// my_craving_accept_thanks, my_craving_decline, my_craving_talk_accept, my_craving_talk_accept_thanks and
    /// my_craving_talk_decline.
    /// </example>
    /// <param name="id">The quest id. Keep it stable, since saves keep progress under it.</param>
    /// <param name="quest">Describes the quest. It needs a Giver.</param>
    /// <returns>The quest, for rolling and canceling runs.</returns>
    public TimbnQuest Add(string id, Action<TimbnQuestBuilder> quest) => Build(new TimbnQuestBuilder(id, "", TimbnLocale.FileText(id)), quest);

    private TimbnQuest Build(TimbnQuestBuilder builder, Action<TimbnQuestBuilder> quest)
    {
        var id = builder.Id;
        quest(builder);
        builder.Finish();
        var owner = _owner.Metadata.GUID;
        var cycle = new TimbnQuestCycle(builder);
        List<TimbnQuestRuntime> steps = [.. builder.Steps.Select(step => new TimbnQuestRuntime(step, () => _owner.Dialog.BubbleColor, owner, cycle))];
        cycle.Use(steps);
        if (builder.Steps.Select(step => TimbnQuests.OtherOwnerOf(step.Id, owner)).FirstOrDefault(other => other is not null) is { } other)
        {
            var otherName = TimbnQuests.OwnerName(other);
            _owner.PluginLogger.LogError($"{nameof(TimbnQuests)}|Quest '{id}' was skipped, since {otherName} already adds a quest with that id.");
            TimbnLoadErrors.AddProblem($"{_owner.Metadata.Name} and {otherName} both add the quest '{id}', so {_owner.Metadata.Name}'s was skipped.");
            return new TimbnQuest(steps, builder.StageNames, added: false, cycle, builder.Repeats);
        }

        // Each step and the cycle rebuild the game's quest cache several times over, so they share one rebuild.
        using (TimbnQuestRuntime.Batch())
        {
            foreach (var runtime in steps)
                _owner.Subscriptions.Add(runtime.Register());

            _owner.Subscriptions.Add(cycle.Register());
        }

        return new TimbnQuest(steps, builder.StageNames, added: true, cycle, builder.Repeats);
    }
}
