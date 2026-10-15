namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Where the base game last had each of its own people that Core has moved since, so every save holds them there,
/// standing still, and never anywhere a mod took them, and so a person no schedule or run drives any more goes back
/// there at once. The place is taken just before Core first moves someone and
/// kept for the session, through plugin reloads. It is dropped when the game moves them itself, since the game has
/// them again. Moves that follow an event Core fired for them count as Core's until they have stood still a moment,
/// which <see cref="TimbnGameMoves"/> sorts out before telling this.
/// </summary>
internal static class TimbnBaseGamePlaces
{
    private static readonly Dictionary<WgoData, Place> _places = [];

    internal static void Remember(WgoData data)
    {
        if (!_places.ContainsKey(data))
            _places[data] = new Place(data.Position, data.MovableDirection, data.IsHidden);
    }

    internal static bool PutBack(WgoData data)
    {
        if (!_places.TryGetValue(data, out var place))
            return false;

        _places.Remove(data);
        if (data.MovementComponent is { IsMoving: true } movement)
            movement.ForceStop();

        data.Position = place.Position;
        data.MovableDirection = place.Direction;
        data.IsHidden = place.Hidden;
        return true;
    }

    internal static void GameMoved(Func<WgoData, bool> isThem)
    {
        foreach (var data in _places.Keys.Where(isThem).ToList())
            _places.Remove(data);
    }

    internal static IDisposable PlaceAllForSave()
    {
        List<Action> putBack = [];
        foreach (var pair in _places.ToList())
        {
            var data = pair.Key;
            if (!TimbnGame.IsInGame || MainGame.WorldData.GetWgoData(data.UniqueId) != data)
            {
                _places.Remove(data);
                continue;
            }

            TimbnSafe.Run(() =>
            {
                var position = data.Position;
                var direction = data.MovableDirection;
                var hidden = data.IsHidden;
                var walk = data.MovementComponent is { } movement ? TimbnWalkState.StandStill(movement) : null;
                putBack.Add(() =>
                {
                    data.Position = position;
                    data.MovableDirection = direction;
                    data.IsHidden = hidden;
                    walk?.Invoke();
                });
                data.Position = pair.Value.Position;
                data.MovableDirection = pair.Value.Direction;
                data.IsHidden = pair.Value.Hidden;
            }, $"{nameof(TimbnBaseGamePlaces)}.{nameof(PlaceAllForSave)}|{data.MovableObjectId}");
        }

        return new TimbnUndo(() =>
        {
            foreach (var undo in putBack)
                TimbnSafe.Run(undo, $"{nameof(TimbnBaseGamePlaces)}.{nameof(PlaceAllForSave)}");
        });
    }

    internal static void Reset() => _places.Clear();

    private sealed class Place
    {
        internal Place(Vector3 position, Vector2 direction, bool hidden)
        {
            Position = position;
            Direction = direction;
            Hidden = hidden;
        }

        internal Vector3 Position { get; }

        internal Vector2 Direction { get; }

        internal bool Hidden { get; }
    }
}
