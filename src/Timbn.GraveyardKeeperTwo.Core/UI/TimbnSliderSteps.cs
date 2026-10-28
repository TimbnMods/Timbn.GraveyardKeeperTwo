using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Picks how big one slider step is. Decimals in the numbers an author wrote set it, so a range like 3.2 to 5 steps by 0.1.
/// A range with whole ends steps by a round number (1, 5, 10, 50 and so on, or 0.5, 0.1, 0.05 and 0.01 below one) chosen
/// to give at least 100 steps, so 0 to 1 steps by 0.01 and 0 to 100 steps by 1.
/// </summary>
public static class TimbnSliderSteps
{
    /// <summary>The fewest steps a range with whole ends is split into.</summary>
    public const int LeastSteps = 100;

    private const int _maxDecimals = 4;
    private const float _largestExact = 1e15f;

    /// <summary>
    /// Zero for whole numbers, otherwise the most decimal places any of the numbers use, such as the minimum, the maximum
    /// and a setting's default. A result of zero means the author gave no decimals, so <see cref="StepSize"/> picks a
    /// round step from the range instead.
    /// </summary>
    public static int Decimals(bool wholeNumbers, float min, float max, params float[] others) =>
        wholeNumbers ? 0 : others.Append(min).Append(max).Max(DecimalPlaces);

    /// <summary>
    /// The size of one step. Whole numbers step by 1, and any decimals step by one in the last of that many places. With
    /// neither it is the largest of 1, 5, 10, 50 and so on (or 0.5, 0.1, 0.05 and 0.01) that still gives
    /// <see cref="LeastSteps"/> steps over the range.
    /// </summary>
    public static float StepSize(bool wholeNumbers, int decimals, float min, float max)
    {
        if (wholeNumbers)
            return 1f;

        if (decimals > 0)
            return (float)Math.Pow(10, -decimals);

        var range = (double)max - min;
        if (!double.IsFinite(range) || range <= 0)
            return 1f;

        var target = range / LeastSteps;
        var power = Math.Pow(10, Math.Floor(Math.Log10(target) + 1e-9));
        return (float)(target >= 5 * power * (1 - 1e-9) ? 5 * power : power);
    }

    /// <summary>How many decimal places a step of this size has, such as 2 for 0.05 and 0 for 500.</summary>
    public static int DecimalPlaces(float number)
    {
        // A decimal cannot hold a number this large or one that is not a number, and nobody counts places out there.
        if (!float.IsFinite(number) || Math.Abs(number) >= _largestExact)
            return 0;

        var text = ((decimal)number).ToString("0.####", CultureInfo.InvariantCulture);
        var point = text.IndexOf('.');
        return point < 0 ? 0 : Math.Min(_maxDecimals, text.Length - point - 1);
    }
}
