using System.Diagnostics;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>How much of a frame a coroutine may spend before it yields, so long work is spread over frames.</summary>
internal sealed class TimbnFrameBudget
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    private readonly double _milliseconds;

    internal TimbnFrameBudget(double milliseconds)
    {
        _milliseconds = milliseconds;
    }

    /// <summary>True once this frame's share is used up. The coroutine yields and calls <see cref="Restart"/> next frame.</summary>
    internal bool IsSpent => _watch.Elapsed.TotalMilliseconds >= _milliseconds;

    internal void Restart() => _watch.Restart();
}
