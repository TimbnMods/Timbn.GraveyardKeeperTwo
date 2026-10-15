namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Works out what a station's craft queue looks like once its mod crafts are taken out, settling it the way the
/// game's own CraftComponent.UpdateQueue does, without touching the game. The current craft's index moves up for
/// every craft taken out ahead of it. When the current craft itself goes, the queue waits at the craft now in its
/// place as ReadyToStartCraft, which the game picks up a second later, and when nothing is left the queue ends
/// Finished with an index of -1, which is how the game leaves an emptied queue.
/// </summary>
internal static class TimbnCraftQueueStrip
{
    public static TimbnCraftQueuePlan Plan(IReadOnlyList<string> craftIds, int currentIndex, CraftComponentStatus status, Func<string, bool> isModCraft)
    {
        List<int> removed = [];
        for (var i = 0; i < craftIds.Count; i++)
        {
            if (isModCraft(craftIds[i]))
                removed.Add(i);
        }

        if (removed.Count == 0)
            return new TimbnCraftQueuePlan(removed, currentIndex, status, false);

        var remaining = craftIds.Count - removed.Count;
        var hadCurrent = currentIndex >= 0 && currentIndex < craftIds.Count;
        if (remaining == 0)
            return new TimbnCraftQueuePlan(removed, -1, CraftComponentStatus.Finished, hadCurrent);

        var before = removed.Count(index => index < currentIndex);
        if (hadCurrent && !removed.Contains(currentIndex))
            return new TimbnCraftQueuePlan(removed, currentIndex - before, status, false);

        var next = Math.Clamp(currentIndex - before, 0, remaining - 1);
        return new TimbnCraftQueuePlan(removed, next, CraftComponentStatus.ReadyToStartCraft, hadCurrent);
    }
}

/// <summary>The queue indices to take out, in ascending order, and the index and status the queue settles at.</summary>
internal sealed class TimbnCraftQueuePlan(List<int> removed, int currentIndex, CraftComponentStatus status, bool currentRemoved)
{
    public List<int> Removed { get; } = removed;

    public int CurrentIndex { get; } = currentIndex;

    public CraftComponentStatus Status { get; } = status;

    /// <summary>True when the craft the station was on went out, so its in progress flags have to be cleared too.</summary>
    public bool CurrentRemoved { get; } = currentRemoved;

    public bool Emptied => CurrentIndex < 0 && Removed.Count > 0;
}
