namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// How an item added with Player.AddOverheadItem sits overhead and leaves. Each rule adds one restriction
/// to the one before it.
/// </summary>
public enum TimbnOverheadRule
{
    /// <summary>
    /// As the game treats a log. The interaction key puts it down, the game's own drops, such as starting work or a
    /// fight, take it, and other things stack on top of it while the player has overhead slots to spare.
    /// </summary>
    Vanilla,

    /// <summary>
    /// The only thing in their hands. The key still puts it down and the game's drops still take it, but nothing else
    /// can be picked up while they hold it, so the pickup hint stays off and anything the game would have put in
    /// their hands lands on the ground at their feet instead. Whatever they held before is dropped at their feet when
    /// it is added, and no other AddOverheadItem is accepted until it is gone.
    /// </summary>
    Exclusive,

    /// <summary>
    /// Works like Exclusive. Only the mod can take it out of their hands, by disposing the handle. The interaction key
    /// and the game's drops leave it alone.
    /// </summary>
    OnlyByCode,
}
