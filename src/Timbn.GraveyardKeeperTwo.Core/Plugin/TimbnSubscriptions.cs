namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnSubscriptions : IDisposable
{
    private readonly List<Owned> _items = [];
    private readonly ManualLogSource _logger;

    internal TimbnSubscriptions(ManualLogSource logger)
    {
        _logger = logger;
    }

    public int Count => _items.Count;

    public IDisposable Add(IDisposable subscription)
    {
        var owned = new Owned(this, subscription);
        _items.Add(owned);
        return owned;
    }

    public IDisposable Add(Action undo) => Add(new TimbnUndo(undo));

    public void Dispose()
    {
        while (_items.Count > 0)
        {
            var last = _items[^1];
            _items.RemoveAt(_items.Count - 1);
            try
            {
                last.DisposeInner();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(TimbnSubscriptions)}|An undo threw: {ex}");
            }
        }
    }

    private sealed class Owned : IDisposable
    {
        private readonly TimbnSubscriptions _owner;
        private IDisposable? _inner;

        public Owned(TimbnSubscriptions owner, IDisposable inner)
        {
            _owner = owner;
            _inner = inner;
        }

        public void Dispose()
        {
            if (_inner is null)
                return;

            _owner._items.Remove(this);
            DisposeInner();
        }

        public void DisposeInner()
        {
            var inner = _inner;
            _inner = null;
            inner?.Dispose();
        }
    }
}

internal sealed class TimbnUndo : IDisposable
{
    private Action? _undo;

    public TimbnUndo(Action undo)
    {
        _undo = undo;
    }

    public void Dispose()
    {
        var undo = _undo;
        _undo = null;
        undo?.Invoke();
    }
}
