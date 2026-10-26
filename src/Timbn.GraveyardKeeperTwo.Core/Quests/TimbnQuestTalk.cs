namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The conversation where the NPC offers the quest. It needs an Accept answer, and picking it starts the quest.</summary>
public sealed class TimbnQuestOffer : TimbnTalk<TimbnQuestOffer>
{
    private readonly string _questId;

    internal TimbnQuestOffer(string questId, Dictionary<string, string> texts)
        : base(new TimbnScript($"{questId}_offer", texts))
    {
        _questId = questId;
    }

    internal string? AcceptKey { get; private set; }

    internal TimbnWants? AcceptWants { get; private set; }

    internal TimbnLines? AcceptThen { get; private set; }

    internal string? DeclineKey { get; private set; }

    internal TimbnLines? DeclineThen { get; private set; }

    /// <summary>
    /// The player's answer that accepts and starts the quest. With needs or a cost it is grayed out with the game's
    /// locks and prices until the player has them. A cost is taken when it is picked, and given back if the quest
    /// can't start.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .Accept("I'll sort it out.", then => then.Say("Thanks!"), needs: need => need.OnWeekday(TimbnWeekday.Pride))
    /// ]]></code>
    /// </example>
    /// <param name="text">The answer.</param>
    /// <param name="then">What the NPC says after it. Leave it out to say nothing.</param>
    /// <param name="needs">What has to be true to pick it. Reputation is with the quest giver.</param>
    /// <param name="cost">What the player pays to take the quest on.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestOffer Accept(string text, Action<TimbnLines>? then = null, Action<TimbnNeeds>? needs = null, Action<TimbnCost>? cost = null) =>
        SetAccept(key => Script.Named(key, text), then, needs, cost);

    /// <summary>
    /// The Accept answer with its text only in the plugin's lang files, under the quest id plus _accept, such as
    /// my_craving_accept. Its lines follow from then, where <see cref="TimbnTalk{TSelf}.Line"/> reads my_craving_accept
    /// plus the line's name.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .Accept(then => then.Line("thanks"))
    /// ]]></code>
    /// </example>
    /// <param name="then">What the NPC says after it.</param>
    /// <param name="needs">What has to be true to pick it. Reputation is with the quest giver.</param>
    /// <param name="cost">What the player pays to take the quest on.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestOffer Accept(Action<TimbnLines> then, Action<TimbnNeeds>? needs = null, Action<TimbnCost>? cost = null) =>
        SetAccept(Script.FileNamed, then, needs, cost);

    /// <summary>The player's answer that turns the quest down for now. The NPC offers it again later.</summary>
    /// <param name="text">The answer.</param>
    /// <param name="then">What the NPC says after it. Leave it out to say nothing.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestOffer Decline(string text, Action<TimbnLines>? then = null) => SetDecline(key => Script.Named(key, text), then);

    /// <summary>The Decline answer with its text only in the plugin's lang files, under the quest id plus _decline.</summary>
    /// <param name="then">What the NPC says after it. Leave it out to say nothing.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestOffer Decline(Action<TimbnLines>? then = null) => SetDecline(Script.FileNamed, then);

    private TimbnQuestOffer SetAccept(Func<string, string> named, Action<TimbnLines>? then, Action<TimbnNeeds>? needs, Action<TimbnCost>? cost)
    {
        if (AcceptKey is not null)
            throw new InvalidOperationException($"Quest {_questId} has two Accept answers.");

        MarkAnswered();
        var prefix = $"{_questId}_accept";
        AcceptKey = named(prefix);
        AcceptWants = TimbnWants.From(needs, cost);
        AcceptThen = Lines(prefix, then);
        return this;
    }

    private TimbnQuestOffer SetDecline(Func<string, string> named, Action<TimbnLines>? then)
    {
        if (DeclineKey is not null)
            throw new InvalidOperationException($"Quest {_questId} has two Decline answers.");

        MarkAnswered();
        var prefix = $"{_questId}_decline";
        DeclineKey = named(prefix);
        DeclineThen = Lines(prefix, then);
        return this;
    }
}

/// <summary>
/// The conversation that finishes a quest or a stage, at the NPC from its TalksWith call or the quest giver. The lines play,
/// then the player picks Accept, which takes the stage's cost, gives its reward and completes the stage, or Decline to
/// come back later. Accept with no text completes the stage as soon as the lines are done, for something the NPC
/// simply hands over. When the NPC has a menu of its own, as Herm and Larry do, the talk joins it as one answer. Its
/// text is the talk's first line when that is a PlayerSay, or the quest's name otherwise, and the rest plays once it
/// is picked. A talk that is only an Accept answer puts that answer straight into the menu, with its price and reward.
/// The NPC shows a speech bubble for the talk only once the stage's needs are met and the player can pay its cost,
/// while the answer in an NPC's own menu is there all along, so the player can still ask and decline.
/// </summary>
/// <example>
/// <code><![CDATA[
/// .Talk(talk => talk
///     .PlayerSay("Larry wants a bottle of quality wine.")
///     .Say("I can get you one, but it will cost you 2 silver.")
///     .Accept("A little steep, but ok.", then => then.Say("Come back on Pride."))
///     .Decline("I don't have the money right now.")
///     .Resume("Here to pay the 2 silver?"))
/// ]]></code>
/// </example>
public sealed class TimbnQuestTalk : TimbnTalk<TimbnQuestTalk>
{
    private readonly string _prefix;

    internal TimbnQuestTalk(string prefix, Dictionary<string, string> texts)
        : base(new TimbnScript(prefix, texts))
    {
        _prefix = prefix;
    }

    internal string? AcceptKey { get; private set; }

    internal TimbnLines? AcceptThen { get; private set; }

    internal string? DeclineKey { get; private set; }

    internal TimbnLines? DeclineThen { get; private set; }

    internal string? ResumeKey { get; private set; }

    internal bool HasAccept { get; private set; }

    internal bool IsOnlyAccept => AcceptKey is not null && Script.Steps.Count == 0 && ResumeKey is null;

    /// <summary>
    /// The player's answer that finishes the stage. It shows the stage's cost and reward, is grayed out with the game's
    /// price until the player can pay, and takes the cost and gives the reward when it is picked.
    /// </summary>
    /// <param name="text">The answer.</param>
    /// <param name="then">What follows it, such as the NPC's thanks or what to do next.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestTalk Accept(string text, Action<TimbnLines>? then = null) => SetAccept(key => Script.Named(key, text), then);

    /// <summary>
    /// The Accept answer with its text only in the plugin's lang files, under the quest id plus _talk_accept, such as
    /// my_craving_talk_accept, or the stage's id in place of the quest id. Its lines follow from then.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// .Accept(then => then.Line("thanks"))
    /// ]]></code>
    /// </example>
    /// <param name="then">What follows it, such as the NPC's thanks or what to do next.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestTalk Accept(Action<TimbnLines> then) => SetAccept(Script.FileNamed, then);

    /// <summary>
    /// Completes the stage as soon as the lines before it are done, with no answer to pick, giving the stage's reward,
    /// such as when the NPC simply hands something over. A stage with a cost needs the player to pick an Accept answer
    /// instead, and there is nothing to Decline or Resume.
    /// </summary>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestTalk Accept()
    {
        MarkAccepted();
        return this;
    }

    private void MarkAccepted()
    {
        if (HasAccept)
            throw new InvalidOperationException($"{_prefix} has two Accept answers.");

        MarkAnswered();
        HasAccept = true;
    }

    private TimbnQuestTalk SetAccept(Func<string, string> named, Action<TimbnLines>? then)
    {
        MarkAccepted();
        var prefix = $"{_prefix}_accept";
        AcceptKey = named(prefix);
        AcceptThen = Lines(prefix, then);
        return this;
    }

    /// <summary>
    /// The player's answer that leaves without finishing, to come back later. Without it, Core adds the game's leave
    /// answer when the stage has a cost or a reward, and an NPC with a menu of its own already has one.
    /// </summary>
    /// <param name="text">The answer.</param>
    /// <param name="then">What the NPC says after it. Leave it out to say nothing.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestTalk Decline(string text, Action<TimbnLines>? then = null) => SetDecline(key => Script.Named(key, text), then);

    /// <summary>The Decline answer with its text only in the plugin's lang files, under the quest id plus _talk_decline.</summary>
    /// <param name="then">What the NPC says after it. Leave it out to say nothing.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestTalk Decline(Action<TimbnLines>? then = null) => SetDecline(Script.FileNamed, then);

    private TimbnQuestTalk SetDecline(Func<string, string> named, Action<TimbnLines>? then)
    {
        if (DeclineKey is not null)
            throw new InvalidOperationException($"{_prefix} has two Decline answers.");

        MarkAnswered();
        var prefix = $"{_prefix}_decline";
        DeclineKey = named(prefix);
        DeclineThen = Lines(prefix, then);
        return this;
    }

    /// <summary>
    /// What the NPC says in place of the lines before the answers when the player comes back after hearing them once,
    /// such as after declining. The talk's opening PlayerSay still plays first.
    /// </summary>
    /// <param name="text">The NPC's line.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnQuestTalk Resume(string text)
    {
        if (ResumeKey is not null)
            throw new InvalidOperationException($"{_prefix} has two Resume lines.");

        ResumeKey = Script.Named($"{_prefix}_resume", text);
        return this;
    }
}
