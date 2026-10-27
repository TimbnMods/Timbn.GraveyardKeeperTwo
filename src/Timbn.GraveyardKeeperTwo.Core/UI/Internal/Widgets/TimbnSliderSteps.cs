using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Picks how many decimal places a slider steps by from the numbers an author wrote, so a range like 3.2 to 5 steps by 0.1
/// and a range like 1 to 18 steps by 1.
/// </summary>
public static class TimbnSliderSteps
{
    private const int _maxDecimals = 4;
    private const float _fewestSteps = 10f;

    /// <summary>
    /// Zero for whole numbers, otherwise the most decimal places any of the numbers use, with more added when that would
    /// leave the range fewer than ten steps (0 to 1 steps by 0.1, not by 1).
    /// </summary>
    public static int Decimals(bool wholeNumbers, float min, float max, params float[] others)
    {
        if (wholeNumbers)
            return 0;

        var decimals = others.Append(min).Append(max).Max(DecimalPlaces);
        while (decimals < _maxDecimals && (max - min) * Math.Pow(10, decimals) < _fewestSteps)
            decimals++;

        return decimals;
    }

    /// <summary>The size of one step for that many decimal places.</summary>
    public static float Step(int decimals) => (float)Math.Pow(10, -decimals);

    private static int DecimalPlaces(float number)
    {
        var text = ((decimal)number).ToString("0.####", CultureInfo.InvariantCulture);
        var point = text.IndexOf('.');
        return point < 0 ? 0 : Math.Min(_maxDecimals, text.Length - point - 1);
    }
}
