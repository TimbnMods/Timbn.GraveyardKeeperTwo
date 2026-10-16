namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class ChurchVisits
{
    private const string _arrive = "donkey_01";
    private const string _road = "donkey_04";
    private const string _gate = "gd_graveyard_weed_4";
    private const float _opens = 6 / 24f;
    private const float _closes = 13 / 24f;
    private const float _everyoneLeaves = 16 / 24f;
    private const float _sermonDayCrowd = 1.5f;
    private const float _rainShare = 0.5f;
    private const float _stormShare = 0.25f;
    private const int _shortestStay = 4 * 60;
    private const int _longestStay = 6 * 60;
    private const int _funeralShortestGap = 2;
    private const int _funeralLongestGap = 10;
    private const float _groupSpacing = 3f;
    private const float _groupSpacingJitter = 1f;
    private const int _capacitySpare = 3;
    private const float _preloadAt = 2 / 24f;
    private const int _preloadCopies = 2;

    private static readonly string[] _homeGroups =
    [
        TimbnGameIds.Groups.VillagersDayAll,
        TimbnGameIds.Groups.TownA1DayAll,
        TimbnGameIds.Groups.TownA2DayAll,
        TimbnGameIds.Groups.TownA3DayAll,
        TimbnGameIds.Groups.TownA4DayAll,
    ];

    private static readonly string[] _heavyRain = ["Strong", "Storm", "Thunder"];

    private readonly Plugin _plugin;
    private readonly GraveyardVisitors _driver = new();
    private readonly Funeral _funeral;
    private readonly List<TimbnNpc> _visitors = [];
    private readonly System.Random _random = new();
    private readonly List<(double At, int Size)> _funeralArrivals = [];
    private readonly List<(double At, string Kind, double LeaveAt)> _waiting = [];
    private int _crowdDay = -1;
    private int _preloadDay = -1;
    private bool _plannedForFuneral;
    private int _toArrive;
    private int _settled;
    private double _nextArrival;
    private double _lastMinutes;

    public ChurchVisits(Plugin plugin)
    {
        _plugin = plugin;
        _funeral = new Funeral(plugin, _driver) { MoreComing = () => _toArrive > 0 || _waiting.Count > 0 };
    }

    private bool IsOpen => TimbnClock.TimeOfDay >= _opens && TimbnClock.TimeOfDay < (_funeral.IsOn ? Funeral.LastArrival : _closes);

    private static bool IsSermonDay => TimbnClock.Weekday == TimbnWeekday.Wrath;

    private static double EveryoneLeavesAt => (TimbnClock.Day + (double)_everyoneLeaves) * 24 * 60;

    private static float WeatherShare =>
        !TimbnWorld.IsRaining ? 1f
        : _heavyRain.Any(heavy => TimbnWorld.Weather.Contains(heavy)) ? _stormShare
        : _rainShare;

    private bool VisitsOn => _plugin.Settings.IsEnabled(PluginConfig.ChurchVisits);

    public void Tick()
    {
        // Funerals run even when daily visits are disabled, so the update loop only pauses when both features are turned off.
        if (!TimbnGame.IsInGame || (!VisitsOn && !_plugin.Settings.IsEnabled(PluginConfig.Funerals)))
            return;

        foreach (var gone in _visitors.Where(npc => npc.IsGone).ToList())
        {
            _visitors.Remove(gone);
            _driver.Forget(gone);
            _funeral.Forget(gone);
        }

        // A story scene that claims an NPC interrupts their routine without invoking the exit callback, which would
        // otherwise leave them stranded until the next save.
        foreach (var taken in _visitors.Where(npc => !npc.IsRunning).ToList())
        {
            GraveyardVisitors.Log($"{taken.Id} was taken over by the game, so they are sent away.");
            Leave(taken);
        }

        var now = TimbnClock.Minutes;
        if (now < _lastMinutes)
            _nextArrival = now;

        _lastMinutes = now;
        _funeral.Check();

        // A rebuilt town district brings new villagers mid session, so their looks load in the background each night,
        // hours before anyone can visit.
        if (_preloadDay != TimbnClock.Day && TimbnClock.TimeOfDay >= _preloadAt)
        {
            _preloadDay = TimbnClock.Day;
            _plugin.Npcs.Preload(Kinds(), _preloadCopies);
        }

        // A spawn can take 10 to 30 ms the first time the game shows a villager kind, so a tick spawns at most one
        // visitor and does nothing else. A group already on its way still finishes arriving after closing time.
        if (_waiting.Count > 0 && now >= _waiting[0].At)
        {
            SpawnNext();
            return;
        }

        if (!IsOpen)
            return;

        if (_crowdDay != TimbnClock.Day)
        {
            PlanCrowd();
            return;
        }

        if (_funeral.IsOn && !_plannedForFuneral)
            Replan("a funeral started after today's crowd was planned");

        var funeral = _funeral.IsOn;
        if (_toArrive <= 0 || (funeral ? _funeralArrivals.Count == 0 || now < _funeralArrivals[0].At : now < _nextArrival))
            return;

        var kinds = Kinds().OrderBy(_ => _random.Next()).ToList();
        if (kinds.Count == 0)
        {
            _toArrive = 0;
            GraveyardVisitors.Log("nobody lives in the village or the town yet, so nobody comes today.");
            return;
        }

        int size;
        if (funeral)
        {
            size = Mathf.Min(_toArrive, _funeralArrivals[0].Size);
            _funeralArrivals.RemoveAt(0);
        }
        else
        {
            size = Mathf.Min(_toArrive, GroupSize());
            _nextArrival = now + _random.Next(5, 31);
        }

        if (!funeral && _random.NextDouble() >= WeatherShare)
        {
            _toArrive -= size;
            _settled += size;
            GraveyardVisitors.Log($"{size} visitor(s) stay home in the {TimbnWorld.Weather}.");
            return;
        }

        Arrive(size, kinds);
        _toArrive -= size;
        _settled += size;
        SpawnNext();
    }

    // Done while the save loads, so the first morning doesn't stall on loading each villager's look or on building
    // the paths for the first time.
    public void Prepare()
    {
        if (!VisitsOn && !_plugin.Settings.IsEnabled(PluginConfig.Funerals))
            return;

        _plugin.Npcs.Preload(Kinds(), _preloadCopies);
        _driver.Paths.Rebuild();
        _driver.Paths.Warm();
    }

    public void Replan() => Replan("the settings changed");

    private static IEnumerable<string> Kinds() => _homeGroups.SelectMany(TimbnNpcs.GroupMemberIds).Distinct();

    private void Replan(string why)
    {
        if (!TimbnGame.IsInGame || _crowdDay != TimbnClock.Day)
            return;

        _plannedForFuneral = _funeral.IsOn;
        _toArrive = Math.Max(0, TodaysCrowd() - _settled);
        if (_funeral.IsOn)
            ScheduleFuneralArrivals();

        GraveyardVisitors.Log($"{why}, {_toArrive} more visitor(s) today.");
    }

    public void SendAway()
    {
        foreach (var npc in _visitors)
        {
            GraveyardVisitors.Log($"{npc.Id} is sent away.");
            npc.Dispose();
        }

        _visitors.Clear();
        _waiting.Clear();
        _driver.Clear();
        _funeral.Clear();
    }

    public void Reset()
    {
        SendAway();
        _toArrive = 0;
        _crowdDay = -1;
        _preloadDay = -1;
    }

    private int TodaysCrowd()
    {
        var minimum = PluginConfig.MinimumChurchVisitors.Value;
        var maximum = Mathf.Max(minimum, PluginConfig.MaximumChurchVisitors.Value);
        var crowd = VisitsOn ? Mathf.Clamp(TimbnWorld.TownGratitude / PluginConfig.GratitudePerVisitor.Value, minimum, maximum) : 0;
        if (_funeral.IsOn)
            return Mathf.Min(Mathf.Max(crowd, Funeral.MinimumMourners), Funeral.Capacity);

        if (IsSermonDay)
            crowd = Mathf.Min(Mathf.FloorToInt(crowd * _sermonDayCrowd), maximum);

        // A graveyard with little to see holds few at once. Arrivals spread over the morning, so a few more fit in turn.
        return Mathf.Min(crowd, Mathf.Max(minimum, _driver.Capacity() + _capacitySpare));
    }

    private TimbnNpcSteps WayOut(TimbnNpc npc)
    {
        // Because the road waypoint is outside the graveyard, the path walk exits through the gate and continues directly
        // along the road in a single movement.
        var steps = new TimbnNpcSteps();
        if (TimbnWorld.Point(_road) is { } road)
            _driver.Paths.Walk(npc, steps, road.Position);
        else
            steps.WalkNavmesh(_road);

        return steps.Walk(_arrive);
    }

    private void PlanCrowd()
    {
        _crowdDay = TimbnClock.Day;
        _plannedForFuneral = _funeral.IsOn;
        _settled = 0;
        _toArrive = TodaysCrowd();
        _nextArrival = TimbnClock.Minutes;
        if (_funeral.IsOn)
            ScheduleFuneralArrivals();

        _driver.Paths.Rebuild();
        _driver.Paths.Entrance = TimbnWorld.Point(_gate)?.Position;
        GraveyardVisitors.Log($"town gratitude {TimbnWorld.TownGratitude}{(IsSermonDay ? ", the sermon day" : "")}, {_toArrive} visitor(s) today.");
        var bounds = TimbnWorld.GraveyardBounds;
        GraveyardVisitors.Log($"the graveyard runs x {bounds.xMin:0.0} to {bounds.xMax:0.0}, z {bounds.yMin:0.0} to {bounds.yMax:0.0}.");
    }

    // Mourners arrive in small staggered groups with randomized intervals, arriving before the cutoff. If arrivals would
    // exceed the cutoff, intervals are compressed to fit.
    private void ScheduleFuneralArrivals()
    {
        _funeralArrivals.Clear();
        var from = Math.Max(TimbnClock.Minutes, (TimbnClock.Day + (double)_opens) * 24 * 60);
        var to = Math.Max(from, (TimbnClock.Day + (double)Funeral.LastArrival) * 24 * 60 - 1);
        var sizes = new List<int>();
        var gaps = new List<double>();
        for (var left = _toArrive; left > 0;)
        {
            var size = Math.Min(left, FuneralGroupSize());
            sizes.Add(size);
            gaps.Add(_funeralShortestGap + _random.NextDouble() * (_funeralLongestGap - _funeralShortestGap));
            left -= size;
        }

        var squeeze = Math.Min(1.0, (to - from) / Math.Max(1.0, gaps.Skip(1).Sum()));
        var at = from;
        for (var group = 0; group < sizes.Count; group++)
        {
            if (group > 0)
                at += gaps[group] * squeeze;

            _funeralArrivals.Add((at, sizes[group]));
        }

        GraveyardVisitors.Log($"funeral, {_toArrive} mourner(s) come in groups of {string.Join(", ", _funeralArrivals.Select(arrival => arrival.Size))}.");
    }

    private int FuneralGroupSize()
    {
        var roll = _random.NextDouble();
        return roll < 0.5 ? 1 : roll < 0.8 ? 2 : roll < 0.93 ? 3 : _random.Next(4, 6);
    }

    private int GroupSize()
    {
        var roll = _random.NextDouble();
        return roll < 0.6 ? 1 : roll < 0.9 ? 2 : 3;
    }

    // Group members spawn a few minutes apart so they don't appear on top of each other at the road spawn point, and
    // so each spawn gets a tick of its own.
    private void Arrive(int size, IReadOnlyList<string> kinds)
    {
        var now = TimbnClock.Minutes;
        var leaveAt = Math.Min(now + _random.Next(_shortestStay, _longestStay + 1), EveryoneLeavesAt);
        for (var place = 0; place < size; place++)
        {
            var at = place == 0 ? now : now + place * _groupSpacing + _random.NextDouble() * _groupSpacingJitter;
            _waiting.Add((at, kinds[place % kinds.Count], leaveAt));
        }

        _waiting.Sort((first, second) => first.At.CompareTo(second.At));
    }

    private void SpawnNext()
    {
        (_, var kind, var leaveAt) = _waiting[0];
        _waiting.RemoveAt(0);
        if (_plugin.Npcs.Spawn(kind, _arrive) is not { } npc)
        {
            _waiting.Clear();
            _toArrive = 0;
            GraveyardVisitors.Log("nobody could be added at the donkey, so nobody else comes today.");
            return;
        }

        npc.CollidesWithPlayer = false;
        _visitors.Add(npc);

        // The walk into the graveyard begins at the road waypoint and proceeds through the gate onto the interior
        // paths in a single continuous movement.
        var setOff = TimbnClock.Minutes;
        var steps = new TimbnNpcSteps();
        steps.Walk(_road)
            .RunCode(visitor => GraveyardVisitors.Log($"{visitor.Id} reaches {_road} {TimbnClock.Minutes - setOff:0} minutes after arriving."))
            .Decide(visitor => _funeral.IsOn ? _funeral.Welcome(visitor, WayOut) : _driver.Welcome(visitor, leaveAt, WayOut));

        npc.RunSteps(steps, () => Leave(npc));
        GraveyardVisitors.Log($"{npc.Id} arrives at {_arrive}.");
    }

    private void Leave(TimbnNpc npc)
    {
        GraveyardVisitors.Log($"{npc.Id} leaves.");
        _visitors.Remove(npc);
        _driver.Forget(npc);
        _funeral.Forget(npc);
        npc.Dispose();
    }
}
