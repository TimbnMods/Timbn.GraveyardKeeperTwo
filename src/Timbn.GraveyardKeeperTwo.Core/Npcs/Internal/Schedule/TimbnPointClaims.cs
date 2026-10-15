namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Which point each NPC has claimed, one point per NPC, so two schedules never send people to the same bench. A claim
/// is also passed on to the game through <c>occupy</c> and <c>release</c>, so the life simulator keeps its villagers away.
/// </summary>
internal sealed class TimbnPointClaims<TOwner> where TOwner : class
{
    private readonly Dictionary<string, TOwner> _holders = [];
    private readonly Dictionary<TOwner, string> _spots = new(new SameObject());
    private readonly Action<string, TOwner> _occupy;
    private readonly Action<string> _release;

    internal TimbnPointClaims(Action<string, TOwner> occupy, Action<string> release)
    {
        _occupy = occupy;
        _release = release;
    }

    internal int Count => _holders.Count;

    internal IEnumerable<KeyValuePair<string, TOwner>> Held => _holders;

    internal string? PointOf(TOwner owner) => _spots.TryGetValue(owner, out var point) ? point : null;

    internal TOwner? HolderOf(string point) => _holders.TryGetValue(point, out var holder) ? holder : null;

    internal bool Claim(string point, TOwner owner, bool takenInGame = false)
    {
        if (HolderOf(point) is { } holder)
            return ReferenceEquals(holder, owner);

        if (takenInGame)
            return false;

        Release(owner);
        _holders[point] = owner;
        _spots[owner] = point;
        _occupy(point, owner);
        return true;
    }

    internal void Release(TOwner owner)
    {
        if (!_spots.TryGetValue(owner, out var point))
            return;

        _spots.Remove(owner);
        _holders.Remove(point);
        _release(point);
    }

    internal void ReleaseAll()
    {
        foreach (var owner in _spots.Keys.ToList())
            Release(owner);
    }

    internal string? WhyBlocked(string point, TOwner? owner, bool exists, bool enabled, bool takenInGame, string? standingThere)
    {
        if (!exists)
            return "no such point";

        if (!enabled)
            return "disabled";

        var holder = HolderOf(point);
        if (holder != null && !ReferenceEquals(holder, owner))
            return "claimed by another schedule";

        if (takenInGame && holder == null)
            return "claimed in the simulator";

        return standingThere is null ? null : "occupied by " + standingThere;
    }

    private sealed class SameObject : IEqualityComparer<TOwner>
    {
        public bool Equals(TOwner? first, TOwner? second) => ReferenceEquals(first, second);

        public int GetHashCode(TOwner owner) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner);
    }
}
