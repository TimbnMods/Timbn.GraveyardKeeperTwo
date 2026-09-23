using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>How a <see cref="TimbnQuest"/> is completed.</summary>
public enum TimbnQuestCompletion
{
    /// <summary>
    /// The player brings the NPC the items in <see cref="TimbnQuest.Wants"/> and hands them over with the
    /// <see cref="TimbnQuest.Give"/> answer. The game takes the items and completes the quest.
    /// </summary>
    Fetch,

    /// <summary>
    /// Your code completes the quest by calling <see cref="TimbnQuest.Complete"/>, for example when the player
    /// reaches a place or kills something. The NPC still offers the quest and gives the reminder lines.
    /// </summary>
    Custom,
}

/// <summary>
/// Describes a whole NPC quest in one object, so a mod adds it with a single call to the plugin's
/// Quests.Add. Core registers the quest in the quest log and tree, shows a speech icon on the NPC when there
/// is something to say, and runs both conversations: the offer (with accept and decline) and, while the quest
/// is in progress, the reminder and hand in. The text fields hold the actual lines. Core turns them into
/// localisation keys named after the quest id, so a translation can override any of them.
/// </summary>
/// <example>
/// Larry wants two bottles of wine once the intro is over, and pays 180 for them.
/// <code><![CDATA[
/// Quests.Add(new TimbnQuest
/// {
///     Id = "my_larry_wine",
///     Npc = "npc_larry",
///     Name = "Larry's Thirst",
///     Description = "Larry wants 2 bottles of wine.",
///     Wants = [TimbnQuests.Item("grape_wine:1", 2)],
///     RewardMoney = 180,
///     AvailableWhen = () => TimbnQuests.StatusOf("6_intro_guards_burial") is QuestStatus.Completed,
///     Offer = ["Oi, Keeper!", "Fetch us 2 bottles of wine, would you?"],
///     Accept = "Fine.",
///     Decline = "Not now, Larry.",
///     Accepted = ["Cheers, guv!"],
///     Remind = ["Where's me wine?"],
///     Give = "Here's your wine.",
///     Later = "Still working on it.",
///     Thanks = ["Lovely jubbly."],
/// });
/// ]]></code>
/// </example>
public sealed class TimbnQuest
{
    /// <summary>The quest id. Keep it stable, since saves store progress under it.</summary>
    public string Id { get; set; } = "";

    /// <summary>The world object id of the NPC who gives the quest, such as npc_larry.</summary>
    public string Npc { get; set; } = "";

    /// <summary>The quest's icon in the quest tree and log. Empty uses the game's default.</summary>
    public string Icon { get; set; } = "";

    /// <summary>The quest's name in the quest log and tree.</summary>
    public string Name { get; set; } = "";

    /// <summary>The quest log description while the quest is in progress.</summary>
    public string Description { get; set; } = "";

    /// <summary>The quest log description once the quest is done. Empty keeps <see cref="Description"/>.</summary>
    public string CompletedDescription { get; set; } = "";

    /// <summary>How the quest is completed. <see cref="TimbnQuestCompletion.Fetch"/> by default.</summary>
    public TimbnQuestCompletion Completion { get; set; } = TimbnQuestCompletion.Fetch;

    /// <summary>
    /// For a fetch quest, the items the player hands over, built with TimbnQuests.Item. The hand in answer shows
    /// them as its price and is greyed out until the player carries them.
    /// </summary>
    public ItemCount[] Wants { get; set; } = [];

    /// <summary>
    /// Money paid when the quest is completed, in the game's raw units, where 100 is one silver coin. A fetch
    /// quest shows it on the hand in answer.
    /// </summary>
    public int RewardMoney { get; set; }

    /// <summary>
    /// The id of a quest that has to be completed before this one is offered. The quest tree also draws this
    /// quest as its child. Empty offers it without one.
    /// </summary>
    public string After { get; set; } = "";

    /// <summary>
    /// An extra condition for offering the quest, checked about once a second, such as a story quest having
    /// started. Null means no extra condition.
    /// </summary>
    public Func<bool>? AvailableWhen { get; set; }

    /// <summary>What the NPC says when offering the quest, one speech bubble per line.</summary>
    public string[] Offer { get; set; } = [];

    /// <summary>The player's answer that accepts the quest.</summary>
    public string Accept { get; set; } = "";

    /// <summary>The player's answer that turns the quest down for now. Empty leaves only the accept answer.</summary>
    public string Decline { get; set; } = "";

    /// <summary>What the NPC says after the player accepts.</summary>
    public string[] Accepted { get; set; } = [];

    /// <summary>What the NPC says after the player declines.</summary>
    public string[] Declined { get; set; } = [];

    /// <summary>
    /// What the NPC says while the quest is in progress. A fetch quest says it when the player comes to hand in
    /// the items, and a custom quest says it whenever the player talks to the NPC.
    /// </summary>
    public string[] Remind { get; set; } = [];

    /// <summary>For a fetch quest, the player's answer that hands the items over and completes the quest.</summary>
    public string Give { get; set; } = "";

    /// <summary>For a fetch quest, the player's answer that leaves without handing in. Empty leaves only the give answer.</summary>
    public string Later { get; set; } = "";

    /// <summary>What the NPC says after a fetch quest is handed in.</summary>
    public string[] Thanks { get; set; } = [];

    /// <summary>The quest's status in the loaded save, or null when no save is loaded.</summary>
    public QuestStatus? Status => TimbnQuests.StatusOf(Id);

    /// <summary>
    /// Completes the quest from code and pays <see cref="RewardMoney"/>. This is how a custom quest finishes. It
    /// does nothing unless the quest is in progress in the loaded save.
    /// </summary>
    public void Complete()
    {
        if (Status != QuestStatus.InProgress)
            return;

        TimbnQuests.Complete(Id);
        if (RewardMoney > 0)
            MainGame.PlayerData.AddRes(LazyConsts.MONEY_KEY, RewardMoney);
    }

    internal string GiveKey => Id + "_give";

    internal void Validate()
    {
        if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(Npc))
            throw new ArgumentException("A TimbnQuest needs an Id and an Npc.");

        if (string.IsNullOrEmpty(Accept))
            throw new ArgumentException($"Quest {Id} needs an Accept answer.");

        if (Completion == TimbnQuestCompletion.Fetch && (Wants.Length == 0 || string.IsNullOrEmpty(Give)))
            throw new ArgumentException($"Fetch quest {Id} needs Wants and a Give answer.");
    }

    internal QuestDef ToDefinition()
    {
        var definition = TimbnQuests.CreateDefinition(Id);
        if (Completion == TimbnQuestCompletion.Fetch)
            definition.FinishOnAnswer(GiveKey, Wants).RewardMoney(RewardMoney);

        if (!string.IsNullOrEmpty(After))
            definition.After(After);

        definition.wgoNpcId = Npc;
        definition.iconId = Icon;
        return definition;
    }

    internal Dictionary<string, string> Texts()
    {
        var texts = new Dictionary<string, string>
        {
            [Id] = Name,
            ["quest_open_" + Id + "_d"] = Description,
            ["quest_closed_" + Id + "_d"] = string.IsNullOrEmpty(CompletedDescription) ? Description : CompletedDescription,
            [Id + "_accept"] = Accept,
            [Id + "_decline"] = Decline,
            [GiveKey] = Give,
            [Id + "_later"] = Later,
        };
        AddLines(texts, "offer", Offer);
        AddLines(texts, "accepted", Accepted);
        AddLines(texts, "declined", Declined);
        AddLines(texts, "remind", Remind);
        AddLines(texts, "thanks", Thanks);
        return texts;
    }

    internal bool IsOffered()
    {
        switch (Status)
        {
            case null or QuestStatus.Completed or QuestStatus.Canceled:
                return false;
            case QuestStatus.InProgress:
                return Completion == TimbnQuestCompletion.Fetch ? TimbnQuests.CanFinish(Id) : Remind.Length > 0;
            default:
                return (string.IsNullOrEmpty(After) || TimbnQuests.StatusOf(After) == QuestStatus.Completed)
                    && (AvailableWhen?.Invoke() ?? true);
        }
    }

    internal void Script(TimbnConversation talk)
    {
        if (Status == QuestStatus.InProgress)
        {
            SayLines(talk, "remind", Remind);
            if (Completion != TimbnQuestCompletion.Fetch)
                return;

            talk.Ask(Answers(GiveKey, Id + "_later", Later))
                .If(GiveKey, then => SayLines(then, "thanks", Thanks));
            return;
        }

        SayLines(talk, "offer", Offer);
        talk.Ask(Answers(Id + "_accept", Id + "_decline", Decline))
            .If(Id + "_accept", then => SayLines(then.Do(() => TimbnQuests.Start(Id)), "accepted", Accepted))
            .If(Id + "_decline", then => SayLines(then, "declined", Declined));
    }

    private static string[] Answers(string first, string secondKey, string secondText) =>
        string.IsNullOrEmpty(secondText) ? [first] : [first, secondKey];

    private void AddLines(Dictionary<string, string> texts, string part, string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
            texts[$"{Id}_{part}_{i + 1}"] = lines[i];
    }

    private void SayLines(TimbnConversation talk, string part, string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
            talk.Say($"{Id}_{part}_{i + 1}");
    }
}
