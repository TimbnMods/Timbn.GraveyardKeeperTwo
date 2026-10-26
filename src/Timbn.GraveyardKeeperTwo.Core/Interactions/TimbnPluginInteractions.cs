namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds actions on the game's two interaction keys, either to a spot in the world, such as a barrel that is only
/// scenery, or to the game's own objects, such as a second key on a chest. Everything added is taken away again when
/// the plugin unloads. Reach it through the plugin's Interactions property.
/// </summary>
public sealed class TimbnPluginInteractions
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginInteractions(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Makes a spot in the world the player can use, such as scenery the game gives no action of its own, like a
    /// barrel or a campfire. Give it actions with <see cref="TimbnInteractionSpot.AddPrimary(TimbnBinding{string}, Action)"/> and
    /// <see cref="TimbnInteractionSpot.AddSecondary(TimbnBinding{string}, Action)"/>. The player reaches it the way they reach a chest, by standing at it
    /// and facing it, and the game's hint shows over it with its keys. An object of the game's own in reach always wins,
    /// and when several spots are in reach the closest one wins. Nothing about a spot goes in the save.
    /// </summary>
    /// <example>
    /// <code>
    /// Interactions.Register(new Vector3(-100.04f, 0f, -78.30f))
    ///     .AddPrimary(() => _hidden == null ? "Hide" : "Take", HideOrTake);
    /// </code>
    /// </example>
    /// <param name="position">The spot, in world units.</param>
    /// <param name="radius">How far past the player's own reach the spot still counts, about the size of the thing it stands for.</param>
    /// <returns>The spot, which takes itself away when disposed. It is also taken away when the plugin unloads. You don't need to keep it.</returns>
    public TimbnInteractionSpot Register(Vector3 position, float radius = TimbnInteractionSpot.DefaultRadius)
    {
        var spot = new TimbnInteractionSpot(position, radius, _owner.PluginLogger);
        _owner.Subscriptions.Add(TimbnInteractionSpots.Add(spot));
        return spot;
    }

    /// <summary>
    /// Adds actions to the game's own objects that match, such as a second key on every chest. Give it actions with
    /// <see cref="TimbnInteractionTarget.AddPrimary(TimbnBinding{string}, Action{Wgo})"/> and <see cref="TimbnInteractionTarget.AddSecondary(TimbnBinding{string}, Action{Wgo})"/>, and their
    /// hints are drawn with the game's own, the way the game draws one. An action only goes on a key the object has no
    /// action of its own on, so a chest can get a second action but never a new primary one, and a warning says so once
    /// for a key that is taken. When several plugins match the same object the first one added wins. The check runs
    /// every time the game asks about the object, so keep it cheap and never allocate in it.
    /// </summary>
    /// <example>
    /// <code>
    /// Interactions.Register(wgo => wgo.InteractionHandler is ChestInteractionHandler)
    ///     .AddSecondary(Text.Key("my_mod_name_chest"), chest => UI.ShowPrompt(Text.Get("my_mod_name_title"), NameOf(chest), name => Rename(chest, name)));
    /// </code>
    /// </example>
    /// <param name="appliesTo">Whether the object gets the actions.</param>
    /// <returns>The registration, which takes its actions away when disposed. It is also taken away when the plugin unloads. You don't need to keep it.</returns>
    public TimbnInteractionTarget Register(Func<Wgo, bool> appliesTo)
    {
        var target = new TimbnInteractionTarget(appliesTo, _owner.PluginLogger);
        _owner.Subscriptions.Add(TimbnInteractions.Add(target));
        return target;
    }
}
