namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Who is still settling after something Core started for them, such as a script event, for a few ticks after it and
/// for as long as they keep moving. It holds no game state, so it can be tested on its own.
/// </summary>
internal sealed class TimbnSettleWindow<TKey> where TKey : class
{
    private readonly Dictionary<TKey, int> _left = [];
    private readonly int _ticks;

    internal TimbnSettleWindow(int ticks)
    {
        _ticks = ticks;
    }

    internal void Start(TKey key) => _left[key] = _ticks;

    internal bool IsSettling(TKey key) => _left.ContainsKey(key);

    internal void Tick(Func<TKey, bool> moving)
    {
        if (_left.Count == 0)
            return;

        foreach (var key in _left.Keys.ToList())
        {
            if (moving(key))
                _left[key] = _ticks;
            else if (--_left[key] <= 0)
                _left.Remove(key);
        }
    }

    internal void Clear() => _left.Clear();
}
