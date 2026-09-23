namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnSubscriptions : IDisposable
{
    private readonly List<IDisposable> _items = [];
    private readonly ManualLogSource _logger;

    internal TimbnSubscriptions(ManualLogSource logger)
    {
        _logger = logger;
    }

    public int Count => _items.Count;

    public T Add<T>(T subscription) where T : IDisposable
    {
        _items.Add(subscription);
        return subscription;
    }

    public IDisposable Add(Action undo) => Add(new TimbnUndo(undo));

    public void Dispose()
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            try
            {
                _items[i].Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(TimbnSubscriptions)}|An undo threw: {ex}");
            }
        }

        _items.Clear();
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
