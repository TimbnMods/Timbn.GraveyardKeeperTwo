namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Notices the game moving a person itself, through one of their script events, a walk or a story graph's teleport,
/// and tells their schedule, a plugin's run and the record of where the base game has them. Core's own calls are made
/// inside <see cref="Ours"/> so they never count. Neither do the moves that follow a script event Core fired, for a few
/// ticks after it and for as long as the person keeps walking. Neither do the events the player's interaction fires,
/// the one that opens a person's menu and the ones fired while the player picks a talk, since those start a
/// conversation, not a move.
/// </summary>
internal static class TimbnGameMoves
{
    private const int _settleTicks = 10;

    private static readonly List<Watch> _watches = [];
    private static readonly TimbnSettleWindow<WgoData> _settling = new(_settleTicks);
    private static int _ours;
    private static int _interactions;

    internal static IDisposable Add(Func<WgoData?> npc, Action<string, bool> moved)
    {
        var watch = new Watch(npc, moved);
        _watches.Add(watch);
        return new TimbnUndo(() => _watches.Remove(watch));
    }

    internal static void Ours(Action action)
    {
        _ours++;
        try
        {
            action();
        }
        finally
        {
            _ours--;
        }
    }

    internal static T Ours<T>(Func<T> action)
    {
        _ours++;
        try
        {
            return action();
        }
        finally
        {
            _ours--;
        }
    }

    internal static void OurEvent(WgoData data) => _settling.Start(data);

    internal static void Tick() =>
        _settling.Tick(data => data.MovementComponent is { } movement && (TimbnWalkState.IsUnderWay(movement) ?? movement.IsMoving));

    internal static void Reset() => _settling.Clear();

    internal static void InteractionStarted() => _interactions++;

    internal static void InteractionEnded()
    {
        if (_interactions > 0)
            _interactions--;
    }

    internal static void OnEvent(WgoData npc, string eventName)
    {
        if (eventName != TimbnNpcMenu.InteractionEvent && _interactions == 0)
            Tell(data => data == npc, $"the game fired their {eventName} event", walks: false);
    }

    internal static void OnWalk(MovementComponent movement)
    {
        if (_ours > 0)
            return;

        TimbnWalkPause.LetGo(data => data.MovementComponent == movement);
        Tell(data => data.MovementComponent == movement, "the game started a walk of theirs", walks: true);
    }

    internal static void OnTeleport(WgoData? npc)
    {
        if (npc != null)
            Tell(data => data == npc, "a story graph teleported them", walks: false);
    }

    private static void Tell(Func<WgoData, bool> isThem, string what, bool walks)
    {
        if (_ours > 0)
            return;

        bool IsMoved(WgoData data) => isThem(data) && !_settling.IsSettling(data);
        TimbnBaseGamePlaces.GameMoved(IsMoved);
        if (_watches.Count == 0)
            return;

        foreach (var watch in _watches.ToList())
        {
            if (watch.Npc() is { } data && IsMoved(data))
                watch.Moved(what, walks);
        }
    }

    private sealed class Watch
    {
        public Watch(Func<WgoData?> npc, Action<string, bool> moved)
        {
            Npc = npc;
            Moved = moved;
        }

        public Func<WgoData?> Npc { get; }

        public Action<string, bool> Moved { get; }
    }
}
