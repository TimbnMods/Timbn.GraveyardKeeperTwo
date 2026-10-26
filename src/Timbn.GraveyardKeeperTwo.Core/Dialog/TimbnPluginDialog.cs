namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gives NPCs conversations on behalf of a plugin, using the game's own speech bubbles and answer choices.
/// Talks are removed when the plugin unloads. Reach it through the plugin's Dialog property.
/// </summary>
public sealed class TimbnPluginDialog
{
    internal static readonly Color DefaultBubbleColor = new(1f, 0.8f, 0.35f);

    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginDialog(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// The color tinted onto the game's speech bubble over an NPC that has one of this plugin's talks or quests on
    /// offer, so players can tell a mod's bubble from the game's. The quest tree also draws the line into each of this
    /// plugin's quests in it once the quest above is done, where the game draws gold. Every plugin has its own, and
    /// the default is a warm gold close to the game's. Set it once in OnAwake. A change shows the next time a bubble
    /// or the quest tree is drawn.
    /// </summary>
    /// <example>
    /// <code>
    /// Dialog.BubbleColor = new Color(0.6f, 0.8f, 1f);
    /// </code>
    /// </example>
    public Color BubbleColor { get; set; } = DefaultBubbleColor;

    /// <summary>
    /// Gives an NPC a conversation of your own. While isOffered returns true the NPC shows a speech icon, and when
    /// the player interacts, the conversation plays in place of the NPC's own. The player's control is taken while it
    /// plays and given back when it ends. isOffered is checked about once a second. Each line is the text itself, and
    /// Core names its localization key after the talk id (talkId_1, talkId_2 and so on, in the order the lines are
    /// written), so a lang file can translate it. Add new lines at the end of a talk that has shipped, since a line put
    /// in the middle moves every key after it and a translation would show the wrong lines. The lines are recorded
    /// once, here, so pick between lines with When and fill in changing values with a <c>Func&lt;object&gt;</c>.
    /// </summary>
    /// <example>
    /// Larry asks for wine while the player is carrying some, and a tip he gives once per save in a bubble color of
    /// its own.
    /// <code><![CDATA[
    /// Dialog.AddTalk(TimbnGameIds.Npcs.Larry, "my_larry_wine", () => TimbnPlayer.CountItem("grape_wine:1") > 0, lines => lines
    ///     .Say("Is that wine you're carrying?")
    ///     .Ask(ask => ask
    ///         .Answer("Have a bottle.", then => then.Say("Lovely."), cost: cost => cost.Item("grape_wine:1"))
    ///         .Answer("It's not for you.")));
    ///
    /// Dialog.AddTalk(TimbnGameIds.Npcs.Larry, "my_larry_tip", () => true, lines => lines.Say("Herm sells nails."),
    ///     new TimbnTalkOptions { Once = true, BubbleColor = new Color(0.6f, 0.8f, 1f) });
    /// ]]></code>
    /// </example>
    /// <param name="npcId">The NPC, from <see cref="TimbnGameIds.Npcs"/>, such as TimbnGameIds.Npcs.Larry.</param>
    /// <param name="talkId">A unique id for this talk. It becomes the NPC's interaction event and the start of its text keys, and reusing an id on another NPC throws.</param>
    /// <param name="isOffered">Whether the talk is available right now. A throw counts as false and is logged.</param>
    /// <param name="lines">Writes the conversation's lines.</param>
    /// <param name="options">Optional settings, such as making the talk a one-shot or giving it its own bubble color.</param>
    /// <returns>A handle that removes the talk early when disposed. You don't need to keep it.</returns>
    public IDisposable AddTalk(string npcId, string talkId, Func<bool> isOffered, Action<TimbnLines> lines, TimbnTalkOptions? options = null)
    {
        Dictionary<string, string> texts = [];
        var written = new TimbnLines(new TimbnScript(talkId, texts));
        lines(written);
        return _owner.Subscriptions.Add(AddRecorded(
            npcId,
            talkId,
            isOffered,
            written,
            texts,
            () => options?.BubbleColor ?? BubbleColor,
            options?.Once == true ? OnceKey(talkId) : null));
    }

    /// <summary>
    /// Gives an NPC a conversation that is written each time the player talks, so plain C# can pick and fill the lines
    /// from the game's state, such as what the player carries. Everything else works the same as AddTalk. A numbered
    /// key can belong to a different line from one talk to the next, so give a Key to any line a lang file translates.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Dialog.AddDynamicTalk(TimbnGameIds.Npcs.Larry, "my_larry_boards", () => true, lines =>
    /// {
    ///     var boards = TimbnPlayer.CountItem("flitch");
    ///     if (boards == 0)
    ///         lines.Say("No boards? Go chop some trees.").Named("none");
    ///     else
    ///         lines.Say("You have %1 boards.", boards).Named("some");
    /// });
    /// ]]></code>
    /// </example>
    /// <param name="npcId">The NPC, from <see cref="TimbnGameIds.Npcs"/>, such as TimbnGameIds.Npcs.Larry.</param>
    /// <param name="talkId">A unique id for this talk. It becomes the NPC's interaction event and the start of its text keys, and reusing an id on another NPC throws.</param>
    /// <param name="isOffered">Whether the talk is available right now. A throw counts as false and is logged.</param>
    /// <param name="lines">Writes the conversation's lines, each time the player talks. A throw is logged and ends the talk.</param>
    /// <param name="options">Optional settings, such as making the talk a one-shot or giving it its own bubble color.</param>
    /// <returns>A handle that removes the talk early when disposed. You don't need to keep it.</returns>
    public IDisposable AddDynamicTalk(string npcId, string talkId, Func<bool> isOffered, Action<TimbnLines> lines, TimbnTalkOptions? options = null)
    {
        IDisposable? text = null;
        void play(TimbnConversation conversation)
        {
            Dictionary<string, string> texts = [];
            var written = new TimbnLines(new TimbnScript(talkId, texts));
            lines(written);
            text?.Dispose();
            text = TimbnLocale.Add(texts);
            written.Script.Replay(conversation);
        }

        var added = TimbnDialog.AddTalk(npcId, talkId, isOffered, play, () => options?.BubbleColor ?? BubbleColor, options?.Once == true ? OnceKey(talkId) : null);
        return _owner.Subscriptions.Add(new TimbnUndo(() =>
        {
            added.Dispose();
            text?.Dispose();
        }));
    }

    /// <summary>
    /// Adds an answer of your own to the menu an NPC or an object opens when the player interacts, next to the game's
    /// own answers and above Leave, the way a quest adds its turn in. Picking it takes its cost, plays the lines that
    /// follow, and shows the menu again. isOffered, needs and cost are read each time the menu opens, so a price can
    /// follow a setting. The text can color any part of itself, with the game's highlight in square brackets or any
    /// color in a color tag, and buttonColor gives the answer's button a color of its own.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Dialog.AddMenuAnswer(TimbnGameIds.Npcs.TavernOwner, "my_mod_hire", () => !_booked, "Play at my [tavern] tonight.",
    ///     then => then.Say("I'll bring my violin.").RunCode(() => _booked = true),
    ///     cost: cost => cost.Money(500));
    ///
    /// Dialog.AddMenuAnswer("royal_mailbox", "my_mod_burn", () => true, "<color=#ff6050>Burn the letters</color>",
    ///     buttonColor: new Color(0.45f, 0.12f, 0.1f));
    /// ]]></code>
    /// </example>
    /// <param name="targetId">The NPC, from <see cref="TimbnGameIds.Npcs"/>, or the id of an object with a menu, such as royal_mailbox.</param>
    /// <param name="answerId">A unique id for the answer. It is the answer's localization key, and the lines that follow are answerId_1, answerId_2 and so on.</param>
    /// <param name="isOffered">Whether the answer shows right now. A throw counts as false and is logged.</param>
    /// <param name="text">The answer as the player sees it.</param>
    /// <param name="then">What follows it, such as the NPC's reply and code with RunCode. Leave it out to go straight back to the menu.</param>
    /// <param name="needs">What has to be true to pick it, shown as a lock. The answer is grayed out until it is met.</param>
    /// <param name="cost">What the player pays when they pick it, shown as a price. The answer is grayed out until they can pay.</param>
    /// <param name="buttonColor">The color of the answer's button, shaded on hover like the game's own. Leave it out for the game's own.</param>
    /// <returns>A handle that removes the answer early when disposed. You don't need to keep it.</returns>
    public IDisposable AddMenuAnswer(string targetId, string answerId, Func<bool> isOffered, string text, Action<TimbnLines>? then = null, Action<TimbnNeeds>? needs = null, Action<TimbnCost>? cost = null, Color? buttonColor = null)
    {
        Dictionary<string, string> texts = [];
        var script = new TimbnScript(answerId, texts);
        var key = script.Named(answerId, text);
        var id = $"{_owner.Metadata.GUID}/{answerId}";
        var lines = new TimbnLines(script.Nested());
        then?.Invoke(lines);
        TimbnAnswer? Answer()
        {
            var offered = false;
            if (!TimbnSafe.Run(() => offered = isOffered(), _owner.PluginLogger, $"{nameof(TimbnPluginDialog)}.{nameof(AddMenuAnswer)}|{answerId}") || !offered)
                return null;

            var answer = new TimbnAnswer(key, id);
            TimbnWants.From(needs, cost)?.ApplyTo(answer, TimbnNpcs.ReputationOf(targetId));
            return answer;
        }

        var shown = TimbnLocale.Add(texts);
        var added = TimbnDialog.AddMenuAnswer(targetId, Answer, conversation => lines.Script.Replay(conversation));
        var painted = buttonColor is { } color ? TimbnAnswerColors.Add(id, color) : null;
        return _owner.Subscriptions.Add(new TimbnUndo(() =>
        {
            added.Dispose();
            shown.Dispose();
            painted?.Dispose();
        }));
    }

    internal static IDisposable AddRecorded(
        string npcId,
        string talkId,
        Func<bool> isOffered,
        TimbnLines lines,
        IReadOnlyDictionary<string, string> texts,
        Func<Color> bubbleColor,
        string? onceKey)
    {
        var text = TimbnLocale.Add(texts);
        IDisposable added;
        try
        {
            added = TimbnDialog.AddTalk(npcId, talkId, isOffered, conversation => lines.Script.Replay(conversation), bubbleColor, onceKey);
        }
        catch
        {
            text.Dispose();
            throw;
        }

        return new TimbnUndo(() =>
        {
            added.Dispose();
            text.Dispose();
        });
    }

    /// <summary>Whether the player has already started a one-shot talk in the loaded save.</summary>
    /// <param name="talkId">The talk's id, as passed to AddTalk.</param>
    /// <returns>True once the talk has been started, until it is reset.</returns>
    public bool HasSeenTalk(string talkId) => TimbnDialog.HasSeen(OnceKey(talkId));

    /// <summary>Makes a one-shot talk offered again in the loaded save, as if the player had never started it.</summary>
    /// <param name="talkId">The talk's id, as passed to AddTalk.</param>
    public void ResetTalk(string talkId) => TimbnDialog.ResetSeen(OnceKey(talkId));

    private string OnceKey(string talkId) => $"{_owner.Metadata.GUID}/{talkId}";
}
