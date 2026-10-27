using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Picks how many decimal places a slider steps by from the numbers an author wrote, so a range like 3.2 to 5 steps by 0.1
/// and a range like 1 to 18 steps by 1, and how big a step is once a range is too long to click through one place at a time.
/// </summary>
public static class TimbnSliderSteps
{
    private const int _maxDecimals = 4;
    private const float _fewestSteps = 10f;
    private const float _mostSteps = 200f;
    private const float _largestExact = 1e15f;

    /// <summary>
    /// Zero for whole numbers, otherwise the most decimal places any of the numbers use, with more added when that would
    /// leave the range fewer than ten steps (0 to 1 steps by 0.1, not by 1). A range of one value or a backwards one adds none.
    /// </summary>
    public static int Decimals(bool wholeNumbers, float min, float max, params float[] others)
    {
        if (wholeNumbers)
            return 0;

        var decimals = others.Append(min).Append(max).Max(DecimalPlaces);
        if (max <= min || !float.IsFinite(max - min))
            return decimals;

        while (decimals < _maxDecimals && (max - min) * Math.Pow(10, decimals) < _fewestSteps)
            decimals++;

        return decimals;
    }

    /// <summary>
    /// The size of one step, one in the last of that many decimal places, made 2, 5 or 10 times larger and so on until the
    /// range takes at most 200 steps, so a range of 0 to 100000 does not take 100000 presses.
    /// </summary>
    public static float StepSize(int decimals, float min, float max)
    {
        var step = (float)Math.Pow(10, -decimals);
        var range = max - min;
        if (!float.IsFinite(range) || range / step <= _mostSteps)
            return step;

        foreach (var power in Enumerable.Range(0, 30))
        {
            foreach (var multiple in new[] { 1f, 2f, 5f })
            {
                var larger = step * multiple * (float)Math.Pow(10, power);
                if (range / larger <= _mostSteps)
                    return larger;
            }
        }

        return step;
    }

    private static int DecimalPlaces(float number)
    {
        // A decimal cannot hold a number this large or one that is not a number, and nobody counts places out there.
        if (!float.IsFinite(number) || Math.Abs(number) >= _largestExact)
            return 0;

        var text = ((decimal)number).ToString("0.####", CultureInfo.InvariantCulture);
        var point = text.IndexOf('.');
        return point < 0 ? 0 : Math.Min(_maxDecimals, text.Length - point - 1);
    }
}
