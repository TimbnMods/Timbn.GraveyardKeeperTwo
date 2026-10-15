namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Sends an event to the handlers added for one craft id, kept free of game statics so it can be tested. Handlers come
/// out in the order they were added, and a handler removed while an event is being sent still hears that event.
/// </summary>
internal sealed class TimbnCraftEventRouter<TEvent>
{
    private readonly Dictionary<string, List<Action<TEvent>>> _handlers = [];

    internal bool IsEmpty => _handlers.Count == 0;

    internal IDisposable Add(string craftId, Action<TEvent> handler)
    {
        if (!_handlers.TryGetValue(craftId, out var list))
        {
            list = [];
            _handlers[craftId] = list;
        }

        list.Add(handler);
        return new TimbnUndo(() =>
        {
            list.Remove(handler);
            if (list.Count == 0)
                _handlers.Remove(craftId);
        });
    }

    internal IReadOnlyList<Action<TEvent>> HandlersFor(string craftId) =>
        _handlers.TryGetValue(craftId, out var list) ? [.. list] : [];
}
