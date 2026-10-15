namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A list of steps a plugin plays on one person with <see cref="TimbnNpc.RunSteps"/>. One run per person at a time, so a
/// new one replaces the one before, whichever plugin started it. A run started while the player talks to the person
/// plays at once, and only a conversation started after it holds it still.
/// </summary>
internal sealed class TimbnNpcCommand : IDisposable
{
    private static readonly Dictionary<SGuid, TimbnNpcCommand> _running = [];

    private readonly TimbnNpc _npc;
    private readonly TimbnFrameworkPlugin _owner;
    private readonly Action? _done;
    private readonly TimbnRoutine<TimbnNpcRun> _routine = new();
    private readonly TimbnNpcRun _run;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly int _startedInTalk;
    private readonly string? _claimedBefore;
    private readonly TimbnTripClaim _tripClaim = new();
    private SGuid _id;
    private bool _disposed;
    private bool _ended;

    internal TimbnNpcCommand(TimbnNpc npc, TimbnFrameworkPlugin owner, TimbnNpcSteps steps, Action? done)
    {
        _npc = npc;
        _owner = owner;
        _done = done;
        _id = npc.Data.UniqueId;
        _run = new TimbnNpcRun(npc);
        _claimedBefore = npc.ClaimedPoint;
        if (_running.TryGetValue(_id, out var before))
            before.End();

        _running[_id] = this;
        _startedInTalk = TimbnNpcTalk.IsTalkingTo(npc.Data) ? TimbnNpcTalk.Session : -1;
        npc.Origin = null;
        npc.StopWalking();
        npc.Log($"run of {steps.Count} steps started");
        _routine.Log = npc.Log;
        _routine.Failed = line => owner.PluginLogger.LogError($"{nameof(TimbnNpcCommand)}|{npc.Id}: {line}");
        _routine.StepStarting = () => _tripClaim.Begin(npc.ClaimedPoint);
        _routine.TripEnded = () =>
        {
            if (_tripClaim.ShouldRelease(npc.ClaimedPoint))
                npc.ReleaseClaim();
        };
        _routine.RealClock = () => Time.realtimeSinceStartupAsDouble;
        _routine.Start(steps.Steps, []);
        _subscriptions.Add(TimbnNpcTalk.StopWhileTalking(npc));
        _subscriptions.Add(owner.Events.Update(Tick));
        _subscriptions.Add(TimbnGameMoves.Add(() => _npc.IsGone ? null : _npc.Data, LeaveToGame));
    }

    internal IDisposable? Owned { get; set; }

    internal static bool IsRunning(WgoData npc) => _running.ContainsKey(npc.UniqueId);

    internal static void Moved(SGuid from, SGuid to)
    {
        if (!_running.TryGetValue(from, out var command))
            return;

        _running.Remove(from);
        command._id = to;
        _running[to] = command;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_running.TryGetValue(_id, out var current) && current == this)
            _running.Remove(_id);

        _npc.StopOwnWalk();
        if (_npc.IsGone || _npc.ClaimedPoint != _claimedBefore)
            _npc.ReleaseClaim();

        _routine.Stop();
        foreach (var subscription in _subscriptions)
            subscription.Dispose();

        _subscriptions.Clear();
        if (!_ended && !_npc.IsGone)
            _npc.PutBackIfFree();
    }

    private void Tick()
    {
        if (_disposed)
            return;

        if (_npc.IsGone || !TimbnGame.IsInGame)
        {
            End();
            return;
        }

        var held = TimbnNpcTalk.IsTalkingTo(_npc.Data) && TimbnNpcTalk.Session != _startedInTalk;
        _routine.Tick(_run, TimbnClock.Minutes, _npc.IsWalking, held);
        if (_disposed || _routine.IsActive)
            return;

        _npc.Log("run done");
        End();
        if (_done != null)
            TimbnSafe.Run(_done, _owner.PluginLogger, $"{nameof(TimbnNpc)}.{nameof(TimbnNpc.RunSteps)}|{TimbnSafe.Describe(_done)}");
    }

    private void LeaveToGame(string what, bool gameWalks)
    {
        if (_disposed)
            return;

        _npc.Log($"{what}, so the run ends and leaves them to the game");
        _npc.LetGo(gameWalks);
        End();
    }

    private void End()
    {
        _ended = true;
        if (Owned != null)
            Owned.Dispose();
        else
            Dispose();
    }
}
