namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The rules for a zombie a mod's station had locked while it worked there, kept free of game statics so they can be
/// tested. A teaching or crafting mod locks its seated zombies for the length of a craft, so a zombie taken off its
/// station when the mod goes must be let go too, or nothing is left to unlock it and it can never be picked up.
/// </summary>
internal static class TimbnSeatedZombieRules
{
    /// <summary>
    /// What detaching a worker from a mod station does to the zombie. It is always left interactable, and the entry keeps
    /// whether it was locked so a mod that comes back finds it locked again.
    /// </summary>
    internal static bool WasLocked(bool isInteractable) => !isInteractable;

    /// <summary>
    /// Whether a zombie found on load is stuck, locked with no station to unlock it. A zombie that works somewhere is
    /// locked on purpose, and so is a porter standing at its station, so only a free one counts.
    /// </summary>
    internal static bool IsStranded(ZombieType type, bool hasStation, bool isInteractable) =>
        !isInteractable && type == ZombieType.Free && !hasStation;
}
