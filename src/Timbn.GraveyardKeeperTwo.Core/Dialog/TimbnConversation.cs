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
    private readonly List<Step> _steps = [];
    private readonly string _talkId;

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
    public TimbnConversation Say(string key)
    {
        _steps.Add(new Step(StepKind.Say) { Key = key });
        return this;
    }

    /// <summary>Adds a line in the player's speech bubble. The next step waits until the line has been shown.</summary>
    /// <param name="key">The line's localisation key.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation PlayerSay(string key)
    {
        _steps.Add(new Step(StepKind.PlayerSay) { Key = key });
        return this;
    }

    /// <summary>
    /// Adds a step that runs your code, such as starting a quest when the player agrees to it. It runs at that
    /// point in the conversation and the next step follows straight away. A throw is logged and ends the
    /// conversation.
    /// </summary>
    /// <param name="action">The code to run.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Do(Action action)
    {
        _steps.Add(new Step(StepKind.Do) { Action = action });
        return this;
    }

    /// <summary>
    /// Shows answer choices over the player and waits for one to be picked. Follow it with <see cref="If"/> to
    /// say what each answer leads to. An answer that is a quest's FinishOnAnswer id shows the quest's price and
    /// reward, is greyed out until the player can pay, and hands the quest in when picked. Since the player
    /// cannot walk off, Core adds a "leave" answer that ends the conversation whenever every answer you give is
    /// one of those quest answers, so a player who cannot pay is never stuck.
    /// </summary>
    /// <param name="answers">The answers' localisation keys, in the order they are shown.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation Ask(params string[] answers)
    {
        if (answers.Length == 0)
            throw new ArgumentException("Ask needs at least one answer.", nameof(answers));

        _steps.Add(new Step(StepKind.Ask) { Answers = answers });
        return this;
    }

    /// <summary>
    /// Says what happens when the player picks an answer from the <see cref="Ask"/> just before it. The branch
    /// plays, then the conversation carries on with whatever steps follow. An answer with no If just carries on.
    /// </summary>
    /// <param name="answer">One of the answer keys passed to Ask.</param>
    /// <param name="then">Adds the branch's steps to the conversation it is given.</param>
    /// <returns>This conversation, for chaining.</returns>
    public TimbnConversation If(string answer, Action<TimbnConversation> then)
    {
        if (_steps.Count == 0 || _steps[^1].Kind != StepKind.Ask)
            throw new InvalidOperationException("If has to follow Ask.");

        var branch = new TimbnConversation(Npc, _talkId);
        then(branch);
        _steps[^1].Branches[answer] = branch;
        return this;
    }

    internal void Run(Action done) => RunFrom(0, done, done);

    private void RunFrom(int index, Action next, Action end)
    {
        if (index >= _steps.Count)
        {
            next();
            return;
        }

        var step = _steps[index];
        void carryOn() => RunFrom(index + 1, next, end);
        switch (step.Kind)
        {
            case StepKind.Say:
                if (!TimbnDialog.Say(Npc, step.Key, carryOn))
                    end();
                break;
            case StepKind.PlayerSay:
                if (!TimbnDialog.PlayerSay(step.Key, carryOn))
                    end();
                break;
            case StepKind.Do:
                try
                {
                    step.Action!();
                }
                catch (Exception ex)
                {
                    TimbnCorePlugin.Logger.LogError($"{nameof(TimbnConversation)}|A step in talk '{_talkId}' threw: {ex}");
                    end();
                    return;
                }

                carryOn();
                break;
            case StepKind.Ask:
                var asked = TimbnDialog.Ask(
                    Npc,
                    step.Answers,
                    chosen =>
                    {
                        if (step.Branches.TryGetValue(chosen, out var branch))
                            branch.RunFrom(0, carryOn, end);
                        else
                            carryOn();
                    },
                    end);
                if (!asked)
                    end();
                break;
        }
    }

    private enum StepKind
    {
        Say,
        PlayerSay,
        Do,
        Ask,
    }

    private sealed class Step(StepKind kind)
    {
        public StepKind Kind { get; } = kind;

        public string Key { get; set; } = "";

        public Action? Action { get; set; }

        public string[] Answers { get; set; } = [];

        public Dictionary<string, TimbnConversation> Branches { get; } = [];
    }
}
