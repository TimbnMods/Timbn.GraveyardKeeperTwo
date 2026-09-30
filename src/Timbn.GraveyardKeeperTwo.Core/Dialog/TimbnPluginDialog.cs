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
    /// offer, so players can tell a mod's bubble from the game's. Every plugin has its own, and the default is a
    /// warm gold. Set it once in OnAwake. A change reaches a bubble the next time it is drawn.
    /// </summary>
    /// <example>
    /// <code>
    /// Dialog.BubbleColor = new Color(0.6f, 0.8f, 1f);
    /// </code>
    /// </example>
    public Color BubbleColor { get; set; } = DefaultBubbleColor;

    /// <summary>
    /// Gives an NPC a conversation of your own. While isOffered returns true the NPC shows a speech icon, and
    /// when the player interacts, your talk handler scripts the conversation in place of the NPC's own. The
    /// player's control is taken while it plays and given back when it ends, which it does by itself. The icon
    /// stays hidden for the same stretch and comes back afterwards. isOffered is checked about once a second.
    /// </summary>
    /// <example>
    /// Larry asks for wine while the player is carrying enough of it.
    /// <code><![CDATA[
    /// Dialog.AddTalk("npc_larry", "my_larry_talk",
    ///     () => TimbnQuests.CanFinish("timbn_larry_wine"),
    ///     talk => talk
    ///         .Say("larry_remind")
    ///         .Ask("larry_give", "larry_later")
    ///         .If("larry_give", then => then.Say("larry_thanks")));
    /// ]]></code>
    /// </example>
    /// <param name="npcId">The NPC's world object id, such as npc_larry.</param>
    /// <param name="eventId">A unique id for this talk. It becomes the NPC's interaction event, and reusing an id on another NPC throws.</param>
    /// <param name="isOffered">Whether the talk is available right now. A throw counts as false and is logged.</param>
    /// <param name="talk">
    /// Scripts the conversation by adding steps to the <see cref="TimbnConversation"/> it is given. Adding no
    /// steps ends the conversation straight away.
    /// </param>
    /// <returns>A handle that removes the talk early when disposed. You don't need to keep it.</returns>
    public IDisposable AddTalk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> talk) =>
        AddTalk(npcId, eventId, isOffered, talk, null);

    /// <inheritdoc cref="AddTalk(string, string, Func{bool}, Action{TimbnConversation})"/>
    /// <example>
    /// A tip that Larry gives once per save, in a bubble color of its own.
    /// <code><![CDATA[
    /// Dialog.AddTalk("npc_larry", "my_tip_talk", () => true,
    ///     talk => talk.Say("larry_tip"),
    ///     new TimbnTalkOptions { Once = true, BubbleColor = new Color(0.6f, 0.8f, 1f) });
    /// ]]></code>
    /// </example>
    /// <param name="npcId">The NPC's world object id, such as npc_larry.</param>
    /// <param name="eventId">A unique id for this talk. It becomes the NPC's interaction event, and reusing an id on another NPC throws.</param>
    /// <param name="isOffered">Whether the talk is available right now. A throw counts as false and is logged.</param>
    /// <param name="talk">Scripts the conversation by adding steps to the <see cref="TimbnConversation"/> it is given.</param>
    /// <param name="options">Optional settings, such as making the talk a one-shot or giving it its own bubble color.</param>
    public IDisposable AddTalk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> talk, TimbnTalkOptions? options) =>
        _owner.Subscriptions.Add(TimbnDialog.AddTalk(
            npcId,
            eventId,
            isOffered,
            talk,
            () => options?.BubbleColor ?? BubbleColor,
            options?.Once == true ? OnceKey(eventId) : null));

    /// <summary>Whether the player has already started a one-shot talk in the loaded save.</summary>
    /// <param name="eventId">The talk's id, as passed to AddTalk.</param>
    /// <returns>True once the talk has been started, until it is reset.</returns>
    public bool HasSeenOnceTalk(string eventId) => TimbnDialog.HasSeen(OnceKey(eventId));

    /// <summary>Makes a one-shot talk offered again in the loaded save, as if the player had never started it.</summary>
    /// <param name="eventId">The talk's id, as passed to AddTalk.</param>
    public void ResetOnceTalk(string eventId) => TimbnDialog.ResetSeen(OnceKey(eventId));

    private string OnceKey(string eventId) => $"{_owner.Metadata.GUID}/{eventId}";
}
