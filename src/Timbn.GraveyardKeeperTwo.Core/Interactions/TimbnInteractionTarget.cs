namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Actions on the game's two interaction keys for the game's own objects that match, made with
/// <see cref="TimbnPluginInteractions.Register(Func{Wgo, bool})"/>. A label is plain text, or a callback read every
/// time the hint is drawn, so <c>Text.Key</c> gives a label that follows the player's language. A label that comes
/// back empty hides its action.
/// </summary>
public sealed class TimbnInteractionTarget : IDisposable
{
    internal TimbnInteractionTarget(Func<Wgo, bool> appliesTo, ManualLogSource logger)
    {
        AppliesTo = appliesTo;
        Logger = logger;
    }

    internal Func<Wgo, bool> AppliesTo { get; }

    internal ManualLogSource Logger { get; }

    internal bool Broken { get; set; }

    internal TimbnInteractionAction<Action<Wgo>>? Primary { get; private set; }

    internal TimbnInteractionAction<Action<Wgo>>? Secondary { get; private set; }

    /// <summary>
    /// Puts an action on the Interaction key (E, or A on a gamepad), for objects that have no primary action of their
    /// own. A second call replaces the first.
    /// </summary>
    /// <param name="label">What the key does, as text or a callback. Empty hides the action.</param>
    /// <param name="onInteract">Runs with the object when the player presses the key at it.</param>
    /// <returns>The same registration, for the next call.</returns>
    public TimbnInteractionTarget AddPrimary(TimbnBinding<string> label, Action<Wgo> onInteract)
    {
        Primary = new TimbnInteractionAction<Action<Wgo>>(label, onInteract);
        return this;
    }

    /// <summary>Puts an action on the Interaction key with a label worked out each time it is drawn.</summary>
    /// <param name="label">Returns what the key does. Empty hides the action.</param>
    /// <param name="onInteract">Runs with the object when the player presses the key at it.</param>
    /// <returns>The same registration, for the next call.</returns>
    public TimbnInteractionTarget AddPrimary(Func<string> label, Action<Wgo> onInteract) => AddPrimary(new TimbnBinding<string>(label), onInteract);

    /// <summary>
    /// Puts an action on the Action key (F, or X on a gamepad), for objects that have no second action of their own,
    /// such as a chest. A second call replaces the first.
    /// </summary>
    /// <param name="label">What the key does, as text or a callback. Empty hides the action.</param>
    /// <param name="onInteract">Runs with the object when the player presses the key at it.</param>
    /// <returns>The same registration, for the next call.</returns>
    public TimbnInteractionTarget AddSecondary(TimbnBinding<string> label, Action<Wgo> onInteract)
    {
        Secondary = new TimbnInteractionAction<Action<Wgo>>(label, onInteract);
        return this;
    }

    /// <summary>Puts an action on the Action key with a label worked out each time it is drawn.</summary>
    /// <param name="label">Returns what the key does. Empty hides the action.</param>
    /// <param name="onInteract">Runs with the object when the player presses the key at it.</param>
    /// <returns>The same registration, for the next call.</returns>
    public TimbnInteractionTarget AddSecondary(Func<string> label, Action<Wgo> onInteract) => AddSecondary(new TimbnBinding<string>(label), onInteract);

    /// <summary>Takes the actions away.</summary>
    public void Dispose() => TimbnInteractions.Remove(this);
}
