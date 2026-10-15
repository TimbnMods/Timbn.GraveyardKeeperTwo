namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The dice for a person's daily choices. They give the same numbers for the same key and the same game minute or
/// day, so a reload or a second look at the same moment rolls alike, and they are well mixed, so neighboring minutes,
/// days and people don't roll alike.
/// </summary>
/// <example>
/// <code>
/// var random = TimbnRolls.ThisMinute(TimbnGameIds.Npcs.Herm);
/// if (random.NextDouble() &lt; 0.75)
///     GoForAWalk();
/// </code>
/// </example>
public static class TimbnRolls
{
    private const ulong _golden = 0x9E3779B97F4A7C15UL;

    /// <summary>Dice for a key, such as a person's id, that give the same numbers all through this game minute.</summary>
    /// <param name="key">What is rolling, such as <see cref="TimbnGameIds.Npcs.Herm"/>.</param>
    /// <returns>A new random source seeded from the key and the minute.</returns>
    public static System.Random ThisMinute(string key) => Seeded(key, (long)TimbnClock.Minutes);

    /// <summary>Dice for a key that give the same numbers all through this game day, such as for picking one thing a day.</summary>
    /// <param name="key">What is rolling, such as <see cref="TimbnGameIds.Npcs.Herm"/>.</param>
    /// <returns>A new random source seeded from the key and the day.</returns>
    public static System.Random ThisDay(string key) => Seeded(key + "/day", TimbnClock.Day);

    internal static System.Random Seeded(string key, long when)
    {
        var mixed = Mix(Mix(StableHash(key)) + unchecked((ulong)when * _golden));
        return new System.Random((int)(mixed & int.MaxValue));
    }

    private static ulong StableHash(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var letter in text)
            hash = unchecked((hash ^ letter) * 1099511628211UL);

        return hash;
    }

    private static ulong Mix(ulong value)
    {
        unchecked
        {
            value += _golden;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
