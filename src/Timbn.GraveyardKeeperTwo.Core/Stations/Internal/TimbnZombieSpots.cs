namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The stations a plugin made with a zombie spot that never gives their seated zombie orders. The game keeps a zombie
/// seated at a supplier station as a caretaker that takes any order its zone has, so a seat is kept from doing that
/// by skipping the caretaker's update while the station it is attached to is in this set.
/// </summary>
internal static class TimbnZombieSpots
{
    private static readonly HashSet<string> _seatOnly = [];

    internal static IDisposable Register(string stationId, bool takesOrders)
    {
        if (takesOrders)
            return TimbnUndo.None;

        _seatOnly.Add(stationId);
        return new TimbnUndo(() => _seatOnly.Remove(stationId));
    }

    internal static bool KeepsSeated(ZombieWgoData zombie) =>
        _seatOnly.Count > 0 && TimbnZombieSpotRules.IsSeatOnly(zombie.AttachedWgoData?.id, _seatOnly);
}
