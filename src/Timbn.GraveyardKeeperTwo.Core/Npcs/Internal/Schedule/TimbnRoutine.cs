namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnRoutineStep<TBody>
{
    internal TimbnRoutineStep(string text, Func<TBody, TimbnStepResult<TBody>> run)
    {
        Text = text;
        Run = run;
    }

    internal string Text { get; }

    internal Func<TBody, TimbnStepResult<TBody>> Run { get; }
}

internal readonly struct TimbnStepResult<TBody>
{
    private TimbnStepResult(TimbnStepKind kind, double minutes, Func<Action, bool>? walk, IReadOnlyList<TimbnRoutineStep<TBody>>? steps)
    {
        Kind = kind;
        Minutes = minutes;
        Walk = walk;
        Steps = steps;
    }

    internal TimbnStepKind Kind { get; }

    internal double Minutes { get; }

    internal Func<Action, bool>? Walk { get; }

    internal IReadOnlyList<TimbnRoutineStep<TBody>>? Steps { get; }

    internal static TimbnStepResult<TBody> Next => new(TimbnStepKind.Next, 0, null, null);

    internal static TimbnStepResult<TBody> Wait(double minutes) => new(TimbnStepKind.Wait, minutes, null, null);

    internal static TimbnStepResult<TBody> WaitReal(double seconds) => new(TimbnStepKind.WaitReal, seconds, null, null);

    internal static TimbnStepResult<TBody> WalkThen(Func<Action, bool> start) => new(TimbnStepKind.Walk, 0, start, null);

    internal static TimbnStepResult<TBody> UntilIdle(double longest) => new(TimbnStepKind.Idle, longest, null, null);

    internal static TimbnStepResult<TBody> Insert(IReadOnlyList<TimbnRoutineStep<TBody>> steps) => new(TimbnStepKind.Insert, 0, null, steps);
}

internal enum TimbnStepKind
{
    Next,
    Wait,
    WaitReal,
    Walk,
    Idle,
    Insert,
}

/// <summary>
/// Runs a list of steps on the game clock, an intro once and then a loop for as long as it runs. It holds no game
/// state, so the body it moves and the clock it reads are handed in on every tick, and it can be tested on its own.
/// </summary>
internal sealed class TimbnRoutine<TBody>
{
    internal const double GiveUpMinutes = 30;

    private const double _idleSettleMinutes = 0.1;

    private const int _idleSettleTicks = 10;

    private readonly List<TimbnRoutineStep<TBody>> _steps = [];
    private readonly List<TimbnRoutineStep<TBody>> _inserted = [];
    private int _loopStart;
    private int _index;
    private Waiting _waiting;
    private double _until;
    private double _settleUntil;
    private int _settleTicks;
    private double _lastWalkingAt;
    private double _lastNow = double.MinValue;
    private int _token;
    private bool _stepInserted;

    private enum Waiting
    {
        None,
        Minutes,
        RealSeconds,
        Arrival,
        Idle,
    }

    internal bool IsActive { get; private set; }

    internal string Step { get; private set; } = "";

    internal bool IsWaitingForArrival => _waiting == Waiting.Arrival;

    internal bool IsWaitingForIdle => _waiting == Waiting.Idle;

    internal int InsertedLeft => _inserted.Count;

    internal int InsertedTotal { get; private set; }

    internal bool InTrip => _inserted.Count > 0 || (_stepInserted && _waiting != Waiting.None);

    internal Action<string>? Log { get; set; }

    internal Action<string>? Failed { get; set; }

    internal Action? TripEnded { get; set; }

    // Runs just before each step of the routine's own list (not an inserted one), which is where a trip begins.
    internal Action? StepStarting { get; set; }

    internal Func<double> RealClock { get; set; } = () => 0;

    internal void Start(IReadOnlyList<TimbnRoutineStep<TBody>> intro, IReadOnlyList<TimbnRoutineStep<TBody>> loop)
    {
        Stop();
        _steps.AddRange(intro);
        _steps.AddRange(loop);
        _loopStart = intro.Count;
        IsActive = _steps.Count > 0;
    }

    internal void Stop()
    {
        _token++;
        _steps.Clear();
        _inserted.Clear();
        _index = 0;
        _loopStart = 0;
        _waiting = Waiting.None;
        _stepInserted = false;
        InsertedTotal = 0;
        Step = "";
        IsActive = false;
    }

    internal double WaitLeft(double now) => _waiting == Waiting.Minutes ? Math.Max(0, _until - now) : 0;

    internal void Tick(TBody body, double now, bool walking, bool held)
    {
        // Setting the time of day without moving the day sends the game clock back, and waits must keep their length.
        if (now < _lastNow)
            Shift(now - _lastNow);

        _lastNow = now;
        if (!IsActive)
            return;

        if (walking || held)
            _lastWalkingAt = now;

        if (held || IsStillWaiting(now, walking))
            return;

        _waiting = Waiting.None;
        if (_stepInserted && _inserted.Count == 0)
        {
            _stepInserted = false;
            TripEnded?.Invoke();
        }

        if (_inserted.Count == 0 && _index >= _steps.Count)
        {
            if (_loopStart >= _steps.Count)
            {
                IsActive = false;
                return;
            }

            _index = _loopStart;
        }

        TimbnRoutineStep<TBody> step;
        _stepInserted = _inserted.Count > 0;
        if (_stepInserted)
        {
            step = _inserted[0];
            _inserted.RemoveAt(0);
        }
        else
        {
            step = _steps[_index++];
            StepStarting?.Invoke();
        }

        Step = step.Text;
        TimbnStepResult<TBody> result;
        try
        {
            result = step.Run(body);
        }
        catch (Exception ex)
        {
            Failed?.Invoke($"step '{step.Text}' threw and was skipped: {ex}");
            result = TimbnStepResult<TBody>.Next;
        }

        Apply(result, now);
    }

    private bool IsStillWaiting(double now, bool walking)
    {
        switch (_waiting)
        {
            case Waiting.Minutes:
                return now < _until;
            case Waiting.RealSeconds:
                return RealClock() < _until;
            case Waiting.Arrival:
                if (now - _lastWalkingAt <= GiveUpMinutes)
                    return true;

                Log?.Invoke($"gave up on '{Step}', no walking for {GiveUpMinutes:0} game minutes");
                return false;
            case Waiting.Idle:
                if (walking)
                {
                    _settleTicks = 0;
                }
                else if (_settleTicks > 0)
                {
                    _settleTicks--;
                    return true;
                }

                return now < _settleUntil || (walking && now < _until);
            default:
                return false;
        }
    }

    private void Apply(TimbnStepResult<TBody> result, double now)
    {
        switch (result.Kind)
        {
            case TimbnStepKind.Wait:
                _waiting = Waiting.Minutes;
                _until = now + result.Minutes;
                break;
            case TimbnStepKind.WaitReal:
                _waiting = Waiting.RealSeconds;
                _until = RealClock() + result.Minutes;
                break;
            case TimbnStepKind.Walk:
                var token = ++_token;
                _waiting = Waiting.Arrival;
                _lastWalkingAt = now;
                if (!result.Walk!(() => Arrived(token)) && token == _token)
                    _waiting = Waiting.None;

                break;
            case TimbnStepKind.Idle:
                _waiting = Waiting.Idle;
                _settleUntil = now + _idleSettleMinutes;
                _settleTicks = _idleSettleTicks;
                _until = now + result.Minutes;
                break;
            case TimbnStepKind.Insert:
                _inserted.InsertRange(0, result.Steps!);
                InsertedTotal = _inserted.Count;
                break;
        }
    }

    private void Shift(double minutes)
    {
        _lastWalkingAt += minutes;
        _settleUntil += minutes;
        if (_waiting is Waiting.Minutes or Waiting.Idle)
            _until += minutes;
    }

    private void Arrived(int token)
    {
        if (token == _token && _waiting == Waiting.Arrival)
            _waiting = Waiting.None;
    }
}
