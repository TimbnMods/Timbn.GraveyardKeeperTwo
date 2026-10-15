namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class ChurchStop : IGraveyardStop
{
    private const int _shortestStay = 60;
    private const int _longestStay = 120;

    // The church door only lets the keeper through, so visitors are put from one landing to the other, the way the
    // keeper goes in. Both landings are world objects, not points.
    internal const string OutsideLanding = "tp_RT_church_enter";
    internal const string InsideLanding = "tp_RT_church_exit";

    // Every pew the keeper builds brings two of these, facing the pulpit.
    private const string _seatTag = "parishioner_bench_point";
    private const float _seatTaken = 0.1f;

    public int RollStay(System.Random random) => random.Next(_shortestStay, _longestStay + 1);

    public IEnumerable<(Vector3 Spot, Vector3 Target, bool Seat)> Spots(GraveyardVisitors visitors) =>
        Seats().Select(seat => (seat.Position, seat.Position, true));

    public TimbnNpcSteps? Plan(TimbnNpc npc, GraveyardVisitors visitors, int stay)
    {
        // The sermon's own crowd may want the pews, so visitors leave the church to it that day.
        if (TimbnClock.Weekday == TimbnWeekday.Wrath)
        {
            GraveyardVisitors.Log($"it is the sermon day, so {npc.Id} skips the church.");
            return null;
        }

        if (Landing(OutsideLanding) is not { } outside || Landing(InsideLanding) is not { } inside)
        {
            GraveyardVisitors.Log($"the church door can't be found, so {npc.Id} skips the church.");
            return null;
        }

        var seats = Seats().ToList();
        var free = seats.Where(seat => !visitors.Taken.Any(taken => TimbnWorld.FlatDistance(taken, seat.Position) < _seatTaken)).ToList();
        if (free.Count == 0)
        {
            GraveyardVisitors.Log($"{(seats.Count == 0 ? "the church has no pews yet" : "every pew seat is taken")}, so {npc.Id} skips the church.");
            return null;
        }

        var seat = free[visitors.Random.Next(free.Count)];
        visitors.Take(npc, seat.Position);
        GraveyardVisitors.Log($"{npc.Id} goes into the church and stands at {GraveyardVisitors.Flat(seat.Position)}, one of {free.Count} free seats out of {seats.Count}.");
        return visitors.Paths.Walk(npc, new TimbnNpcSteps(), outside)
            .Teleport(inside)
            .WalkNavmesh(seat.Position.x, seat.Position.z)
            .Face(seat.Direction)
            .WaitGameMinutes(stay)
            .WalkNavmesh(inside.x, inside.z)
            .Teleport(outside);
    }

    internal static Vector3? Landing(string id) =>
        TimbnWorld.FindAll(id) is { Count: > 0 } landings ? landings[0].Position : null;

    internal static IEnumerable<GDPointData> Seats() =>
        MainGame.WorldData.gdPointsData.GetGDPointsDataByCustomTag(_seatTag)?.Where(seat => seat.Enabled) ?? [];
}
