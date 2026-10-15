namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class ChurchStop : IGraveyardStop
{
    private const int _shortestStay = 60;
    private const int _longestStay = 120;

    // Because the church door only triggers transitions for the player, visitors teleport between the outside and
    // inside landings. Both landings are world objects rather than GDPoints.
    internal const string OutsideLanding = "tp_RT_church_enter";
    internal const string InsideLanding = "tp_RT_church_exit";

    // Each constructed pew provides two seating points facing the pulpit.
    private const string _seatTag = "parishioner_bench_point";
    private const float _seatTaken = 0.1f;

    public int RollStay(System.Random random) => random.Next(_shortestStay, _longestStay + 1);

    public int Capacity() => TimbnClock.Weekday == TimbnWeekday.Wrath ? 0 : Seats().Count();

    public TimbnNpcSteps? Plan(TimbnNpc npc, GraveyardVisitors visitors, int stay)
    {
        // The Sunday congregation occupies the church pews on sermon day, so casual visitors bypass the church.
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
