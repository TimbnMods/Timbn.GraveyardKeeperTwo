namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Lets the keeper walk through chosen people by excluding the keeper's body layers on their solid colliders. A
/// person's view exists all the time, but its parts, which carry the colliders, are pooled and only loaded while the
/// person is on or near the screen, so the layers are put on as parts load and taken off again as they go back to the
/// pool, where another person may pick them up.
/// </summary>
internal static class TimbnNpcCollision
{
    private const float _checkSeconds = 0.5f;

    private static readonly Dictionary<TimbnNpc, List<Collider>> _passing = [];
    private static float _nextCheck;

    internal static void Set(TimbnNpc npc, bool collides)
    {
        if (collides)
        {
            if (_passing.Remove(npc, out var marked))
                Restore(marked);

            return;
        }

        _passing.TryAdd(npc, []);
        _nextCheck = 0f;
    }

    internal static void Forget(TimbnNpc npc)
    {
        if (_passing.Remove(npc, out var marked))
            Restore(marked);
    }

    internal static void Reset()
    {
        foreach (var marked in _passing.Values)
            Restore(marked);

        _passing.Clear();
    }

    internal static void Tick()
    {
        if (_passing.Count == 0 || Time.unscaledTime < _nextCheck)
            return;

        _nextCheck = Time.unscaledTime + _checkSeconds;
        var mask = PlayerLayers();
        if (mask == 0)
            return;

        foreach (var (npc, marked) in _passing.ToList())
        {
            if (npc.IsGone)
            {
                Restore(marked);
                _passing.Remove(npc);
                continue;
            }

            var view = TimbnWorld.ViewOf(npc.Data);
            for (var i = marked.Count - 1; i >= 0; i--)
            {
                var collider = marked[i];
                if (collider != null && view != null && collider.transform.IsChildOf(view.transform))
                    continue;

                if (collider != null)
                    collider.excludeLayers = 0;

                marked.RemoveAt(i);
            }

            if (view == null)
                continue;

            foreach (var collider in view.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger)
                    continue;

                if (collider.excludeLayers.value != mask)
                    collider.excludeLayers = mask;

                if (!marked.Contains(collider))
                    marked.Add(collider);
            }
        }
    }

    private static void Restore(List<Collider> marked)
    {
        foreach (var collider in marked)
        {
            if (collider != null)
                collider.excludeLayers = 0;
        }

        marked.Clear();
    }

    // The layers of the solid parts of the player's body, so whatever the game gives it is covered.
    private static int PlayerLayers()
    {
        var body = UnityEngine.Object.FindFirstObjectByType<PlayerPhysicalBody>();
        if (body == null)
            return 0;

        var mask = 0;
        foreach (var collider in body.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.isTrigger)
                mask |= 1 << collider.gameObject.layer;
        }

        return mask;
    }
}
