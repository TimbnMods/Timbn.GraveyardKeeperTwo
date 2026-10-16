namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class StandByStop : IGraveyardStop
{
    private const float _snap = 0.1f;
    private const float _pathSnap = 0.3f;
    private const float _offPath = 0.15f;

    private readonly string _name;
    private readonly string _missing;
    private readonly Func<IEnumerable<WgoData>> _find;
    private readonly int _shortestStay;
    private readonly int _longestStay;

    public StandByStop(string name, string missing, Func<IEnumerable<WgoData>> find, int shortestStay, int longestStay)
    {
        _name = name;
        _missing = missing;
        _find = find;
        _shortestStay = shortestStay;
        _longestStay = longestStay;
    }

    public int RollStay(System.Random random) => random.Next(_shortestStay, _longestStay + 1);

    public int Capacity() => _find().Count();

    public TimbnNpcSteps? Plan(TimbnNpc npc, GraveyardVisitors visitors, int stay)
    {
        var targets = _find().ToList();
        if (targets.Count == 0)
        {
            GraveyardVisitors.Log($"{_missing}, so {npc.Id} skips the {_name}.");
            return null;
        }

        var free = targets.Where(target => !visitors.IsClaimed(target)).OrderBy(_ => visitors.Random.Next()).ToList();
        foreach (var target in free)
        {
            var center = target.Position;
            if (!TryFindStandSpot(target, visitors.Paths, out var stand))
                continue;

            visitors.Claim(npc, target);
            visitors.Take(npc, stand);
            GraveyardVisitors.Log($"{npc.Id} visits the {_name} at {GraveyardVisitors.Flat(center)}, one of {free.Count} free out of {targets.Count}, and stands at {GraveyardVisitors.Flat(stand)}.");
            return visitors.Paths.Walk(npc, new TimbnNpcSteps(), stand)
                .Face(stand.z < center.z ? Direction.Up : Direction.Down)
                .WaitGameMinutes(stay);
        }

        GraveyardVisitors.Log($"{(free.Count == 0 ? $"every {_name} has a visitor" : $"no free {_name} has ground to stand on")}, so {npc.Id} skips the {_name}.");
        return null;
    }

    internal bool TryFindStandSpot(WgoData target, GraveyardPaths paths, out Vector3 stand)
    {
        var center = target.Position;
        if (!paths.TryNearest(center, out var onPath, preferAcross: true))
            onPath = center + Vector3.back;

        var direction = onPath.z <= center.z ? Vector3.forward : Vector3.back;
        return TimbnWorld.TryFindWalkable(onPath + direction * _offPath, _snap, out stand) || TimbnWorld.TryFindWalkable(onPath, _pathSnap, out stand);
    }
}
