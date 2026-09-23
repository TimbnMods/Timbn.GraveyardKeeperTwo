namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds quests to the game on behalf of a plugin, removed again when the plugin unloads. A quest added with
/// <see cref="Add"/> is a whole NPC quest in one object, with its conversations and speech icon handled for
/// you. <see cref="Register"/> is the lower level, for a QuestDef you build and drive yourself. Reach it
/// through the plugin's Quests property.
/// </summary>
public sealed class TimbnPluginQuests
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginQuests(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a whole NPC quest. Core registers it in the quest log and tree, adds its text, shows a speech icon on
    /// the NPC when it is on offer or ready to hand in, and runs the offer and hand in conversations. See
    /// <see cref="TimbnQuest"/> for the fields.
    /// </summary>
    /// <param name="quest">The quest. It is read again whenever the NPC is talked to, so its lines can change.</param>
    /// <returns>A handle that removes the quest early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(TimbnQuest quest)
    {
        quest.Validate();
        var text = TimbnLocale.Add(quest.Texts());
        var registered = TimbnQuests.Register(quest.ToDefinition());
        var talk = TimbnDialog.AddTalk(quest.Npc, quest.Id + "_talk", quest.IsOffered, quest.Script);
        return _owner.Subscriptions.Add(new TimbnUndo(() =>
        {
            talk.Dispose();
            registered.Dispose();
            text.Dispose();
        }));
    }

    /// <summary>
    /// Adds the quest to the game's balance and, when a save is loaded, to that save's quest list, then redoes
    /// the quest tree layout. Blank fields are filled with the same empty values the game's own quests have.
    /// If the mod is later removed, a save that holds the quest keeps its progress as a hidden placeholder.
    /// </summary>
    /// <param name="definition">A definition from TimbnQuests.CreateDefinition.</param>
    /// <returns>A handle that removes the quest early when disposed. You don't need to keep it.</returns>
    public IDisposable Register(QuestDef definition) => _owner.Subscriptions.Add(TimbnQuests.Register(definition));
}
