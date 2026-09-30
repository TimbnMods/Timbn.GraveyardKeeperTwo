namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Scripts a conversation with an NPC as a list of steps, using the game's own speech bubbles and answer
/// choices. A talk added with the plugin's Dialog.AddTalk gets one of these, fills it in, and Core plays the
/// steps in order once the talk handler returns. Each step waits for the one before it, and the conversation
/// ends by itself when the last step finishes, so there is no done callback to remember. The player's control
/// is taken for as long as it runs, the same as the game's own dialogues, so they cannot walk off or interact
/// with the NPC again part way through. Every line and answer is a localisation key added with the plugin's
/// Text property.
/// </summary>
/// <example>
/// Larry offers a quest, starts it if the player agrees, and answers either way.
/// <code><![CDATA[
/// talk.Say("larry_offer")
///     .Ask("larry_accept", "larry_decline")
///     .If("larry_accept", then => then
///         .Do(() => TimbnQuests.Start("timbn_larry_wine"))
///         .Say("larry_accepted"))
///     .If("larry_decline", then => then.Say("larry_declined"));
/// ]]></code>
/// </example>
public sealed class TimbnConversation
{
    private readonly List<Action<Action, Action>> _steps = [];
    private readonly string _talkId;
    private Dictionary<string, TimbnConversation>? _branches;

    internal TimbnConversation(WgoData npc, string talkId)
    {
        Npc = npc;
        _talkId = talkId;
    }

    /// <summary>The NPC the player is talking to.</summary>
    public WgoData Npc { get; }

    /// <summary>Adds a line in the NPC's speech bubble. The next step waits until the line has been shown.</summary>
    /// <param name="key">The line's localisation key.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Say(string key) => Say(key, []);

    /// <summary>
    /// Adds a line in the NPC's speech bubble with values filled into its text. The text holds the game's own
    /// placeholders <c>%1</c>, <c>%2</c> and so on, filled from <paramref name="values"/> in order. A value that is
    /// a <c>Func&lt;object&gt;</c> is worked out when the line is shown rather than when the talk is scripted, so it
    /// can reflect what an earlier step changed.
    /// </summary>
    /// <example>
    /// With the text <c>You owe me %1 coins, %2.</c>
    /// <code><![CDATA[
    /// Func<object> price = () => CurrentPrice();
    /// talk.Say("larry_price", price, "friend");
    /// ]]></code>
    /// </example>
    /// <param name="key">The line's localisation key.</param>
    /// <param name="values">The values for <c>%1</c>, <c>%2</c> and so on. A number is written the way the game writes numbers.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Say(string key, params object[] values) =>
        Add((next, end) =>
        {
            if (TimbnDialog.Say(Npc, key, values, next))
                return;

            end();
        });

    /// <summary>Adds a line in the player's speech bubble. The next step waits until the line has been shown.</summary>
    /// <param name="key">The line's localisation key.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation PlayerSay(string key) => PlayerSay(key, []);

    /// <summary>Adds a line in the player's speech bubble with values filled into its text, the same way as <see cref="Say(string, object[])"/>.</summary>
    /// <param name="key">The line's localisation key.</param>
    /// <param name="values">The values for <c>%1</c>, <c>%2</c> and so on.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation PlayerSay(string key, params object[] values) =>
        Add((next, end) =>
        {
            if (TimbnDialog.PlayerSay(key, values, next))
                return;

            end();
        });

    /// <summary>
    /// Adds a step that runs your code, such as starting a quest when the player agrees to it. It runs at that
    /// point in the conversation and the next step follows straight away. A throw is logged and ends the
    /// conversation.
    /// </summary>
    /// <param name="action">The code to run.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Do(Action action) =>
        Add((next, end) =>
        {
            if (TimbnSafe.Run(action, $"{nameof(TimbnConversation)}|A step in talk '{_talkId}'"))
                next();
            else
                end();
        });

    /// <summary>
    /// Adds a step that gives the player an item, then carries on. It goes into the player's inventory and bags,
    /// and anything that does not fit drops in front of the player, so nothing is lost. See
    /// <see cref="TimbnItems.GiveToPlayer"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// talk.Say("larry_found_them").Give("flitch", 2).Say("larry_given");
    /// </code>
    /// </example>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many to give.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Give(string itemId, int count = 1) =>
        Do(() =>
        {
            var moved = TimbnItems.GiveToPlayer(new Item(itemId, count));
            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnConversation)}|Talk '{_talkId}' gave the player {count} {itemId}, {moved} into their inventory.");
        });

    /// <summary>
    /// Adds a step that takes an item from the player's inventory and bags, then carries on. It takes as many as
    /// the player has, up to the count, and the items are gone. Check first with <see cref="When"/> and
    /// <see cref="TimbnItems.CountOnPlayer"/> when the player has to have enough. See
    /// <see cref="TimbnItems.TakeFromPlayer"/>.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// talk.When(
    ///     () => TimbnItems.CountOnPlayer("flitch") >= 2,
    ///     then => then.Take("flitch", 2).Say("larry_thanks"),
    ///     otherwise => otherwise.Say("larry_need_more"));
    /// ]]></code>
    /// </example>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many to take.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Take(string itemId, int count = 1) =>
        Do(() =>
        {
            var taken = TimbnItems.TakeFromPlayer(itemId, count);
            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnConversation)}|Talk '{_talkId}' took {taken} of {count} {itemId} from the player.");
        });

    /// <summary>
    /// Adds a branch chosen by the game's state when the conversation reaches it, for example a different line
    /// when the player is carrying enough of something. The condition is checked at that point, not when the talk
    /// is scripted. The chosen branch plays, then the conversation carries on with whatever steps follow. A
    /// throw in the condition is logged and ends the conversation.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// talk.When(
    ///     () => MainGame.PlayerData.inventory.Data.GetTotalCountInInventory("grape_wine") > 0,
    ///     then => then.Say("larry_thanks_for_wine"),
    ///     otherwise => otherwise.Say("larry_still_thirsty"));
    /// ]]></code>
    /// </example>
    /// <param name="condition">Whether the branch in <paramref name="then"/> should play.</param>
    /// <param name="then">Adds the steps that play when the condition is true.</param>
    /// <param name="otherwise">Adds the steps that play when it is false. Leave it out to play nothing.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation When(Func<bool> condition, Action<TimbnConversation> then, Action<TimbnConversation>? otherwise = null)
    {
        var yes = Branch(then);
        var no = otherwise is null ? null : Branch(otherwise);
        return Add((next, end) =>
        {
            var chosen = false;
            if (!TimbnSafe.Run(() => chosen = condition(), $"{nameof(TimbnConversation)}|A condition in talk '{_talkId}'"))
            {
                end();
                return;
            }

            var branch = chosen ? yes : no;
            if (branch is null)
                next();
            else
                branch.RunFrom(0, next, end);
        });
    }

    /// <summary>
    /// Shows answer choices over the player and waits for one to be picked. Follow it with <see cref="If"/> to
    /// say what each answer leads to. An answer that is a quest's FinishOnAnswer id shows the quest's price and
    /// reward, is greyed out until the player can pay, and hands the quest in when picked. Since the player
    /// cannot walk off, Core adds a "leave" answer that ends the conversation whenever every answer you give has
    /// a price, a need, or a reward, so a player who cannot pay is never stuck.
    /// </summary>
    /// <param name="answers">
    /// The answers, in the order they are shown. A plain localisation key is an answer with no extras, and
    /// <see cref="TimbnAnswer"/> adds prices, needs, and rewards.
    /// </param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Ask(params TimbnAnswer[] answers)
    {
        if (answers.Length == 0)
            throw new ArgumentException("Ask needs at least one answer.", nameof(answers));

        var keys = answers.Select(answer => answer.Key).ToArray();
        Dictionary<string, AnswerData> extras = [];
        foreach (var answer in answers)
        {
            if (answer.ToData() is { } data)
                extras[answer.Key] = data;
        }

        Dictionary<string, TimbnConversation> branches = [];
        Add((next, end) =>
        {
            var asked = TimbnDialog.Ask(
                Npc,
                keys,
                chosen =>
                {
                    Log($"answer '{chosen}' picked.");
                    if (branches.TryGetValue(chosen, out var branch))
                        branch.RunFrom(0, next, end);
                    else
                        next();
                },
                () =>
                {
                    Log("left at the answers.");
                    end();
                },
                extras);
            if (!asked)
                end();
        });
        _branches = branches;
        return this;
    }

    /// <inheritdoc cref="Ask(TimbnAnswer[])"/>
    public TimbnConversation Ask(params string[] answers) => Ask(answers.Select(answer => new TimbnAnswer(answer)).ToArray());

    /// <summary>
    /// Says what happens when the player picks an answer from the <see cref="Ask(TimbnAnswer[])"/> just before it. The branch
    /// plays, then the conversation carries on with whatever steps follow. An answer with no If just carries on.
    /// </summary>
    /// <param name="answer">One of the answer keys passed to Ask.</param>
    /// <param name="then">Adds the branch's steps to the conversation it is given.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation If(string answer, Action<TimbnConversation> then)
    {
        if (_branches is null)
            throw new InvalidOperationException("If has to follow Ask.");

        _branches[answer] = Branch(then);
        return this;
    }

    internal void Run(Action done) => RunFrom(0, done, done);

    private void Log(string message) =>
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnConversation)}|Talk '{_talkId}' {message}");

    private TimbnConversation Add(Action<Action, Action> step)
    {
        _branches = null;
        _steps.Add(step);
        return this;
    }

    private TimbnConversation Branch(Action<TimbnConversation> script)
    {
        var branch = new TimbnConversation(Npc, _talkId);
        script(branch);
        return branch;
    }

    private void RunFrom(int index, Action next, Action end)
    {
        if (index >= _steps.Count)
        {
            next();
            return;
        }

        _steps[index](() => RunFrom(index + 1, next, end), end);
    }
}
