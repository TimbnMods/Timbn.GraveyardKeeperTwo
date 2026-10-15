using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a quest for the plugin's Quests.Add, with who gives it, when it is offered, and what the player does. A
/// quest is a list of stages. What is written on the builder itself (Description, TalksWith, Needs, Talk and the rest) is
/// its first stage, and each <see cref="AddStage"/> adds the next, which Core turns into one game quest per stage, each
/// continuing the one before, the way the game builds its own storylines. Every line is the text itself, and Core turns
/// it into localization keys named after the quest id. To keep the text only in the plugin's lang files, use the
/// Quests.Add that takes no name with Description(), Line, and the Accept and Decline that take no text.
/// </summary>
/// <example>
/// Larry wants 3 bottles of wine and pays 300 and a silver bottle for them.
/// <code><![CDATA[
/// Quests.Add("my_larry_wine", TimbnGameIds.Npcs.Larry, "Larry's Thirst", quest => quest
///     .Description("Larry wants 3 bottles of wine.")
///     .Offer(talk => talk
///         .Say("Could you fetch us 3 bottles of wine?")
///         .Accept("Fine.", then => then.Say("Cheers!"))
///         .Decline("Not now."))
///     .Cost(cost => cost.Item("grape_wine:1", 3))
///     .Reward(reward => reward.Money(300).Item("grape_wine:2"))
///     .Talk(talk => talk
///         .Accept("Here's your wine.", then => then.Say("Lovely."))
///         .Decline("Still working on it.")));
/// ]]></code>
/// </example>
public sealed class TimbnQuestBuilder
{
    private static readonly Regex _stageName = new("^[a-z0-9_]+$");

    internal static readonly Regex Placeholder = new(@"\{([a-z][a-z0-9_]*)\}");

    private readonly List<(string Name, string Description, Action<TimbnQuestStage> Build)> _stages = [];
    private readonly List<Action<TimbnNeeds, TimbnQuestRun?>> _needs = [];
    private readonly List<Action<TimbnCost, TimbnQuestRun?>> _costs = [];
    private readonly List<Action<TimbnRewards, TimbnQuestRun?>> _rewards = [];
    private string _description = "";
    private bool _descriptionInFile;
    private string? _done;

    internal TimbnQuestBuilder(string id, string npcId, string name)
        : this(id, npcId, new Dictionary<string, string>())
    {
        Texts[id] = name;
    }

    private TimbnQuestBuilder(string id, string npcId, Dictionary<string, string> texts)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("A quest needs an id.");

        Id = id;
        GiverId = npcId ?? "";
        Texts = texts;
    }

    internal string Id { get; }

    internal string GiverId { get; private set; }

    internal Func<TimbnQuestRun, string>? GiverOf { get; private set; }

    internal Func<TimbnQuestRun, string>? TalkNpcOf { get; private set; }

    internal Func<TimbnQuestRun, string>? IconOf { get; private set; }

    internal Func<TimbnQuestRun, bool>? SetupOf { get; private set; }

    internal bool Repeats { get; private set; }

    internal int ExpireDays { get; private set; }

    internal Action<TimbnQuestRun?>? WhenCanceled { get; private set; }

    internal bool HasRunNeeds { get; private set; }

    internal bool HasRunCost { get; private set; }

    internal bool HasRunRewards { get; private set; }

    internal Dictionary<string, string> Texts { get; }

    internal string IconId { get; set; } = "";

    internal int RingPhase { get; private set; }

    internal List<string> Parents { get; } = [];

    internal string? Continued { get; set; }

    internal Func<bool>? OfferCondition { get; set; }

    internal TimbnQuestOffer? OfferTalk { get; set; }

    internal TimbnQuestEvent? StartEvent { get; set; }

    internal TimbnWants NeedsOf { get; } = new();

    internal TimbnWants CostOf { get; } = new();

    internal TimbnRewards RewardsOf { get; } = new();

    internal bool WaitsForCode { get; private set; }

    internal TimbnQuestEvent? WaitsForEvent { get; private set; }

    internal string TalkNpc { get; private set; } = "";

    internal TimbnQuestTalk? TalkOf { get; private set; }

    internal TimbnLines? ThenLines { get; private set; }

    internal Action? WhenDone { get; private set; }

    internal Action? WhenStarted { get; private set; }

    internal Action? WhenEnded { get; private set; }

    internal List<TimbnExpression> StartExpressions { get; } = [];

    internal List<TimbnExpression> DoneExpressions { get; } = [];

    internal List<TimbnQuestBuilder> Steps { get; } = [];

    internal List<string> StageNames { get; } = [];

    internal bool NeedsMark => WaitsForCode || WaitsForEvent is not null;

    internal bool StartsWithContinued => Continued is not null && OfferTalk is null && StartEvent is null;

    /// <summary>
    /// The quest's icon in the quest tree and log, such as quest_icon_larry_portal. Left out, a quest that continues
    /// another uses that quest's icon, and any other quest the game's default.
    /// </summary>
    /// <param name="iconId">The icon's sprite id.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Icon(string iconId)
    {
        IconId = iconId;
        return this;
    }

    /// <summary>
    /// The quest's icon read from its run, such as the icon of the item the villager asked for. Any sprite the game
    /// has works, item icons included. Before the first run it shows the icon from <see cref="Icon(string)"/>.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .Icon(run => TimbnItems.IconId(run.Item("dish")))
    /// ]]></code>
    /// </example>
    /// <param name="iconId">Picks the icon's sprite id from the run.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Icon(Func<TimbnQuestRun, string> iconId)
    {
        IconOf = iconId;
        return this;
    }

    /// <summary>
    /// Which of the game's five rings the quest's node in the quest tree shows while the quest is in progress, the way
    /// the game sets one for each of its own quests. The green grows further around with each ring, and a completed
    /// quest always shows the gold laurel. Left out, a step of a ContinuesFrom chain gets a ring by its place in the chain,
    /// growing as the story goes on like the game's own storylines, and a quest on its own shows the first ring.
    /// </summary>
    /// <param name="phase">The ring, 1 to 5.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Phase(int phase)
    {
        if (phase is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(phase), $"Quest {Id} has phase {phase}, but the game only has rings 1 to 5.");

        RingPhase = phase;
        return this;
    }

    /// <summary>The quest description while the quest is in progress.</summary>
    /// <param name="text">The text.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Description(string text)
    {
        _description = text;
        _descriptionInFile = false;
        return this;
    }

    /// <summary>
    /// The quest description with its text only in the plugin's lang files, under quest_open_ plus the quest id plus _d
    /// while in progress, and quest_closed_ plus the id plus _d once done. Without the closed line it keeps the in
    /// progress one.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Description()
    {
        _descriptionInFile = true;
        return this;
    }

    /// <summary>The quest description once the quest is done. Left out, it keeps the in progress description.</summary>
    /// <param name="text">The text.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder DoneDescription(string text)
    {
        _done = text;
        return this;
    }

    /// <summary>Offers the quest only once another quest is completed, and draws it below that quest in the tree.</summary>
    /// <param name="quest">The quest returned by an earlier Quests.Add.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OfferAfter(TimbnQuest quest) =>
        quest.Repeats
            ? throw new ArgumentException($"Quest {Id} comes after {quest.Id}, which is repeatable and so never stays done.")
            : OfferAfter(quest.EndId);

    /// <summary>
    /// Offers the quest only once another quest is completed, and draws it below that quest in the tree with a line
    /// from it. Under a game quest it goes right below it, and when that area is full the quests below move down a
    /// row to make room.
    /// </summary>
    /// <param name="questId">The id of a mod quest or one of the game's own, such as 58_port_supplies_order.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OfferAfter(string questId)
    {
        Parents.Add(questId);
        return this;
    }

    /// <summary>
    /// Makes the quest the next step of another of your quests, the way the game chains its own quests. It takes that
    /// quest's place in the quest tree, sharing its cell, and the tree shows the newest step there. With no Offer or
    /// StartsOn of its own, it starts within about a second of that quest being completed, once its OfferWhen is true
    /// too if it has one. Only a mod quest can be continued, since a mod never takes a game quest's place.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// var ask = Quests.Add("my_wine_ask", TimbnGameIds.Npcs.Larry, "Larry's Wine", quest => ...);
    /// Quests.Add("my_wine_pay", TimbnGameIds.Npcs.Larry, "Paying for the Wine", quest => quest
    ///     .ContinuesFrom(ask)
    ///     .Description("Pay Herm for the wine.")
    ///     ...);
    /// ]]></code>
    /// </example>
    /// <param name="quest">The quest returned by an earlier Quests.Add. For a quest with stages, this continues its last stage.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder ContinuesFrom(TimbnQuest quest) =>
        quest.Repeats
            ? throw new ArgumentException($"Quest {Id} continues {quest.Id}, which is repeatable. Put the next part in a stage instead.")
            : ContinuesId(quest.EndId);

    internal TimbnQuestBuilder ContinuesId(string questId)
    {
        if (Continued is not null)
            throw new InvalidOperationException($"Quest {Id} continues two quests.");

        Continued = questId;
        Parents.Insert(0, questId);
        return this;
    }

    /// <summary>
    /// An extra condition for offering or starting the quest, such as a story quest having started. An offer is
    /// checked about once a second, and a start on a game event is checked when that event happens.
    /// </summary>
    /// <param name="condition">True when the quest may be offered.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OfferWhen(Func<bool> condition)
    {
        OfferCondition = condition;
        return this;
    }

    /// <summary>
    /// An extra condition for offering or starting the quest, written as one of the game's script expressions, such as
    /// <c>IsTechUnlocked("wood_basic") &amp;&amp; $money &gt;= 500</c>. It is checked like <see cref="OfferWhen(Func{bool})"/>.
    /// </summary>
    /// <param name="expression">The game expression, true when the quest may be offered.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OfferWhenExpression(string expression)
    {
        var condition = new TimbnExpression(expression);
        return OfferWhen(condition.IsTrue);
    }

    /// <summary>
    /// The conversation where the quest giver offers the quest. While the quest can be taken, the giver shows a speech
    /// bubble, and talking to them plays this conversation.
    /// </summary>
    /// <param name="talk">Scripts the offer. It needs an Accept answer.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Offer(Action<TimbnQuestOffer> talk)
    {
        if (OfferTalk is not null)
            throw new InvalidOperationException($"Quest {Id} has two Offer conversations.");

        OfferTalk = new TimbnQuestOffer(Id, Texts);
        talk(OfferTalk);
        return this;
    }

    /// <summary>Starts the quest by itself when the player does something in the game, while OfferAfter and OfferWhen allow it.</summary>
    /// <param name="gameEvent">The event.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder StartsOn(TimbnQuestEvent gameEvent)
    {
        StartEvent = gameEvent;
        return this;
    }

    /// <summary>
    /// The NPC the stage's Talk is with, such as TimbnGameIds.Npcs.Herm for a job Larry sends the player on. Left out, it is
    /// the quest giver.
    /// </summary>
    /// <param name="npcId">The NPC, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder TalksWith(string npcId)
    {
        TalkNpc = npcId;
        return this;
    }

    /// <summary>The NPC the stage's Talk is with, read from the quest's run, such as the villager a delivery goes to.</summary>
    /// <param name="npcId">Picks the NPC from the run.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder TalksWith(Func<TimbnQuestRun, string> npcId)
    {
        TalkNpcOf = npcId;
        return this;
    }

    /// <summary>
    /// The NPC who gives the quest, read from the quest's run, for a quest added with no NPC. The offer's speech bubble
    /// shows over them, and the Talk is with them unless <see cref="TalksWith(string)"/> names someone else.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .Giver(run => run.NpcId("villager"))
    /// ]]></code>
    /// </example>
    /// <param name="npcId">Picks the NPC from the run.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Giver(Func<TimbnQuestRun, string> npcId)
    {
        GiverOf = npcId;
        return this;
    }

    /// <summary>
    /// The NPC who gives the quest, for a quest added with no NPC. Use the Func form when the giver changes by run.
    /// </summary>
    /// <param name="npcId">The NPC, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Giver(string npcId)
    {
        GiverId = npcId;
        return this;
    }

    /// <summary>
    /// Fills a new run of the quest when your code calls the quest's NewRun, such as picking a villager and the dish they
    /// want. Until a run is rolled the quest is never offered or started. Text anywhere in the quest shows a run value
    /// by its name in braces, such as {dish}, and the overloads of Giver, TalksWith, Needs, Cost and Reward that take a run
    /// read it. The run is kept with the save and ends when the quest is done or canceled.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .Setup(run =>
    /// {
    ///     var villagers = TimbnNpcs.GroupMemberIds(TimbnGameIds.Groups.VillagersDayAll);
    ///     if (villagers.Count == 0)
    ///         return false;
    ///
    ///     run.Npc("villager", villagers[UnityEngine.Random.Range(0, villagers.Count)]).Item("dish", "cook_muffin");
    ///     return true;
    /// })
    /// ]]></code>
    /// </example>
    /// <param name="setup">Fills the run. Return false when nothing fits right now, and no run starts.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Setup(Func<TimbnQuestRun, bool> setup)
    {
        SetupOf = setup;
        return this;
    }

    /// <summary>
    /// Plays the quest again and again. Once its last stage is done, Core puts every stage back to not started and drops
    /// it from the quest tree and log until it starts again, so it only ever takes one entry. With a
    /// <see cref="Setup"/> the next play waits for the next NewRun. Without one, the quest is offered again the usual way.
    /// No other quest can use OfferAfter or ContinuesFrom on a repeatable quest.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Repeatable()
    {
        Repeats = true;
        return this;
    }

    /// <summary>
    /// Cancels a run that has gone this many game days without being finished, whether or not the player took the
    /// quest on. The quest goes back to not started and leaves the log, ready for the next NewRun.
    /// </summary>
    /// <param name="days">Game days from the NewRun, at least 1.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder ExpiresAfter(int days)
    {
        if (days < 1)
            throw new ArgumentOutOfRangeException(nameof(days), $"Quest {Id} expires after {days} days, but it has to be at least 1.");

        ExpireDays = days;
        return this;
    }

    /// <summary>
    /// Runs your code when a run is canceled, by <see cref="ExpiresAfter"/>, the quest's Cancel or anything else, before
    /// the quest goes back to not started, such as taking back something the player was handed. It gets the run, or null
    /// for a quest without a Setup, and the quest's Status still says whether the player had taken it on. A throw is
    /// logged.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .OnCancel(run =>
    /// {
    ///     if (parcel.Status == QuestStatus.InProgress && run is not null)
    ///         TimbnItems.TakeFromPlayer(run.Item("parcel"), 1);
    /// })
    /// ]]></code>
    /// </example>
    /// <param name="action">The code to run.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OnCancel(Action<TimbnQuestRun?> action)
    {
        WhenCanceled += action;
        return this;
    }

    /// <summary>
    /// What has to be true before the NPC offers the stage's Talk, checked but never taken, such as reputation with that
    /// NPC or the day of the week. When the NPC has a menu of its own, the talk's answer shows there with a lock for
    /// each until they are met.
    /// </summary>
    /// <param name="needs">Adds the needs.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Needs(Action<TimbnNeeds> needs)
    {
        needs(new TimbnNeeds(NeedsOf));
        _needs.Add((add, _) => needs(add));
        return this;
    }

    /// <summary>Needs read from the quest's run, such as having the item the villager asked for.</summary>
    /// <param name="needs">Adds the needs.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Needs(Action<TimbnNeeds, TimbnQuestRun> needs)
    {
        _needs.Add((add, run) => needs(add, run!));
        HasRunNeeds = true;
        return this;
    }

    /// <summary>
    /// What the player pays to finish the stage, taken when they pick the talk's Accept answer, which shows it as the
    /// game's price and is grayed out until they can pay. The NPC only shows a speech bubble once they can.
    /// </summary>
    /// <param name="cost">Adds the cost.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Cost(Action<TimbnCost> cost)
    {
        cost(new TimbnCost(CostOf));
        _costs.Add((add, _) => cost(add));
        return this;
    }

    /// <summary>A cost read from the quest's run, such as the dish the villager asked for.</summary>
    /// <example>
    /// <code><![CDATA[
    /// .Cost((cost, run) => cost.Item(run.Item("dish")))
    /// ]]></code>
    /// </example>
    /// <param name="cost">Adds the cost.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Cost(Action<TimbnCost, TimbnQuestRun> cost)
    {
        _costs.Add((add, run) => cost(add, run!));
        HasRunCost = true;
        return this;
    }

    /// <summary>
    /// The conversation that finishes the stage, with the NPC from <see cref="TalksWith(string)"/> or the quest giver. Its Accept
    /// answer takes the stage's <see cref="Cost(Action{TimbnCost})"/>, gives its <see cref="Reward(Action{TimbnRewards})"/> and completes the stage. Accept with no
    /// text completes the stage once the lines are done, for something the NPC simply hands over. See
    /// <see cref="TimbnQuestTalk"/>.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .Reward(reward => reward.Item("grape_wine:3"))
    /// .Talk(talk => talk
    ///     .PlayerSay("I'm here for my wine.")
    ///     .Say("Found it, here you go.")
    ///     .Accept())
    /// ]]></code>
    /// </example>
    /// <param name="talk">Scripts the talk. It needs an Accept.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Talk(Action<TimbnQuestTalk> talk)
    {
        if (TalkOf is not null)
            throw new InvalidOperationException($"Quest {Id} has two Talk conversations.");

        TalkOf = new TimbnQuestTalk($"{Id}_talk", Texts);
        talk(TalkOf);
        return this;
    }

    /// <summary>
    /// What the player gets when the stage is done. A Talk's Accept answer shows it, and the game pays it when the
    /// answer is picked. Otherwise Core pays it when the stage completes.
    /// </summary>
    /// <param name="rewards">Adds the rewards.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Reward(Action<TimbnRewards> rewards)
    {
        rewards(RewardsOf);
        _rewards.Add((add, _) => rewards(add));
        return this;
    }

    /// <summary>A reward read from the quest's run, such as the sale price of the dish the villager asked for.</summary>
    /// <example>
    /// <code><![CDATA[
    /// .Reward((reward, run) => reward.Money(TimbnItems.SalePrice(run.Item("dish"))))
    /// ]]></code>
    /// </example>
    /// <param name="rewards">Adds the rewards.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder Reward(Action<TimbnRewards, TimbnQuestRun> rewards)
    {
        _rewards.Add((add, run) => rewards(add, run!));
        HasRunRewards = true;
        return this;
    }

    internal TimbnWants NeedsFor(TimbnQuestRun? run)
    {
        if (!HasRunNeeds || run is null)
            return NeedsOf;

        var wants = new TimbnWants();
        foreach (var add in _needs)
            add(new TimbnNeeds(wants), run);

        return wants;
    }

    internal TimbnWants CostFor(TimbnQuestRun? run)
    {
        if (!HasRunCost || run is null)
            return CostOf;

        var wants = new TimbnWants();
        foreach (var add in _costs)
            add(new TimbnCost(wants), run);

        return wants;
    }

    internal TimbnRewards RewardsFor(TimbnQuestRun? run)
    {
        if (!HasRunRewards || run is null)
            return RewardsOf;

        var rewards = new TimbnRewards();
        foreach (var add in _rewards)
            add(rewards, run);

        return rewards;
    }

    internal bool ReadsRun => GiverOf is not null || TalkNpcOf is not null || IconOf is not null || HasRunNeeds || HasRunCost || HasRunRewards;

    /// <summary>The quest also waits for your code to call its MarkReady, such as when the player reaches a place.</summary>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder ReadyByCode()
    {
        WaitsForCode = true;
        return this;
    }

    /// <summary>The quest also waits for something the player does in the game, such as sleeping or crafting.</summary>
    /// <param name="gameEvent">The event.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder ReadyOn(TimbnQuestEvent gameEvent)
    {
        WaitsForEvent = gameEvent;
        return this;
    }

    /// <summary>
    /// Lines that play once a stage with no Talk is done, such as the player thinking aloud on arriving somewhere. Use
    /// PlayerSay for those. A Say line comes from the quest giver wherever they stand. A stage with a Talk puts its lines
    /// after the Accept answer instead.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .ReadyOn(TimbnQuestEvent.EnteredWorldZone("vineyard_basement"))
    /// .DoneLines(then => then.PlayerSay("This would be a good place to make wine."))
    /// ]]></code>
    /// </example>
    /// <param name="lines">Adds the lines.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder DoneLines(Action<TimbnLines> lines)
    {
        if (ThenLines is not null)
            throw new InvalidOperationException($"Quest {Id} has two DoneLines.");

        ThenLines = new TimbnLines(new TimbnScript($"{Id}_done", Texts));
        lines(ThenLines);
        return this;
    }

    /// <summary>
    /// Runs your code once the stage is done, after the lines that follow the Accept answer, or as soon as they stop if
    /// they end early. A throw is logged.
    /// </summary>
    /// <param name="action">The code to run.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OnDone(Action action)
    {
        WhenDone += action;
        return this;
    }

    /// <summary>
    /// Runs your code when the quest starts and again whenever a save loads with it in progress. Use it for effects that
    /// are not saved, such as a marker or a hold on the player, and undo them in <see cref="OnEnd"/>. For a change that
    /// is saved, use <see cref="OnStartExpression"/>. A throw is logged.
    /// </summary>
    /// <param name="action">The code to run.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OnStart(Action action)
    {
        WhenStarted += action;
        return this;
    }

    /// <summary>
    /// Runs your code when the quest stops being in progress, whether it is done, reset, the player leaves the save, or
    /// the plugin unloads, to undo what <see cref="OnStart"/> did. A throw is logged.
    /// </summary>
    /// <param name="action">The code to run.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OnEnd(Action action)
    {
        WhenEnded += action;
        return this;
    }

    /// <summary>
    /// One of the game's script expressions, run once by the game the moment the quest starts, such as
    /// <c>AddPerk("perk_x")</c> or <c>$money += 50</c>. Use it for changes that are saved, since the save keeps the
    /// result. The game also replays a <c>RevealTech("...")</c> here on load. Call it again to add more. A typo fails
    /// when the quest is added.
    /// </summary>
    /// <param name="expression">The game expression.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OnStartExpression(string expression)
    {
        StartExpressions.Add(new TimbnExpression(expression));
        return this;
    }

    /// <summary>
    /// One of the game's script expressions, run once by the game the moment the quest is completed, such as
    /// <c>RevealTech("tech_x")</c>. See <see cref="OnStartExpression"/>.
    /// </summary>
    /// <param name="expression">The game expression.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder OnDoneExpression(string expression)
    {
        DoneExpressions.Add(new TimbnExpression(expression));
        return this;
    }

    /// <summary>
    /// Adds the next stage of the quest, written in the same place. Core makes each stage its own game quest, continuing
    /// the one before in the same spot in the quest tree, with the game's own start and complete popups, and each starts
    /// by itself once the stage before is done. When the quest itself has stage parts (a Description, a Talk, a ReadyOn
    /// and so on), it is the first stage and this one the next. Otherwise the first AddStage is the first stage and keeps
    /// the quest's id, and the quest's offer, OfferWhen, StartsOn, OfferAfter and Icon belong to it. Every later stage's id is
    /// the quest id plus the stage name. The quest's DoneDescription describes the end of the last stage.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Quests.Add("my_gold_wine", TimbnGameIds.Npcs.Larry, "Larry's Gold Wine", quest => quest
    ///     .Offer(talk => talk.Say("Could you find me some gold wine?").Accept("Sure."))
    ///     .AddStage("pay", "Pay Herm for the gold wine.", stage => stage
    ///         .TalksWith(TimbnGameIds.Npcs.Herm)
    ///         .Cost(cost => cost.Money(200))
    ///         .Talk(talk => talk
    ///             .PlayerSay("Larry wants gold wine.")
    ///             .Say("That'll be 2 silver.")
    ///             .Accept("Here's the silver.")
    ///             .Decline("Not now.")))
    ///     .AddStage("deliver", "Bring Larry the gold wine.", stage => stage
    ///         .Cost(cost => cost.Item("grape_wine:3"))
    ///         .Talk(talk => talk.Accept("Here's your wine.")))
    ///     .DoneDescription("Larry got his gold wine."));
    /// ]]></code>
    /// </example>
    /// <param name="name">
    /// A short name of lowercase letters, digits and underscores. It is part of the stage's id, so keep it stable.
    /// </param>
    /// <param name="description">The quest description during this stage.</param>
    /// <param name="stage">Sets up the stage.</param>
    /// <returns>This builder, for chaining.</returns>
    public TimbnQuestBuilder AddStage(string name, string description, Action<TimbnQuestStage> stage)
    {
        if (!_stageName.IsMatch(name))
            throw new ArgumentException($"Stage name '{name}' on quest {Id} can only hold lowercase letters, digits and underscores, since it becomes part of the stage's id.");

        if (_stages.Exists(s => s.Name == name))
            throw new ArgumentException($"Quest {Id} has two stages named '{name}'.");

        _stages.Add((name, description, stage));
        return this;
    }

    internal void Finish()
    {
        if (GiverId.Length == 0 && GiverOf is null)
            throw new ArgumentException($"Quest {Id} needs an NPC to give it. Pass one to Quests.Add, or call Giver to read one from the run.");

        Steps.Clear();
        StageNames.Clear();
        var questIsFirst = _stages.Count == 0 || HasStepParts;
        List<(string Name, TimbnQuestBuilder Step)> steps = [];
        if (questIsFirst)
            steps.Add((Id, this));

        foreach ((var name, var description, var build) in _stages)
        {
            var step = steps.Count == 0 ? new TimbnQuestBuilder(Id, GiverId, Texts) : new TimbnQuestBuilder($"{Id}_{name}", GiverId, Texts[Id]);
            step.GiverOf = GiverOf;
            step.IconOf ??= IconOf;
            if (steps.Count > 0)
                step.ContinuesId(steps[^1].Step.Id);

            step.Description(description);
            build(new TimbnQuestStage(step));
            steps.Add((name, step));
        }

        if (!questIsFirst)
        {
            var first = steps[0].Step;
            if (first.IconId.Length == 0)
                first.IconId = IconId;

            if (Continued is not null)
                first.Continued = Continued;

            first.Parents.InsertRange(0, Parents);
            first.OfferCondition = OfferCondition;
            first.OfferTalk = OfferTalk;
            first.StartEvent = StartEvent;
        }

        var last = steps[^1].Step;
        if (last != this && _done is not null)
        {
            last._done ??= _done;
            _done = null;
        }

        Dictionary<string, string> madeBy = [];
        foreach ((var name, var step) in steps)
        {
            step.FinishStep();
            foreach (var key in step.Texts.Keys)
            {
                if (!madeBy.TryAdd(key, name))
                    throw new ArgumentException($"Stage '{name}' of quest {Id} makes the text key '{key}', which '{madeBy[key]}' already makes. Give stage '{name}' another name.");
            }

            Steps.Add(step);
            if (_stages.Count > 0)
                StageNames.Add(name);
        }

        if (SetupOf is not null)
            return;

        if (ExpireDays > 0)
            throw new ArgumentException($"Quest {Id} expires, but it has no Setup, so it never has a run to expire.");

        if (Steps.Exists(step => step.ReadsRun))
            throw new ArgumentException($"Quest {Id} reads its run, but it has no Setup to fill one.");

        if (Steps.SelectMany(step => step.Texts.Values).Select(text => Placeholder.Match(text)).FirstOrDefault(match => match.Success) is { } placeholder)
            throw new ArgumentException($"Quest {Id} shows {placeholder.Value} in its text, but it has no Setup to fill it. Add a Setup that sets '{placeholder.Groups[1].Value}'.");
    }

    private bool HasStepParts =>
        _description.Length > 0 || _descriptionInFile || RingPhase != 0 || !NeedsOf.IsEmpty || !CostOf.IsEmpty || !RewardsOf.IsEmpty || WaitsForCode || WaitsForEvent is not null
        || HasRunNeeds || HasRunCost || HasRunRewards || TalkNpcOf is not null
        || TalkOf is not null || TalkNpc.Length > 0 || ThenLines is not null || WhenDone is not null
        || WhenStarted is not null || WhenEnded is not null || StartExpressions.Count > 0 || DoneExpressions.Count > 0;

    private void FinishStep()
    {
        if (string.IsNullOrEmpty(TalkNpc))
            TalkNpc = GiverId;

        if (OfferTalk is { AcceptKey: null })
            throw new ArgumentException($"The offer of quest {Id} needs an Accept answer.");

        if (WaitsForCode && WaitsForEvent is not null)
            throw new ArgumentException($"Quest {Id} waits for both code and an event. Pick one, or split it into two quests.");

        if (TalkOf is null)
        {
            if (!NeedsOf.IsEmpty || HasRunNeeds)
                throw new ArgumentException($"Quest {Id} has Needs but no Talk for them to gate.");

            if (!CostOf.IsEmpty || HasRunCost)
                throw new ArgumentException($"Quest {Id} has a Cost but no Talk with an Accept answer to take it.");

            if (!NeedsMark)
                throw new ArgumentException($"Quest {Id} can never finish. Give it a Talk, ReadyByCode or ReadyOn.");
        }
        else if (ThenLines is not null)
        {
            throw new ArgumentException($"Quest {Id} has a Talk, so its last lines go in the Talk, not in DoneLines.");
        }
        else if (!TalkOf.HasAccept)
        {
            throw new ArgumentException($"The talk of quest {Id} needs an Accept. Use Accept() with no text to finish the step once the lines are done.");
        }
        else if (TalkOf.AcceptKey is null)
        {
            if (TalkOf.Script.Steps.Count == 0)
                throw new ArgumentException($"The talk of quest {Id} has no lines before its Accept() with no text, so nothing would happen.");

            if (TalkOf.DeclineKey is not null || TalkOf.ResumeKey is not null)
                throw new ArgumentException($"The talk of quest {Id} finishes with no answer, so it can't have Decline or Resume.");

            if (!CostOf.IsEmpty || HasRunCost)
                throw new ArgumentException($"Quest {Id} has a Cost, so its talk needs an Accept answer the player picks.");
        }

        var open = $"quest_open_{Id}_d";
        var closed = $"quest_closed_{Id}_d";
        if (_descriptionInFile)
        {
            Texts[open] = TimbnLocale.FileText(open);
            Texts[closed] = _done ?? TimbnLocale.FileTextOrNull(closed) ?? Texts[open];
            return;
        }

        Texts[open] = _description;
        Texts[closed] = _done ?? _description;
    }

    internal void SetName(string name) => Texts[Id] = name;
}
