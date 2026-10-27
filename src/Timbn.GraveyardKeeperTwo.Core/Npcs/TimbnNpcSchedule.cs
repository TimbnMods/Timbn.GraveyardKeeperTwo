namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A person following a <see cref="TimbnNpcDay"/> on the game clock in every save the player loads, set up with
/// Npcs.Schedule. Disposing it or unloading the mod stops the walk it started and lets go of its claim.
/// </summary>
public sealed class TimbnNpcSchedule : IDisposable
{
    private static readonly List<TimbnNpcSchedule> _active = [];

    private readonly TimbnPluginNpcs _npcs;
    private readonly TimbnFrameworkPlugin _owner;
    private readonly TimbnNpcDay _day;
    private readonly string _npcId;
    private readonly List<TimbnNpcBlock> _blocks;
    private readonly float[] _starts;
    private readonly TimbnRoutine<TimbnNpcRun> _routine = new();
    private readonly List<IDisposable> _subscriptions = [];
    private IDisposable? _talk;
    private TimbnNpcRun? _run;
    private int _current = -1;
    private bool _inScene;
    private readonly TimbnTripClaim _tripClaim = new();
    private string? _leftToGame;
    private bool _leftToRun;
    private bool _rejoin;
    private double _leftAt;
    private string? _conditionName;
    private bool _gated;
    private bool _waitingForStart;
    private bool _warnedUnknownNpc;
    private bool _disposed;

    internal TimbnNpcSchedule(TimbnPluginNpcs npcs, TimbnFrameworkPlugin owner, string npcId, TimbnNpcDay day)
    {
        if (day.Blocks.Count == 0)
            throw new ArgumentException("A day needs at least one block.", nameof(day));

        _npcs = npcs;
        _owner = owner;
        _day = day;
        _npcId = npcId;
        _blocks = day.Blocks.OrderBy(block => block.Start).ToList();
        _starts = _blocks.Select(block => block.Start).ToArray();
        _routine.Log = line => Npc?.Log(line);
        _routine.Failed = line => _owner.PluginLogger.LogError($"{nameof(TimbnNpcSchedule)}|{_npcId}: {line}");
        _routine.StepStarting = () => _tripClaim.Begin(Npc?.ClaimedPoint);
        _routine.TripEnded = () =>
        {
            if (Npc is { } npc && _tripClaim.ShouldRelease(npc.ClaimedPoint))
                npc.ReleaseClaim();
        };
        _routine.RealClock = () => Time.realtimeSinceStartupAsDouble;
        _subscriptions.Add(owner.Events.Update(Tick));
        _subscriptions.Add(owner.Events.SaveClosed(Reset));
        _subscriptions.Add(TimbnGameMoves.Add(() => Npc?.Data, LeaveToGame));
        _active.Add(this);
    }

    internal static bool IsScheduled(WgoData data, TimbnNpcSchedule? except = null) =>
        _active.Any(schedule => schedule != except && schedule.Npc?.Data == data);

    private TimbnNpc? Npc => _run?.Npc is { IsGone: false } npc ? npc : null;

    private TimbnNpcBlock? Block => _current >= 0 ? _blocks[_current] : null;

    /// <summary>Describes the block of the moment and what the person is doing, in one line for a debug window.</summary>
    public string Describe()
    {
        if (_disposed)
            return $"{_npcId}: schedule ended.";

        if (_gated)
            return $"{_npcId}: waiting for the day's check.";

        if (_waitingForStart)
            return $"{_npcId}: waiting for them to stand on {_day.StartPoint} before the day starts.";

        if (Block is not { } block)
            return $"{_npcId}: not running{(TimbnGame.IsInGame ? ", not in the world" : "")}.";

        if (_leftToRun)
            return $"{_npcId}: left to a plugin's run until it ends.";

        if (_leftToGame is { } what)
            return $"{_npcId}: left to the game since {what}, until a later block starts{ResumeNote()}.";

        var waitLeft = _routine.WaitLeft(TimbnClock.Minutes);
        var doing = _inScene ? "held for a story scene"
            : _routine.IsWaitingForArrival ? $"walking ({_routine.Step})"
            : waitLeft > 0 ? $"waiting, {waitLeft:0} game minutes left ({_routine.Step})"
            : _routine.IsActive ? _routine.Step
            : "done for this block";
        return $"{_npcId}: block {block.Name} from {block.Start:0.00}, {doing}.";
    }

    /// <summary>
    /// Stops the schedule. The walk it started ends, its claim is let go, and when nothing else drives the person they
    /// go back where the base game has them.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        var npc = Npc;
        Reset();
        _disposed = true;
        _active.Remove(this);
        foreach (var subscription in _subscriptions)
            subscription.Dispose();

        _subscriptions.Clear();
        npc?.PutBackIfFree(this);
    }

    private void Tick()
    {
        if (_disposed || !TimbnGame.IsInGame)
            return;

        if (!ConditionHolds())
        {
            if (!_gated)
            {
                Log("the day's check is false, leaving them alone");
                var leaving = Npc;
                Reset();
                leaving?.PutBackIfFree(this);
                _gated = true;
            }

            return;
        }

        if (_gated)
        {
            _gated = false;
            Log("the day's check is true, the day starts");
        }

        if ((Npc ?? Bind()) is not { } npc)
            return;

        var index = TimbnDayPlan.BlockAt(_starts, TimbnClock.TimeOfDay);
        npc.NextBlockStart = _starts[(index + 1) % _starts.Length];
        var talking = TimbnNpcTalk.IsTalkingTo(npc.Data);
        var scene = TimbnStoryScene.IsRunning;
        var commanded = TimbnNpcCommand.IsRunning(npc.Data);
        if (commanded && _leftToGame == null)
            LeaveToRun(npc);

        if (_leftToRun && !commanded)
            TakeBackFromRun(npc);

        if (_leftToGame != null)
        {
            if (!scene && !talking && !commanded)
                TryTakeBack(npc, index);

            return;
        }

        if (!ReadyToStart(npc))
            return;

        if (scene != _inScene)
        {
            _inScene = scene;
            if (!scene)
                npc.ResumeAfterScene();
        }

        if (scene)
            npc.PauseForScene();
        else if ((index != _current || _rejoin) && !_routine.InTrip)
            Enter(npc, index, late: _rejoin || _current < 0 || index != (_current + 1) % _blocks.Count);

        _run!.BlockDue = index != _current;
        _routine.Tick(_run, TimbnClock.Minutes, npc.IsWalking, scene || talking);
    }

    private bool ReadyToStart(TimbnNpc npc)
    {
        if (_current >= 0 || _day.StartPoint is not { } start)
            return true;

        if (npc.IsOn(start))
        {
            _waitingForStart = false;
            return true;
        }

        if (!_waitingForStart)
        {
            _waitingForStart = true;
            npc.Log($"waiting for them to stand on {start} before the day starts");
        }

        return false;
    }

    private bool ConditionHolds()
    {
        if (_day.Condition is not { } condition)
            return true;

        _conditionName ??= $"{nameof(TimbnNpcSchedule)}|{TimbnSafe.Describe(condition)}";
        return TimbnSafe.Check(condition, _owner.PluginLogger, _conditionName);
    }

    private TimbnNpc? Bind()
    {
        if (_npcs.Find(_npcId) is not { } npc)
        {
            // A person not in the world yet is waited for, but an id the game has never heard of is a typo.
            if (!_warnedUnknownNpc && TimbnGame.IsInGame && GameBalance.Me.GetDataOrNull<WGODef>(_npcId) is null)
            {
                _warnedUnknownNpc = true;
                _owner.PluginLogger.LogWarning($"{nameof(TimbnNpcSchedule)}|The game has no NPC '{_npcId}', so the schedule waits for one that never comes.");
            }

            return null;
        }

        Reset();
        npc.LogWhen = () => _day.Logged;
        npc.Doorways = _day.Doorways;
        _run = new TimbnNpcRun(npc);
        _talk = TimbnNpcTalk.StopWhileTalking(npc);
        if (TimbnNpcs.ReleaseLeftoverClaims(npc.Data) is var released and > 0)
            npc.Log($"freed {released} spot(s) still marked as taken by them in the save");

        return npc;
    }

    private void Enter(TimbnNpc npc, int index, bool late)
    {
        var block = _blocks[index];
        _current = index;
        _rejoin = false;
        npc.Origin = block.OriginPoint;
        npc.ReleaseClaim();
        npc.StopWalking();
        npc.Log($"block {block.Name}, {(late ? "joined late" : "starting on time")}");
        _routine.Start(late ? block.LateSteps.Steps : block.IntroSteps.Steps, block.LoopSteps.Steps);
    }

    private void LeaveToGame(string what, bool gameWalks)
    {
        if (_leftToRun && Npc is { } running)
        {
            _leftToRun = false;
            _leftToGame = what;
            _leftAt = TimbnClock.Minutes;
            running.Log($"{what} during a plugin's run, so the day waits for a later block start after it{ResumeNote()}");
            return;
        }

        if (_leftToGame != null || Npc is not { } npc)
            return;

        _leftToGame = what;
        _leftAt = TimbnClock.Minutes;
        _inScene = false;
        npc.LetGo(gameWalks);
        npc.ReleaseClaim();
        _routine.Stop();
        npc.Log($"{what}, so the day leaves them to the game until a later block starts{ResumeNote()}");
    }

    private void LeaveToRun(TimbnNpc npc)
    {
        _leftToGame = "a plugin's run took them";
        _leftToRun = true;
        _leftAt = TimbnClock.Minutes;
        if (_inScene)
            npc.ResumeAfterScene();

        _inScene = false;
        npc.ReleaseClaim();
        _routine.Stop();
        npc.Log($"{_leftToGame}, so the day leaves them to it until it ends");
    }

    private void TakeBackFromRun(TimbnNpc npc)
    {
        _leftToRun = false;
        _leftToGame = null;
        _rejoin = _current >= 0;
        npc.Log(_rejoin ? "the plugin's run ended, the day takes them back" : "the plugin's run ended, the day takes them back once it may start");
    }

    private void TryTakeBack(TimbnNpc npc, int index)
    {
        if (!TimbnDayPlan.IsTakeBackDue(_starts, TimbnClock.Day, TimbnClock.TimeOfDay, ref _leftAt))
            return;

        var block = _blocks[index];
        var dayStarted = _current >= 0;
        if (dayStarted)
            _current = index;

        if (_day.ResumePoint is { } home && !npc.IsOn(home))
        {
            npc.Log($"block {block.Name} started but they are not on {home}, still left to the game");
            return;
        }

        _leftToGame = null;
        if (!dayStarted)
        {
            npc.Log($"block {block.Name} started, the day takes them back once it may start");
            return;
        }

        npc.Log($"block {block.Name} started, the day takes them back");
        Enter(npc, index, late: true);
    }

    private string ResumeNote() => _day.ResumePoint is { } home ? $" with them on {home}" : "";

    private void Log(string line)
    {
        if (_day.Logged)
            _owner.PluginLogger.LogDebug($"day {TimbnClock.Day} {TimbnClock.TimeText} {_npcId}: {line}");
    }

    private void Reset()
    {
        if (_routine.IsWaitingForArrival)
            Npc?.StopOwnWalk();

        if (_inScene)
            Npc?.ResumeAfterScene();

        // Through the run, not Npc, so a claim of someone the game removed is still let go.
        _run?.Npc.ReleaseClaim();
        if (Npc is { } npc)
            npc.NextBlockStart = null;

        _routine.Stop();
        _talk?.Dispose();
        _talk = null;
        _current = -1;
        _inScene = false;
        _leftToGame = null;
        _leftToRun = false;
        _rejoin = false;
        _waitingForStart = false;
        _run = null;
    }
}
