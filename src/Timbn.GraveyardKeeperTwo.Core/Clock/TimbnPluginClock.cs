namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Changes how fast the game's time runs on behalf of a plugin, undone when the plugin unloads or the player
/// returns to the main menu. See <see cref="TimbnClock"/> to read the day and time. Reach it through the plugin's
/// Clock property.
/// </summary>
public sealed class TimbnPluginClock
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginClock(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Makes the game's time run faster or slower until the returned handle is disposed, the way sleeping runs it
    /// 50 times faster. Several plugins can hold a speed at once, and the newest one wins. When the last is
    /// released, the speed goes back to what it was, unless the game changed it meanwhile, in which case the game's
    /// value stays. Changing the speed directly with SetTimeSpeedMultiplier instead makes mods undo each other.
    /// </summary>
    /// <example>
    /// <code>
    /// _fastTime = Clock.SetSpeed(7f);
    /// // later
    /// _fastTime?.Dispose();
    /// </code>
    /// </example>
    /// <param name="multiplier">How many times faster than normal time runs. 1 is normal speed.</param>
    /// <returns>A handle that puts the speed back when disposed. It is also released when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable SetSpeed(float multiplier) => _owner.SessionSubscriptions.Add(TimbnGameSpeed.Set(multiplier));

    /// <summary>
    /// Stops the game's clock until the returned handle is disposed, so the time of day, the weather and the
    /// calendar stand still while the world keeps moving. Several plugins can hold it at once, and the clock runs
    /// again when the last one lets go, unless the game unpaused it meanwhile. Setting the clock's pause flag
    /// directly instead makes mods undo each other.
    /// </summary>
    /// <example>
    /// <code>
    /// _frozen = Clock.Pause();
    /// // later
    /// _frozen?.Dispose();
    /// </code>
    /// </example>
    /// <returns>A handle that starts the clock again when disposed. It is also released when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable Pause() => _owner.SessionSubscriptions.Add(TimbnClockPause.Take());
}
