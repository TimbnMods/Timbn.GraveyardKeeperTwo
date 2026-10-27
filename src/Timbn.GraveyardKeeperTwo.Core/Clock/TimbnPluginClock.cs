namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Changes how fast the game's time runs on behalf of a mod. The change is undone when the mod unloads or the player
/// returns to the main menu. See <see cref="TimbnClock"/> to read the day and time. Reach it through the mod's Clock
/// property.
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
    /// 50 times faster. Several mods can hold a speed at once and the newest one wins. When the last is released,
    /// the speed goes back to what it was. If the game changed it meanwhile, the game's value stays.
    /// </summary>
    /// <example>
    /// <code>
    /// _fastTime = Clock.MultiplyGameSpeed(7f);
    /// // later
    /// _fastTime?.Dispose();
    /// </code>
    /// </example>
    /// <param name="multiplier">How many times faster than normal time runs. 1 is normal speed.</param>
    /// <returns>A handle that puts the speed back when disposed. It is also released when the mod unloads and on the way to the main menu.</returns>
    public IDisposable MultiplyGameSpeed(float multiplier) => _owner.SessionSubscriptions.Add(TimbnGameSpeed.Set(multiplier));

    /// <summary>
    /// Stops the game's clock until the returned handle is disposed, so the time of day, the weather and the
    /// calendar stand still while the world keeps moving. Several mods can hold it at once. The clock runs again
    /// when the last one lets go, unless the game unpaused it meanwhile.
    /// </summary>
    /// <example>
    /// <code>
    /// _frozen = Clock.Freeze();
    /// // later
    /// _frozen?.Dispose();
    /// </code>
    /// </example>
    /// <returns>A handle that starts the clock again when disposed. It is also released when the mod unloads and on the way to the main menu.</returns>
    public IDisposable Freeze() => _owner.SessionSubscriptions.Add(TimbnClockPause.Take());
}
