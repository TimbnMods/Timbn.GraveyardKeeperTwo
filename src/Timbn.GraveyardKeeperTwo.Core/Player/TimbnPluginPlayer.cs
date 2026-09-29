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
    /// Holds the player still. The game's own requests to lock or unlock movement are remembered meanwhile and
    /// applied when the last hold is released, so nothing that happens while held can free the player early or
    /// leave them stuck. Several plugins can hold at once. The player can still use the menus and other keys, so
    /// see <see cref="TakeControl"/> to take those away too.
    /// </summary>
    /// <example>
    /// <code>
    /// _hold = Player.HoldStill();
    /// // later
    /// _hold?.Dispose();
    /// </code>
    /// </example>
    /// <returns>A handle that releases the hold when disposed. It is also released when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable HoldStill() => _owner.SessionSubscriptions.Add(TimbnMovement.HoldStill());

    /// <summary>
    /// Takes the player's control away, the way the game does during its own dialogues and cutscenes, so they can
    /// neither move nor interact and the game's hotkeys stay quiet. Core keeps its own flag for this, so the game
    /// giving control back after one of its cutscenes never frees the player early, and several plugins can take
    /// control at once. <see cref="TimbnInput.PlayerHasControl"/> reads false while it is held.
    /// </summary>
    /// <example>
    /// <code>
    /// using (Player.TakeControl())
    ///     RunScriptedMoment();
    /// </code>
    /// </example>
    /// <returns>A handle that gives control back when disposed. It is also released when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable TakeControl() => _owner.SessionSubscriptions.Add(TimbnControl.Take());

    /// <summary>
    /// Makes the player walk faster or slower until the returned handle is disposed. Several plugins can hold a
    /// speed at once, and the newest one wins. When the last is released, the speed goes back to what it was,
    /// unless something else changed it meanwhile.
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
    /// <returns>A handle that puts the speed back when disposed. It is also released when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable SetSpeed(float multiplier) => _owner.SessionSubscriptions.Add(TimbnMovement.SetSpeed(multiplier));
}
