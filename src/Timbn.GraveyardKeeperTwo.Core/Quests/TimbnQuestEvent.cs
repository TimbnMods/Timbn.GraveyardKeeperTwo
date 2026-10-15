namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Something the player does in the game that moves a quest along, for a quest's ReadyOn or StartsOn. Each one
/// matches the game's own event for it by id.
/// </summary>
/// <example>
/// <code><![CDATA[
/// .Description("Sleep on it.").ReadyOn(TimbnQuestEvent.Slept())
/// ]]></code>
/// </example>
public sealed class TimbnQuestEvent
{
    private readonly GlobalEventsSystem.Event.Type _type;
    private readonly string _id;

    private TimbnQuestEvent(GlobalEventsSystem.Event.Type type, string id)
    {
        _type = type;
        _id = id;
    }

    /// <summary>The player finishes a craft at a station.</summary>
    /// <param name="craftId">The craft's id from the balance.</param>
    public static TimbnQuestEvent Crafted(string craftId) => new(GlobalEventsSystem.Event.Type.CraftFinish, craftId);

    /// <summary>The player finishes building something.</summary>
    /// <param name="objectId">The built object's id.</param>
    public static TimbnQuestEvent Built(string objectId) => new(GlobalEventsSystem.Event.Type.BuildBuilding, objectId);

    /// <summary>The player walks into one of the game's marked areas.</summary>
    /// <param name="zoneTag">The area's tag.</param>
    public static TimbnQuestEvent EnteredZone(string zoneTag) => new(GlobalEventsSystem.Event.Type.PlayerEnterGDZone, zoneTag);

    /// <summary>The player walks into a world zone, such as the graveyard or the church.</summary>
    /// <param name="worldZoneId">The world zone id.</param>
    public static TimbnQuestEvent EnteredWorldZone(string worldZoneId) => new(GlobalEventsSystem.Event.Type.WalkIntoWorldZone, worldZoneId);

    /// <summary>The player wins a fight.</summary>
    /// <param name="fightId">The fight level id.</param>
    public static TimbnQuestEvent WonFight(string fightId) => new(GlobalEventsSystem.Event.Type.FightWon, fightId);

    /// <summary>The player wakes up after sleeping.</summary>
    public static TimbnQuestEvent Slept() => new(GlobalEventsSystem.Event.Type.AfterSleep, "");

    /// <summary>The player uses an item.</summary>
    /// <param name="itemId">The item id.</param>
    public static TimbnQuestEvent UsedItem(string itemId) => new(GlobalEventsSystem.Event.Type.PlayerUseItem, itemId);

    internal bool Matches(GlobalEventsSystem.Event.Type type, string id) => type == _type && string.Equals(id ?? "", _id, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => _id.Length == 0 ? _type.ToString() : $"{_type} {_id}";
}
