namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A spot in the world with actions on the game's two interaction keys, made with
/// <see cref="TimbnPluginInteractions.Register(Vector3, float)"/>. A label is plain text, or a callback read every time
/// the hint is drawn, so <c>Text.Key</c> gives a label that follows the player's language. A label that comes back
/// empty hides its action, and a spot with no action showing is not offered at all.
/// </summary>
public sealed class TimbnInteractionSpot : IDisposable
{
    /// <summary>How far past the player's own reach a spot counts by default, about the size of a barrel.</summary>
    public const float DefaultRadius = 0.3f;

    internal TimbnInteractionSpot(Vector3 position, float radius, ManualLogSource logger)
    {
        Position = position;
        Radius = radius;
        Logger = logger;
    }

    /// <summary>The spot, in world units.</summary>
    public Vector3 Position { get; }

    /// <summary>How far past the player's own reach the spot still counts. It can change while the spot is in the world, such as from a setting.</summary>
    public float Radius { get; set; }

    /// <summary>How high over the spot the hint is drawn, 1.2 by default, about the top of a barrel.</summary>
    public float HintHeight { get; set; } = 1.2f;

    internal ManualLogSource Logger { get; }

    internal TimbnInteractionAction<Action>? Primary { get; private set; }

    internal TimbnInteractionAction<Action>? Secondary { get; private set; }

    /// <summary>Puts an action on the Interaction key (E, or A on a gamepad). A second call replaces the first.</summary>
    /// <param name="label">What the key does, as text or a callback. Empty hides the action.</param>
    /// <param name="onInteract">Runs when the player presses the key at the spot.</param>
    /// <returns>The same spot, for the next call.</returns>
    public TimbnInteractionSpot AddPrimary(TimbnBinding<string> label, Action onInteract)
    {
        Primary = new TimbnInteractionAction<Action>(label, onInteract);
        return this;
    }

    /// <summary>Puts an action on the Interaction key with a label worked out each time it is drawn, such as Hide or Take.</summary>
    /// <param name="label">Returns what the key does. Empty hides the action.</param>
    /// <param name="onInteract">Runs when the player presses the key at the spot.</param>
    /// <returns>The same spot, for the next call.</returns>
    public TimbnInteractionSpot AddPrimary(Func<string> label, Action onInteract) => AddPrimary(new TimbnBinding<string>(label), onInteract);

    /// <summary>Puts an action on the Action key (F, or X on a gamepad). A second call replaces the first.</summary>
    /// <param name="label">What the key does, as text or a callback. Empty hides the action.</param>
    /// <param name="onInteract">Runs when the player presses the key at the spot.</param>
    /// <returns>The same spot, for the next call.</returns>
    public TimbnInteractionSpot AddSecondary(TimbnBinding<string> label, Action onInteract)
    {
        Secondary = new TimbnInteractionAction<Action>(label, onInteract);
        return this;
    }

    /// <summary>Puts an action on the Action key with a label worked out each time it is drawn.</summary>
    /// <param name="label">Returns what the key does. Empty hides the action.</param>
    /// <param name="onInteract">Runs when the player presses the key at the spot.</param>
    /// <returns>The same spot, for the next call.</returns>
    public TimbnInteractionSpot AddSecondary(Func<string> label, Action onInteract) => AddSecondary(new TimbnBinding<string>(label), onInteract);

    /// <summary>Takes the spot away.</summary>
    public void Dispose() => TimbnInteractionSpots.Remove(this);
}
