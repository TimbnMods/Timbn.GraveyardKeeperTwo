namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// How big a line made with <see cref="TimbnText"/> is. The game's font is drawn at 16 points with a line
/// that is 12 units tall, and every size here is a step of 4 points, so a line is a whole number of units tall (6, 9, 12,
/// 15, 18 and 21) and a line of text is exactly as tall as its font.
/// </summary>
public enum TimbnTextSize
{
    /// <summary>8 points, a 6 unit line.</summary>
    ExtraSmall = 8,

    /// <summary>12 points, a 9 unit line.</summary>
    Small = 12,

    /// <summary>16 points, a 12 unit line, which is what the game's own text is.</summary>
    Normal = 16,

    /// <summary>20 points, a 15 unit line.</summary>
    Large = 20,

    /// <summary>24 points, an 18 unit line.</summary>
    ExtraLarge = 24,

    /// <summary>28 points, a 21 unit line.</summary>
    ExtraExtraLarge = 28,
}
