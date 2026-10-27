namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds the steps every conversation shares. Talks added with Dialog.AddTalk and quest conversations both use them.
/// Each line is the text itself. Core turns it into a localization key named after the talk or quest and numbered in
/// the order written, so a lang file can translate it. <see cref="Named"/> gives a line a name of its own instead.
/// <see cref="Line"/> leaves the text out of code and reads it from the mod's lang files by name. The steps play in
/// order, each waiting for the one before.
/// </summary>
/// <typeparam name="TSelf">The conversation type, so each step chains on the same type.</typeparam>
public abstract class TimbnTalk<TSelf>
    where TSelf : TimbnTalk<TSelf>
{
    private bool _answered;
    private TimbnLineKey? _lastLine;

    private protected TimbnTalk(TimbnScript script)
    {
        Script = script;
    }

    internal TimbnScript Script { get; }

    internal TimbnLineKey? Opening { get; private set; }

    /// <summary>
    /// A line in the NPC's speech bubble. The text can hold the game's placeholders %1, %2 and so on, filled from
    /// <paramref name="values"/> in order. A value that is a <c>Func&lt;object&gt;</c> is worked out when the line
    /// shows, so it can reflect what an earlier step changed.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Func<object> carried = () => TimbnPlayer.CountItem("flitch");
    /// talk.Say("You have %1 boards.", carried);
    /// ]]></code>
    /// </example>
    /// <param name="text">What the NPC says.</param>
    /// <param name="values">The values for %1, %2 and so on.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf Say(string text, params object[] values)
    {
        var key = Script.Line(text);
        return Step(c => c.Say(key.Value, values), key);
    }

    /// <summary>
    /// A line in the NPC's speech bubble with its text only in the mod's lang files. Its key is the talk or quest id
    /// plus this name, so Line("ask") in the offer of quest my_craving reads my_craving_offer_ask. Values fill %1, %2
    /// and so on the same way as <see cref="Say"/>. A name with no line in lang/en.txt fails when the talk or quest is
    /// added.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// talk.Line("ask").Accept(then => then.Line("thanks")).Decline()
    /// ]]></code>
    /// </example>
    /// <param name="name">A short name of lowercase letters, digits and underscores, starting with a letter.</param>
    /// <param name="values">The values for %1, %2 and so on.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf Line(string name, params object[] values)
    {
        var key = Script.FileLine(name);
        return Step(c => c.Say(key.Value, values));
    }

    /// <summary>A line spoken by another NPC, skipped with a warning when that NPC is not in the world.</summary>
    /// <param name="npcId">The other NPC, from <see cref="TimbnGameIds.Npcs"/>, such as TimbnGameIds.Npcs.Herm.</param>
    /// <param name="text">What the other NPC says.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf SayAs(string npcId, string text)
    {
        var key = Script.Line(text);
        return Step(c => c.SayAs(npcId, key.Value), key);
    }

    /// <summary>A line in the player's speech bubble, with values filled in the same way as <see cref="Say"/>.</summary>
    /// <param name="text">What the player says.</param>
    /// <param name="values">The values for %1, %2 and so on.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf PlayerSay(string text, params object[] values)
    {
        var key = Script.Line(text);
        if (Script.Steps.Count == 0)
            Opening = key;

        return Step(c => c.PlayerSay(key.Value, values), key);
    }

    /// <summary>
    /// Names the line just before it, so its localization key is the talk or quest id plus this name rather than a
    /// number. Translators then see what the line is. Adding lines elsewhere never moves its key. It has to come
    /// right after a Say, PlayerSay or SayAs line.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// talk.Say("Got any boards?").Named("ask")
    /// ]]></code>
    /// </example>
    /// <param name="name">A short name of lowercase letters, digits and underscores, starting with a letter.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf Named(string name)
    {
        if (_lastLine is null)
            throw new InvalidOperationException("Named names the Say, PlayerSay or SayAs line right before it.");

        Script.Rename(_lastLine, name);
        _lastLine = null;
        return (TSelf)this;
    }

    /// <summary>Runs your code at this point in the conversation. A throw is logged and ends the conversation.</summary>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf RunCode(Action action) => Step(c => c.Do(action));

    /// <summary>
    /// Runs one of the game's script expressions at this point in the conversation, such as <c>AddRep("npc_larry", 5)</c>
    /// or <c>$money += 50</c>, like the game's own conversations do. It runs against the NPC being talked to, so
    /// <c>@name</c> reads a value on them. A typo fails when the talk or quest is added.
    /// </summary>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf RunExpression(string expression)
    {
        var run = new TimbnExpression(expression);
        return Step(c => c.Do(() => run.RunOn(c.Npc)));
    }

    /// <summary>
    /// Plays one set of lines or another, picked by one of the game's script expressions when the conversation
    /// reaches it, such as <c>$money &gt;= 500</c>. It runs against the NPC being talked to. See
    /// <see cref="When(Func{bool}, Action{TimbnLines}, Action{TimbnLines})"/>.
    /// </summary>
    /// <param name="expression">The game expression, true for <paramref name="then"/>.</param>
    /// <param name="then">The lines when it is true.</param>
    /// <param name="otherwise">The lines when it is false. Leave it out to play nothing.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf When(string expression, Action<TimbnLines> then, Action<TimbnLines>? otherwise = null)
    {
        var condition = new TimbnExpression(expression);
        var yes = new TimbnLines(Script.Nested());
        then(yes);
        TimbnLines? no = null;
        if (otherwise is not null)
        {
            no = new TimbnLines(Script.Nested());
            otherwise(no);
        }

        return Step(c => c.When(() => condition.IsTrueFor(c.Npc), t => yes.Script.Replay(t), no is null ? null : o => no.Script.Replay(o)));
    }

    /// <summary>Gives the player an item. Anything that does not fit drops on the ground by the player.</summary>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many to give.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf Give(string itemId, int count = 1) => Step(c => c.Give(itemId, count));

    /// <summary>
    /// Takes an item from the player's inventory and bags, as many as they have up to the count. Check first with
    /// <see cref="When(Func{bool}, Action{TimbnLines}, Action{TimbnLines})"/> and TimbnPlayer.CountItem when the player has to have enough.
    /// </summary>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many to take.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf Take(string itemId, int count = 1) => Step(c => c.Take(itemId, count));

    /// <summary>A pause before the next step, in real seconds.</summary>
    /// <param name="seconds">How long to pause.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf WaitRealSeconds(float seconds) => Step(c => c.Wait(seconds));

    /// <summary>Shows the game's black story bars until the conversation ends. Put it first to have them for the whole talk.</summary>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf Cinematic() => Step(c => c.Cinematic());

    /// <summary>Plays one set of lines or another, picked by the game's state when the conversation reaches it.</summary>
    /// <example>
    /// <code><![CDATA[
    /// talk.When(() => TimbnClock.TimeOfDay > 0.75f,
    ///     then => then.Say("Bit late for visitors."),
    ///     otherwise => otherwise.Say("Lovely morning."))
    /// ]]></code>
    /// </example>
    /// <param name="condition">Whether <paramref name="then"/> plays.</param>
    /// <param name="then">The lines when the condition is true.</param>
    /// <param name="otherwise">The lines when it is false. Leave it out to play nothing.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf When(Func<bool> condition, Action<TimbnLines> then, Action<TimbnLines>? otherwise = null)
    {
        var yes = new TimbnLines(Script.Nested());
        then(yes);
        TimbnLines? no = null;
        if (otherwise is not null)
        {
            no = new TimbnLines(Script.Nested());
            otherwise(no);
        }

        return Step(c => c.When(condition, t => yes.Script.Replay(t), no is null ? null : o => no.Script.Replay(o)));
    }

    /// <summary>
    /// Shows answer choices over the player and waits for one. The picked answer's lines play, then the
    /// conversation carries on with whatever steps follow. An answer with needs or a cost is grayed out with the
    /// game's locks and prices until the player has them. The player cannot walk off, so when every answer has a need,
    /// a cost or a reward, Core adds the game's leave answer.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// talk.Say("Got any boards?")
    ///     .Ask(ask => ask
    ///         .Answer("Here, take two.", then => then.Say("Thanks!"), cost: cost => cost.Item("flitch", 2), reward: reward => reward.Money(5))
    ///         .Answer("Not today."))
    /// ]]></code>
    /// </example>
    /// <returns>This conversation, for chaining.</returns>
    public TSelf Ask(Action<TimbnAsk> answers)
    {
        var ask = new TimbnAsk(Script);
        answers(ask);
        if (ask.IsEmpty)
            throw new ArgumentException("Ask needs at least one answer.", nameof(answers));

        return Step(ask.AddTo);
    }

    private protected void MarkAnswered() => _answered = true;

    private protected TimbnLines Lines(string prefix, Action<TimbnLines>? script)
    {
        var lines = new TimbnLines(Script.Child(prefix));
        script?.Invoke(lines);
        return lines;
    }

    private TSelf Step(Action<TimbnConversation> step, TimbnLineKey? line = null)
    {
        if (_answered)
            throw new InvalidOperationException("Lines go before the answers. Put what follows an answer in that answer's then.");

        Script.Steps.Add(step);
        _lastLine = line;
        return (TSelf)this;
    }
}

/// <summary>Lines of a conversation, such as a talk added with Dialog.AddTalk or what follows an answer.</summary>
public sealed class TimbnLines : TimbnTalk<TimbnLines>
{
    internal TimbnLines(TimbnScript script)
        : base(script)
    {
    }
}

/// <summary>The answers of one Ask, each with the lines that follow it.</summary>
public sealed class TimbnAsk
{
    private readonly TimbnScript _script;
    private readonly List<Choice> _choices = [];

    internal TimbnAsk(TimbnScript script)
    {
        _script = script;
    }

    internal bool IsEmpty => _choices.Count == 0;

    /// <summary>One answer the player can pick.</summary>
    /// <param name="text">The answer as the player sees it.</param>
    /// <param name="then">What follows it. Leave it out to carry straight on.</param>
    /// <param name="needs">
    /// What has to be true to pick it, checked but not taken, such as reputation with the NPC the player is talking to.
    /// The answer shows a lock for each and is grayed out until they are all met.
    /// </param>
    /// <param name="cost">What the player pays when they pick it. The answer shows the price and is grayed out until they can pay.</param>
    /// <param name="reward">
    /// What the player gets for picking it. The game pays money, items and resources when it is picked, and Core
    /// pays reputation with the NPC the player is talking to.
    /// </param>
    /// <param name="key">
    /// A name for the answer's localization key, such as accept, in place of a number. See <see cref="TimbnTalk{TSelf}.Named"/>.
    /// </param>
    /// <returns>These answers, for chaining.</returns>
    public TimbnAsk Answer(string text, Action<TimbnLines>? then = null, Action<TimbnNeeds>? needs = null, Action<TimbnCost>? cost = null, Action<TimbnRewards>? reward = null, string? key = null)
    {
        var lineKey = key is null ? _script.Line(text) : _script.NamedLine(key, text);
        TimbnLines? lines = null;
        if (then is not null)
        {
            lines = new TimbnLines(_script.Nested());
            then(lines);
        }

        var wanted = TimbnWants.From(needs, cost);
        TimbnRewards? given = null;
        if (reward is not null)
        {
            given = new TimbnRewards();
            reward(given);
        }

        _choices.Add(new Choice(lineKey, lines, wanted, given));
        return this;
    }

    internal void AddTo(TimbnConversation conversation)
    {
        var reputation = TimbnNpcs.ReputationOf(conversation.Npc.id);
        conversation.Ask([.. _choices.Select(choice => choice.Build(reputation))]);
        foreach (var choice in _choices)
        {
            conversation.If(choice.Key.Value, branch =>
            {
                if (choice.Rewards is { } rewards)
                    branch.Do(() => rewards.PayReputation(reputation));

                choice.Then?.Script.Replay(branch);
            });
        }
    }

    private sealed class Choice
    {
        public Choice(TimbnLineKey key, TimbnLines? then, TimbnWants? wants, TimbnRewards? rewards)
        {
            Key = key;
            Then = then;
            Wants = wants;
            Rewards = rewards;
        }

        public TimbnLineKey Key { get; }

        public TimbnLines? Then { get; }

        public TimbnWants? Wants { get; }

        public TimbnRewards? Rewards { get; }

        public TimbnAnswer Build(string? reputation)
        {
            var answer = new TimbnAnswer(Key.Value);
            Wants?.ApplyTo(answer, reputation);
            Rewards?.ApplyTo(answer, reputation);
            return answer;
        }
    }
}
