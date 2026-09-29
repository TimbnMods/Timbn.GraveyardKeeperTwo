using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads the game's calendar and clock for the loaded save, so a mod can ask what day or time it is without
/// digging through EnvironmentEngine and the save. Every member reads as zero or empty at the main menu.
/// </summary>
public static class TimbnClock
{
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

            foreach (TimbnWeekday day in Enum.GetValues(typeof(TimbnWeekday)))
            {
                if (ConstDef.Get(WeekdayId(day))?.IntValue == data.CurrentDayNumber)
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

    /// <summary>
    /// How far the clock moved from <paramref name="from"/> to <paramref name="to"/>, as a fraction of a day. It
    /// counts across midnight, so 0.9 to 0.1 is 0.2. Useful with TimeOfDayChanged to work out how much game time
    /// passed since the last tick.
    /// </summary>
    /// <param name="from">The earlier time of day.</param>
    /// <param name="to">The later time of day.</param>
    /// <returns>The part of a day between the two, from 0 up to but not including 1.</returns>
    public static float Between(float from, float to) => Mathf.Repeat(to - from, 1f);

    internal static string WeekdayId(TimbnWeekday day) => $"day_{day.ToString().ToLowerInvariant()}";
}
