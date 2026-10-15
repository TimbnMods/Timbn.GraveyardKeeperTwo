namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class GraveyardVisitors
{
    private static readonly string[] _monumentIds = ["graveyard_monument_0", "graveyard_monument_1", "graveyard_monument_2"];

    private static readonly string[] _columbariumIds = ["graveyard_columbarium_1", "graveyard_columbarium_2"];

    private static readonly string[] _cryptIds = ["graveyard_crypt"];

    private static readonly string[] _flowerBedIds = ["graveyard_flowerbed_1", "graveyard_flowerbed_2", "graveyard_flowerbed_3"];

    private readonly Dictionary<TimbnNpc, Visitor> _visitors = [];
    private readonly Dictionary<TimbnNpc, Vector3> _taken = [];
    private readonly Dictionary<TimbnNpc, WgoData> _claimed = [];
    private readonly IGraveyardStop _wander = new WanderStop();
    private readonly IGraveyardStop[] _stops;

    public GraveyardVisitors()
    {
        GraveStop = new StandByStop("grave", "nobody is buried in the graveyard yet", TimbnWorld.GravesWithBodies, 90, 150);
        _stops =
        [
            GraveStop,
            new StandByStop("monument", "no monument stands in the graveyard yet", () => _monumentIds.SelectMany(TimbnWorld.FindAll), 20, 40),
            new StandByStop("columbarium", "no columbarium stands in the graveyard yet", () => _columbariumIds.SelectMany(TimbnWorld.FindAll), 30, 60),
            new StandByStop("crypt", "no crypt stands in the graveyard yet", () => _cryptIds.SelectMany(TimbnWorld.FindAll), 30, 60),
            new StandByStop("flower bed", "no flower bed grows in the graveyard yet", () => _flowerBedIds.SelectMany(TimbnWorld.FindAll), 10, 30),
            new BenchStop(),
            new ChurchStop(),
            _wander,
        ];
    }

    public StandByStop GraveStop { get; }

    public System.Random Random { get; } = new();

    public GraveyardPaths Paths { get; } = new();

    public IEnumerable<Vector3> Taken => _taken.Values;

    public IEnumerable<TimbnNpc> People => _visitors.Keys;

    public static void Log(string line)
    {
        if (PluginConfig.WriteDebugLog.Value)
            Plugin.Logger.LogDebug($"day {TimbnClock.Day} {TimbnClock.TimeText} Church visits: {line}");
    }

    public static string Flat(Vector3 spot) => $"({spot.x:0.0}, {spot.z:0.0})";

    public TimbnNpcSteps? Welcome(TimbnNpc npc, double leaveAt, Func<TimbnNpc, TimbnNpcSteps> wayOut)
    {
        _visitors[npc] = new Visitor(leaveAt, wayOut);
        return Next(npc);
    }

    public IEnumerable<(Vector3 Spot, Vector3 Target, bool Seat)> Spots() => _stops.SelectMany(stop => stop.Spots(this));

    public void Take(TimbnNpc npc, Vector3 spot) => _taken[npc] = spot;

    public void Claim(TimbnNpc npc, WgoData target) => _claimed[npc] = target;

    public bool IsClaimed(WgoData target) => _claimed.ContainsValue(target);

    public void Forget(TimbnNpc npc)
    {
        _visitors.Remove(npc);
        _taken.Remove(npc);
        _claimed.Remove(npc);
        Paths.Forget(npc);
    }

    public void Clear()
    {
        _visitors.Clear();
        _taken.Clear();
        _claimed.Clear();
        Paths.Clear();
    }

    private TimbnNpcSteps? Next(TimbnNpc npc)
    {
        _taken.Remove(npc);
        _claimed.Remove(npc);
        if (!_visitors.TryGetValue(npc, out var visitor))
            return null;

        if (TimbnClock.Minutes >= visitor.LeaveAt)
        {
            Log($"{npc.Id} heads home.");
            return visitor.WayOut(npc);
        }

        var untilLeaving = (int)Math.Ceiling(visitor.LeaveAt - TimbnClock.Minutes);
        var choices = _stops.Where(stop => stop != visitor.Last || stop == _wander).ToList();
        while (choices.Count > 0)
        {
            var stop = choices[Random.Next(choices.Count)];
            var stay = Math.Max(1, Math.Min(stop.RollStay(Random), untilLeaving));
            if (stop.Plan(npc, this, stay) is { } steps)
            {
                visitor.Last = stop;
                return steps.Decide(Next);
            }

            choices.Remove(stop);
        }

        Log($"{npc.Id} finds nothing to do and waits at {Flat(npc.Position)}.");
        visitor.Last = null;
        return new TimbnNpcSteps()
            .WaitGameMinutes(5, 15)
            .Decide(Next);
    }

    private sealed class Visitor
    {
        public Visitor(double leaveAt, Func<TimbnNpc, TimbnNpcSteps> wayOut)
        {
            LeaveAt = leaveAt;
            WayOut = wayOut;
        }

        public double LeaveAt { get; }

        public Func<TimbnNpc, TimbnNpcSteps> WayOut { get; }

        public IGraveyardStop? Last { get; set; }
    }
}
