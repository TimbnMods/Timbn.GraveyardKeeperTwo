namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// How big a page's popup is along one side, a number of the game's UI units (the pixels its controls are drawn in), a
/// share of the screen, or whatever its content needs.
/// </summary>
/// <example>
/// <code>
/// UI.ShowPage("Rename", build, width: TimbnLength.Pixels(180));
/// </code>
/// </example>
public readonly struct TimbnLength
{
    private TimbnLength(TimbnLengthKind kind, float value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>Whatever the content needs for a height, and the page's default share for a width.</summary>
    public static TimbnLength Auto => default;

    /// <summary>A fixed size in the game's UI units, which is the room inside the frame and not counting it.</summary>
    public static TimbnLength Pixels(float units) => new(TimbnLengthKind.Pixels, units);

    /// <summary>A share of the screen's size from 10 to 100, taken by the whole window with its frame.</summary>
    public static TimbnLength Percent(float percent) => new(TimbnLengthKind.Percent, percent);

    internal TimbnLengthKind Kind { get; }

    internal float Value { get; }

    /// <summary>Whether this is <see cref="Auto"/>.</summary>
    public bool IsAuto => Kind == TimbnLengthKind.Auto;
}

internal enum TimbnLengthKind
{
    Auto,
    Pixels,
    Percent,
}
