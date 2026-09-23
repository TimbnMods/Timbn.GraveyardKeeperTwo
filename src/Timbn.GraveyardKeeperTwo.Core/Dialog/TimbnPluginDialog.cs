namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gives NPCs conversations on behalf of a plugin, using the game's own speech bubbles and answer choices.
/// Talks are removed when the plugin unloads. Reach it through the plugin's Dialog property.
/// </summary>
public sealed class TimbnPluginDialog
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginDialog(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

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
    /// <param name="eventId">A unique id for this talk. It becomes the NPC's interaction event.</param>
    /// <param name="isOffered">Whether the talk is available right now. A throw counts as false and is logged.</param>
    /// <param name="talk">
    /// Scripts the conversation by adding steps to the <see cref="TimbnConversation"/> it is given. Adding no
    /// steps ends the conversation straight away.
    /// </param>
    /// <returns>A handle that removes the talk early when disposed. You don't need to keep it.</returns>
    public IDisposable AddTalk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> talk) =>
        _owner.Subscriptions.Add(TimbnDialog.AddTalk(npcId, eventId, isOffered, talk));
}
