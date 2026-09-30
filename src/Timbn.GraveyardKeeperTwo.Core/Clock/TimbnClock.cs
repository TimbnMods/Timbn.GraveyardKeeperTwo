using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads the game's calendar and clock for the loaded save, so a mod can ask what day or time it is without
/// digging through EnvironmentEngine and the save. Every member reads as zero or empty at the main menu.
/// </summary>
public static class TimbnClock
{
    private static readonly (TimbnWeekday Day, string Id)[] _weekdayIds =
    [
        (TimbnWeekday.Gluttony, LazyConsts.ConstDefs.DAY_GLUTTONY),
        (TimbnWeekday.Sloth, LazyConsts.ConstDefs.DAY_SLOTH),
        (TimbnWeekday.Lust, LazyConsts.ConstDefs.DAY_LUST),
        (TimbnWeekday.Envy, LazyConsts.ConstDefs.DAY_ENVY),
        (TimbnWeekday.Pride, LazyConsts.ConstDefs.DAY_PRIDE),
        (TimbnWeekday.Wrath, LazyConsts.ConstDefs.DAY_WRATH),
    ];

    private static EnvironmentData? Data => TimbnGame.IsInGame ? MainGame.Instance.GameSave?.environmentData : null;

    /// <summary>The day of the save, counting up from 1, the same number NewDayStarted hands its handlers.</summary>
    public static int Day => Data?.Day ?? 0;

    /// <summary>
    /// The day's place in the game's six day week, from 1 to 6, the same number NewWeekdayStarted hands its handlers.
    /// See <see cref="Weekday"/> for its name.
    /// </summary>
    public static int WeekdayNumber => Data?.CurrentDayNumber ?? 0;

    /// <summary>Today's name in the game's six day week, such as Gluttony, or null at the main menu.</summary>
    /// <example>
    /// <code>
    /// if (TimbnClock.Weekday == TimbnWeekday.Pride)
    ///     Logger.LogInfo("The town market is open.");
    /// </code>
    /// </example>
    public static TimbnWeekday? Weekday
    {
        get
        {
            if (Data is not { } data)
                return null;

            foreach (var (day, id) in _weekdayIds)
            {
                if (ConstDef.Get(id)?.IntValue == data.CurrentDayNumber)
                    return day;
            }

            return null;
        }
    }

    /// <summary>
    /// The time of day, running from 0 at midnight through 0.25 at dawn, 0.5 at noon and 0.75 at dusk, back to 1
    /// at the next midnight.
    /// </summary>
    public static float TimeOfDay => Data?.TimeOfDay ?? 0f;

    /// <summary>The time of day as a 24 hour clock, such as 06:30.</summary>
    public static string Time
    {
        get
        {
            var minutes = Mathf.RoundToInt(TimeOfDay * 24f * 60f) % 1440;
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes / 60, minutes % 60);
        }
    }

    /// <summary>Whether the game's clock is stopped, as it is during some cutscenes or while a plugin holds Clock.Pause.</summary>
    public static bool IsPaused => TimbnGame.IsInGame && EnvironmentEngine.Instance != null && EnvironmentEngine.Instance.IsPaused;

    /// <summary>
    /// How far the clock moved from <paramref name="from"/> to <paramref name="to"/>, as a fraction of a day. It
    /// counts across midnight, so 0.9 to 0.1 is 0.2. Useful with TimeOfDayChanged to work out how much game time
    /// passed since the last tick.
    /// </summary>
    /// <param name="from">The earlier time of day.</param>
    /// <param name="to">The later time of day.</param>
    /// <returns>The part of a day between the two, from 0 up to but not including 1.</returns>
    public static float Between(float from, float to) => Mathf.Repeat(to - from, 1f);

    /// <summary>
    /// Sets the time of day, the way the game does when a cutscene jumps the clock. The lighting and everything
    /// that follows the clock update at once. The day does not change, so setting a time earlier than now turns the
    /// clock back within the same day.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnClock.SetTime(0.25f);
    /// </code>
    /// </example>
    /// <param name="timeOfDay">The new time, from 0 at midnight to 1 at the next midnight. Values outside wrap around.</param>
    /// <returns>False at the main menu, when there is no clock to set.</returns>
    public static bool SetTime(float timeOfDay)
    {
        if (!TimbnGame.IsInGame || EnvironmentEngine.Instance == null)
            return false;

        EnvironmentEngine.Instance.SetTimeOfDay(Mathf.Repeat(timeOfDay, 1f));
        return true;
    }

    internal static string WeekdayId(TimbnWeekday day) => _weekdayIds.First(pair => pair.Day == day).Id;
}
