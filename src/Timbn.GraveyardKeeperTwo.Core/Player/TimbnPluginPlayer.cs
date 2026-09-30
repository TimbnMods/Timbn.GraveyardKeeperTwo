namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Acts on the player character, with every change undone when the plugin unloads or the player returns to the
/// main menu. See <see cref="TimbnPlayer"/> to read where the player is. Reach it through the plugin's Player
/// property.
/// </summary>
public sealed class TimbnPluginPlayer
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginPlayer(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Takes the player's control away, the way the game does during its own cutscenes and conversations. They
    /// cannot walk, interact, attack, use the hotbar, or open the game's menus with their keys, nothing can push
    /// them, and hotkeys checked with <see cref="TimbnInput.IsDownWithControl"/> stay quiet. Each call adds its own
    /// reason to the game's list of reasons the player is held, and the game only gives control back once every
    /// reason has let go, so several plugins can take control at once and a game cutscene ending meanwhile never
    /// frees the player. Read your own keys with LazyInput while it is held, and see
    /// <see cref="TimbnPlayer.IsControlTakenByGame"/> to notice the game stepping in.
    /// </summary>
    /// <example>
    /// <code>
    /// _control = Player.TakeControl();
    /// // later
    /// _control?.Dispose();
    /// </code>
    /// </example>
    /// <returns>A handle that gives control back when disposed. It is also released when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable TakeControl() => _owner.SessionSubscriptions.Add(TimbnControl.Take());

    /// <summary>
    /// Makes the player walk faster or slower until the returned handle is disposed. It multiplies the game's own
    /// speed without changing it, so the game's slowdowns, such as aiming a bow, still apply on top and cannot
    /// cancel it. Several plugins can hold a speed at once, and the newest one wins. When the last is released,
    /// the player walks at the game's speed again.
    /// </summary>
    /// <example>
    /// <code>
    /// Buff = new TimbnPotionBuff
    /// {
    ///     OnStart = () => _swift = Player.SetSpeed(1.5f),
    ///     OnEnd = () => _swift?.Dispose(),
    /// };
    /// </code>
    /// </example>
    /// <param name="multiplier">How many times the normal walking speed. 1 is normal.</param>
    /// <returns>A handle that ends the change when disposed. It is also released when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable SetSpeed(float multiplier) => _owner.SessionSubscriptions.Add(TimbnMovement.SetSpeed(multiplier));
}
