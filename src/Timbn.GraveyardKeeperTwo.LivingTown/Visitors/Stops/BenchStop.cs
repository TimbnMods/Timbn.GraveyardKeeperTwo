namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class BenchStop : IGraveyardStop
{
    private const int _shortestStay = 60;
    private const int _longestStay = 120;

    // The bench models have no seat points, so these are measured off graveyard_bench_1, a backless bench 1.24 wide
    // running z -0.25 to 0.10. Deeper than these the sitter is drawn through the bench, and further forward they float.
    private const float _seatSide = 0.25f;
    private const float _seatDepthFacingDown = -0.23f;

    // Mirrors the down seat about the model's middle (z -0.075), so a sitter facing up sits on the side they face,
    // the way the church benches put their parishioner points behind the bench.
    private const float _seatDepthFacingUp = -0.06f;
    private const float _edgeStep = 0.05f;
    private const float _seatTaken = 0.1f;

    // A placed bench cuts its footprint out of the navmesh, so the nearest walkable ground is just off the seat.
    private const float _edgeRange = 0.5f;

    // A plot is only a frame of build slots (GraveyardModuleArea, 3.84 by 3.00 from its lower left corner), and the
    // cemetery's own path runs through it between the lower bench slot (z 0.60 to 1.20) and the upper one (1.80 to 2.40).
    private const string _plotPrefix = "graveyard_module_";
    private const float _plotWidth = 3.84f;
    private const float _plotDepth = 3f;
    private const float _plotPath = 1.5f;

    private static readonly string[] _benchIds = ["graveyard_bench_1", "graveyard_bench_2", "graveyard_bench_3"];

    public int RollStay(System.Random random) => random.Next(_shortestStay, _longestStay + 1);

    public TimbnNpcSteps? Plan(TimbnNpc npc, GraveyardVisitors visitors, int stay)
    {
        var benches = _benchIds.SelectMany(TimbnWorld.FindAll).ToList();
        if (benches.Count == 0)
        {
            GraveyardVisitors.Log($"no bench stands in the graveyard yet, so {npc.Id} skips the bench.");
            return null;
        }

        var plots = TimbnWorld.Objects(wgo => wgo.id.StartsWith(_plotPrefix)).ToList();
        List<(WgoData Bench, Direction Facing, List<(Vector3 Seat, Vector3 Edge)> Seats)> open = [];
        foreach (var bench in benches)
        {
            if (Facing(bench, plots) is not { } facing)
            {
                GraveyardVisitors.Log($"the bench at {GraveyardVisitors.Flat(bench.Position)} is in no plot, so nobody sits on it.");
                continue;
            }

            var seats = FreeSeats(bench, facing, visitors);
            if (seats.Count > 0)
                open.Add((bench, facing, seats));
        }

        if (open.Count == 0)
        {
            GraveyardVisitors.Log($"every bench in the graveyard is taken, so {npc.Id} skips the bench.");
            return null;
        }

        (var chosen, var facingChosen, var free) = open[visitors.Random.Next(open.Count)];
        (var seat, var edge) = free[visitors.Random.Next(free.Count)];
        visitors.Take(npc, seat);
        GraveyardVisitors.Log($"{npc.Id} sits on the bench at {GraveyardVisitors.Flat(chosen.Position)}, one of {open.Count} with room, at {GraveyardVisitors.Flat(seat)} facing {facingChosen}, stepping on from {GraveyardVisitors.Flat(edge)}.");
        return visitors.Paths.Walk(npc, new TimbnNpcSteps(), edge)
            .WalkStraight(seat.x, seat.z)
            .Face(facingChosen)
            .Animate(TimbnGameIds.NpcAnimations.Villager.Sitting)
            .WaitGameMinutes(stay)
            .WalkStraight(edge.x, edge.z);
    }

    public IEnumerable<(Vector3 Spot, Vector3 Target, bool Seat)> Spots(GraveyardVisitors visitors)
    {
        var plots = TimbnWorld.Objects(wgo => wgo.id.StartsWith(_plotPrefix)).ToList();
        foreach (var bench in _benchIds.SelectMany(TimbnWorld.FindAll))
        {
            if (Facing(bench, plots) is not { } facing)
                continue;

            foreach ((var seat, _) in Seats(bench, facing))
                yield return (seat, bench.Position, true);
        }
    }

    // Benches have one facing, so visitors sit looking at their plot's path, down from a bench above it and up from one below.
    private static Direction? Facing(WgoData bench, List<WgoData> plots)
    {
        foreach (var plot in plots)
        {
            var across = bench.Position.x - plot.Position.x;
            var up = bench.Position.z - plot.Position.z;
            if (across >= 0f && across < _plotWidth && up >= 0f && up < _plotDepth)
                return up > _plotPath ? Direction.Down : Direction.Up;
        }

        return null;
    }

    private static List<(Vector3 Seat, Vector3 Edge)> FreeSeats(WgoData bench, Direction facing, GraveyardVisitors visitors) =>
        Seats(bench, facing).Where(seat => !visitors.Taken.Any(taken => TimbnWorld.FlatDistance(taken, seat.Seat) < _seatTaken)).ToList();

    private static List<(Vector3 Seat, Vector3 Edge)> Seats(WgoData bench, Direction facing)
    {
        List<(Vector3 Seat, Vector3 Edge)> seats = [];
        var depth = facing == Direction.Up ? _seatDepthFacingUp : _seatDepthFacingDown;
        foreach (var side in new[] { -_seatSide, _seatSide })
        {
            var seat = bench.Position + new Vector3(side, 0f, depth);
            if (TryFindEdge(seat, facing, out var edge))
                seats.Add((seat, edge));
        }

        return seats;
    }

    // Visitors get on and off from the side they face, which is the path's side, never round the back.
    private static bool TryFindEdge(Vector3 seat, Direction facing, out Vector3 edge)
    {
        var ahead = facing == Direction.Up ? Vector3.forward : Vector3.back;
        for (var reach = _edgeStep; reach <= _edgeRange; reach += _edgeStep)
        {
            if (TimbnWorld.TryFindWalkable(seat + ahead * reach, _edgeStep, out edge))
                return true;
        }

        edge = default;
        return false;
    }
}
