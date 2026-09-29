namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnHolds<T>
{
    private readonly List<Hold> _holds = [];
    private readonly Action<T> _apply;
    private readonly Action _release;

    internal TimbnHolds(Action<T> apply, Action release)
    {
        _apply = apply;
        _release = release;
    }

    internal bool IsHeld => _holds.Count > 0;

    internal IDisposable Take(T value)
    {
        var changed = _holds.Count == 0 || !EqualityComparer<T>.Default.Equals(_holds[_holds.Count - 1].Value, value);
        var hold = new Hold(this, value);
        _holds.Add(hold);
        if (changed)
            _apply(value);

        return hold;
    }

    internal void ReleaseAll()
    {
        if (_holds.Count == 0)
            return;

        foreach (var hold in _holds)
            hold.Active = false;

        _holds.Clear();
        _release();
    }

    private void Release(Hold hold)
    {
        var wasNewest = _holds.Count > 0 && _holds[_holds.Count - 1] == hold;
        if (!_holds.Remove(hold))
            return;

        if (_holds.Count == 0)
        {
            _release();
            return;
        }

        var newest = _holds[_holds.Count - 1].Value;
        if (wasNewest && !EqualityComparer<T>.Default.Equals(newest, hold.Value))
            _apply(newest);
    }

    private sealed class Hold : IDisposable
    {
        private readonly TimbnHolds<T> _owner;

        public Hold(TimbnHolds<T> owner, T value)
        {
            _owner = owner;
            Value = value;
        }

        public T Value { get; }

        public bool Active { get; set; } = true;

        public void Dispose()
        {
            if (!Active)
                return;

            Active = false;
            _owner.Release(this);
        }
    }
}
