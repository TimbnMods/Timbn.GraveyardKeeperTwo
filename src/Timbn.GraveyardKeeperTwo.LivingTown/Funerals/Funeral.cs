namespace Timbn.GraveyardKeeperTwo.LivingTown;

// A grave filled in since the previous check triggers a funeral. Attendees gather in the church, three mourners speak
// in turn at the choir, then everyone files out to line up along the path to the grave and view it before departing.
internal sealed class Funeral
{
    internal const float LastArrival = 9 / 24f;
    internal const int MinimumMourners = 5;

    private const float _checkedAt = 2 / 24f;
    private const float _beginsAt = 12 / 24f;
    private const float _leavesBy = 15 / 24f;
    private const string _altar = "base_god_church_player";
    private const string _choir = "crossroad_chor_player_01";
    private const string _aisle = "base_ceremony_nun_church_02";
    private const string _parishionerTag = "parishioner_point";
    private const int _firstSpeechMinutes = 60;
    private const int _laterSpeechMinutes = 30;
    private const int _viewMinutes = 5;
    private const float _lineGap = 0.8f;
    private const float _seatTaken = 0.1f;

    // The first three arrivals speak in this order. Each begins at one post and returns to another. The first speaker
    // begins directly at the altar to avoid walking there.
    private static readonly string[] _startPosts = [_altar, "church_parishioner_1", "church_parishioner_2"];

    private static readonly string[] _returnPosts = ["church_parishioner_3", "church_parishioner_1", "church_parishioner_2"];

    private readonly Plugin _plugin;
    private readonly GraveyardVisitors _visitors;
    private readonly TimbnSaveData<FuneralData> _data;
    private readonly List<Mourner> _mourners = [];
    private readonly List<Mourner> _toSpeak = [];
    private readonly List<Mourner> _line = [];
    private readonly HashSet<string> _usedPosts = [];
    private readonly List<Vector3> _usedSeats = [];
    private readonly System.Random _random = new();
    private HashSet<string> _graves = [];
    private WgoData? _grave;
    private Mourner? _speaker;
    private Vector3 _stand;
    private int _spoken;
    private bool _leaving;

    public Funeral(Plugin plugin, GraveyardVisitors visitors)
    {
        _plugin = plugin;
        _visitors = visitors;
        _data = plugin.Saves.Register<FuneralData>();
        visitors.Paths.Avoid = Crowd;
    }

    public bool IsOn => _grave != null;

    // Whether more mourners are still arriving from the road spawn point, holding the ceremony until they arrive.
    public Func<bool> MoreComing { get; set; } = () => false;

    // Non-speaking attendees stand at pew seat points, so church capacity equals available pew seats plus the three speakers.
    public static int Capacity => ChurchStop.Seats().Count() + _startPosts.Length;

    private static double BeginsAt => (TimbnClock.Day + (double)_beginsAt) * 24 * 60;

    // Past this cutoff, attendees proceed to the grave even if some speakers never spoke, preventing a delayed speaker
    // from holding up the service.
    private static bool Overdue => TimbnClock.Minutes >= (TimbnClock.Day + (double)_leavesBy) * 24 * 60;

    // The speaking begins once all attendees are seated or at their posts, or at noon even if some are still walking.
    private bool CeremonyDue =>
        TimbnClock.Minutes >= BeginsAt || (_mourners.Count > 0 && !MoreComing() && _mourners.All(mourner => mourner.InChurch));

    private GraveyardPaths Paths => _visitors.Paths;

    public void Check()
    {
        var data = _data.Current;
        if (data.CheckedDay == TimbnClock.Day || TimbnClock.TimeOfDay < _checkedAt || !_plugin.Settings.IsEnabled(PluginConfig.Funerals))
            return;

        data.CheckedDay = TimbnClock.Day;

        // Any leftover attendees from yesterday's funeral are cleared, as funeral services do not carry over overnight.
        Clear();
        var graves = TimbnWorld.GravesWithBodies().GroupBy(grave => grave.UniqueId.Id).ToDictionary(group => group.Key, group => group.First());
        if (!data.Seeded)
        {
            data.Seeded = true;
            data.KnownGraves = [.. graves.Keys];
            GraveyardVisitors.Log($"funeral, {graves.Count} grave(s) already filled in, so none of them gets one.");
            return;
        }

        data.KnownGraves.IntersectWith(graves.Keys);
        data.TriedGraves.IntersectWith(graves.Keys);
        var fresh = graves.Where(pair => !data.KnownGraves.Contains(pair.Key)).ToList();
        if (fresh.Count == 0)
        {
            GraveyardVisitors.Log("funeral, no new grave was filled in, so none today.");
            return;
        }

        // The grave remains fresh so the next daily check schedules the funeral. Funerals are postponed on sermon day to
        // leave the church for the congregation.
        if (TimbnClock.Weekday == TimbnWeekday.Wrath)
        {
            GraveyardVisitors.Log("funeral, a new grave was filled in but it is the sermon day, so tomorrow.");
            return;
        }

        if (TimbnClock.TimeOfDay >= LastArrival)
        {
            GraveyardVisitors.Log("funeral, a new grave was found too late for anyone to come today, so tomorrow.");
            return;
        }

        _grave = fresh[0].Value;
        _graves = [.. fresh.Select(pair => pair.Key)];
        GraveyardVisitors.Log($"funeral, a new grave at {GraveyardVisitors.Flat(_grave.Position)} gets one today.");

        // If a funeral fails to reach the ceremony (e.g. door pathfinding failure or the player sleeping early), the
        // second attempt is treated as completed to avoid repeating indefinitely.
        if (_graves.All(data.TriedGraves.Contains))
        {
            GraveyardVisitors.Log("funeral, it is the second try for this grave, so there is no third.");
            Held();
        }
        else
        {
            data.TriedGraves.UnionWith(_graves);
        }
    }

    public TimbnNpcSteps Welcome(TimbnNpc npc, Func<TimbnNpc, TimbnNpcSteps> wayOut)
    {
        if (ChurchStop.Landing(ChurchStop.OutsideLanding) is not { } outside || ChurchStop.Landing(ChurchStop.InsideLanding) is not { } inside)
        {
            GraveyardVisitors.Log($"funeral, the church door can't be found, so {npc.Id} goes home.");
            return wayOut(npc);
        }

        var mourner = new Mourner(npc);
        var role = _mourners.Count;
        _mourners.Add(mourner);
        if (role < _startPosts.Length)
        {
            mourner.StartPost = _startPosts[role];
            mourner.ReturnPost = _returnPosts[role];
            mourner.Speaker = true;
            _toSpeak.Add(mourner);
        }
        else if (FreeSeat() is { } seat)
        {
            mourner.Seat = seat;
            _usedSeats.Add(seat.Position);
        }
        else
        {
            mourner.StartPost = FreePost();
        }

        var place = mourner.Seat != null ? "stands at a pew seat point" : mourner.StartPost == null ? "has nowhere to stand" : $"stands at {mourner.StartPost}";
        GraveyardVisitors.Log($"funeral, {npc.Id} goes to the church and {place}{(role < _startPosts.Length ? $", speaker {role + 1}" : "")}.");
        var steps = Paths.Walk(npc, new TimbnNpcSteps(), outside).Teleport(inside);
        if (mourner.Seat is { } seatPoint)
        {
            steps.WalkNavmesh(seatPoint.Position.x, seatPoint.Position.z).Face(seatPoint.Direction);
        }
        else if (mourner.StartPost != null && Point(mourner.StartPost) is { } post)
        {
            steps.WalkNavmesh(post.Id).Face(Direction.Up);
        }

        return steps
            .RunCode(_ => mourner.InChurch = true)
            .Decide(_ => Act(mourner, wayOut));
    }

    public void Forget(TimbnNpc npc)
    {
        if (_mourners.FirstOrDefault(mourner => mourner.Npc == npc) is not { } gone)
            return;

        _mourners.Remove(gone);
        _toSpeak.Remove(gone);
        _line.Remove(gone);
        if (_speaker == gone)
            _speaker = null;

        TryFinish();
    }

    public void Clear()
    {
        _grave = null;
        _graves = [];
        _mourners.Clear();
        _toSpeak.Clear();
        _line.Clear();
        _usedPosts.Clear();
        _usedSeats.Clear();
        _speaker = null;
        _spoken = 0;
        _leaving = false;
    }

    // Mourners queued along the path, which departing attendees navigate around when an alternate route is available.
    private IReadOnlyList<Vector2> Crowd(TimbnNpc walker) =>
        _grave == null
            ? []
            : _line.Where(mourner => mourner.Npc != walker && mourner.Place >= 0).Select(mourner => new Vector2(mourner.Npc.Position.x, mourner.Npc.Position.z)).ToList();

    private static GDPointData? Point(string id) => MainGame.WorldData.gdPointsData.GetGDPointDataById(id);

    private GDPointData? FreeSeat()
    {
        var free = ChurchStop.Seats()
            .Where(seat => !_usedSeats.Any(taken => TimbnWorld.FlatDistance(taken, seat.Position) < _seatTaken))
            .ToList();
        return free.Count == 0 ? null : free[_random.Next(free.Count)];
    }

    private string? FreePost()
    {
        var reserved = _startPosts.Concat(_returnPosts).ToHashSet();
        var free = (MainGame.WorldData.gdPointsData.GetGDPointsDataByCustomTag(_parishionerTag) ?? [])
            .Where(point => point.Enabled && !reserved.Contains(point.Id) && !_usedPosts.Contains(point.Id))
            .ToList();
        if (free.Count == 0)
            return null;

        var post = free[_random.Next(free.Count)].Id;
        _usedPosts.Add(post);
        return post;
    }

    private TimbnNpcSteps? Act(Mourner mourner, Func<TimbnNpc, TimbnNpcSteps> wayOut)
    {
        if (_grave == null)
            return wayOut(mourner.Npc);

        if (!_leaving && _speaker == null)
        {
            if (_toSpeak.Count > 0 && Overdue)
            {
                GraveyardVisitors.Log($"funeral, {_toSpeak.Count} speaker(s) never spoke and it is late, so everyone leaves without them.");
                _toSpeak.Clear();
                BeginLeaving();
            }
            else if (_toSpeak.Count == 0 && CeremonyDue)
                BeginLeaving();
            else if (_toSpeak.Count > 0 && _toSpeak[0] == mourner && (_spoken > 0 || CeremonyDue))
                return Speak(mourner, wayOut);
        }

        if (_leaving)
            return Leave(mourner, wayOut);

        return new TimbnNpcSteps()
            .WaitGameMinutes(1)
            .Decide(_ => Act(mourner, wayOut));
    }

    // Once the ceremony begins, the grave is marked as having had its funeral, even if saving the game interrupts the procession.
    private void Held()
    {
        _data.Current.KnownGraves.UnionWith(_graves);
    }

    private TimbnNpcSteps Speak(Mourner mourner, Func<TimbnNpc, TimbnNpcSteps> wayOut)
    {
        Held();
        _speaker = mourner;
        var minutes = _spoken == 0 ? _firstSpeechMinutes : _laterSpeechMinutes;
        GraveyardVisitors.Log($"funeral, {mourner.Npc.Id} speaks at the choir for {minutes} minutes.");
        var steps = new TimbnNpcSteps();
        if (mourner.StartPost != _altar && Point(_altar) is { } altar)
            steps.WalkStraight(altar.Position.x, altar.Position.z);

        steps.Walk(_choir).Face(Direction.Down).WaitGameMinutes(minutes).Walk(_altar);
        if (mourner.ReturnPost != null && Point(mourner.ReturnPost) is { } back)
            steps.WalkStraight(back.Position.x, back.Position.z).Face(Direction.Up);

        return steps
            .RunCode(_ => Spoke(mourner))
            .Decide(_ => Act(mourner, wayOut));
    }

    private void Spoke(Mourner mourner)
    {
        _toSpeak.Remove(mourner);
        if (_speaker == mourner)
            _speaker = null;

        _spoken++;
    }

    private void BeginLeaving()
    {
        Held();
        _leaving = true;
        _line.AddRange(_mourners.Where(mourner => mourner.InChurch));
        if (_grave is { } grave && !_visitors.GraveStop.TryFindStandSpot(grave, Paths, out _stand))
        {
            GraveyardVisitors.Log("funeral, no ground to stand on by the grave, so they stand just below it.");
            _stand = grave.Position + Vector3.back;
        }

        GraveyardVisitors.Log($"funeral, the speaking is over and {_line.Count} leave the church for the grave.");
    }

    private TimbnNpcSteps? Leave(Mourner mourner, Func<TimbnNpc, TimbnNpcSteps> wayOut)
    {
        if (!_line.Contains(mourner))
            _line.Add(mourner);

        if (ChurchStop.Landing(ChurchStop.OutsideLanding) is not { } outside || ChurchStop.Landing(ChurchStop.InsideLanding) is not { } inside)
            return wayOut(mourner.Npc);

        // The speakers lead the exit, and remaining attendees hold position until all speakers reach the aisle
        // point, then follow spaced one minute apart.
        if (!mourner.Speaker && _mourners.Any(other => other.Speaker && !other.Reached))
        {
            return new TimbnNpcSteps()
                .WaitGameMinutes(1)
                .Decide(_ => Leave(mourner, wayOut));
        }

        var steps = new TimbnNpcSteps();
        var order = mourner.Speaker ? _line.IndexOf(mourner) : _line.Take(_line.IndexOf(mourner)).Count(other => !other.Speaker);
        if (order > 0)
            steps.WaitGameMinutes(order);

        if (mourner.Speaker)
            steps.WalkNavmesh(_aisle).RunCode(_ => mourner.Reached = true);

        return steps
            .WalkNavmesh(inside.x, inside.z)
            .Teleport(outside)
            .Decide(_ => GoToPlace(mourner, wayOut));
    }

    private TimbnNpcSteps? GoToPlace(Mourner mourner, Func<TimbnNpc, TimbnNpcSteps> wayOut)
    {
        var place = _line.IndexOf(mourner);
        if (_grave == null || place < 0)
            return wayOut(mourner.Npc);

        return Paths.Walk(mourner.Npc, new TimbnNpcSteps(), SpotAt(place))
            .RunCode(_ => mourner.Place = place)
            .Decide(_ => Turn(mourner, wayOut));
    }

    private TimbnNpcSteps? Turn(Mourner mourner, Func<TimbnNpc, TimbnNpcSteps> wayOut)
    {
        var place = _line.IndexOf(mourner);
        if (_grave is not { } grave || place < 0)
            return wayOut(mourner.Npc);

        var steps = new TimbnNpcSteps();
        if (place > 0)
        {
            if (place < mourner.Place)
                Paths.Walk(mourner.Npc, steps, SpotAt(place)).RunCode(_ => mourner.Place = place);

            return steps
                .WaitGameMinutes(1)
                .Decide(_ => Turn(mourner, wayOut));
        }

        if (mourner.Place != 0)
            Paths.Walk(mourner.Npc, steps, SpotAt(0));

        GraveyardVisitors.Log($"funeral, {mourner.Npc.Id} looks at the grave.");
        return steps
            .RunCode(_ => mourner.Place = 0)
            .Face(grave.Position)
            .WaitGameMinutes(_viewMinutes)
            .RunCode(_ => Viewed(mourner))
            .Decide(wayOut);
    }

    private void Viewed(Mourner mourner)
    {
        _line.Remove(mourner);
        TryFinish();
    }

    private Vector3 SpotAt(int place)
    {
        if (place == 0 || _grave is not { } grave)
            return _stand;

        var church = ChurchStop.Landing(ChurchStop.OutsideLanding) ?? _stand;
        return Paths.PointAlong(grave.Position, church, place * _lineGap);
    }

    private void TryFinish()
    {
        if (_grave == null || !_leaving || _line.Count > 0 || _mourners.Any(mourner => !mourner.InChurch))
            return;

        GraveyardVisitors.Log("funeral, everyone has looked at the grave, so it is over.");
        Clear();
    }

    private sealed class Mourner
    {
        public Mourner(TimbnNpc npc)
        {
            Npc = npc;
        }

        public TimbnNpc Npc { get; }

        public string? StartPost { get; set; }

        public GDPointData? Seat { get; set; }

        public string? ReturnPost { get; set; }

        public bool InChurch { get; set; }

        public bool Speaker { get; set; }

        public bool Reached { get; set; }

        public int Place { get; set; } = -1;
    }
}
