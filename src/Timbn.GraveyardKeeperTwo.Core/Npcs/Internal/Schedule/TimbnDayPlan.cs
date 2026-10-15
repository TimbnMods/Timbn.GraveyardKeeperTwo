namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Works out which block of an NPC's day the clock is in, with no game state, so it can be tested on its own.</summary>
internal static class TimbnDayPlan
{
    internal const double MinutesPerDay = 1440.0;

    internal static double Minutes(int day, float timeOfDay) => (day + (double)timeOfDay) * MinutesPerDay;

    internal static int BlockAt(IReadOnlyList<float> starts, float timeOfDay)
    {
        var index = -1;
        var best = float.MinValue;
        var latest = -1;
        for (var i = 0; i < starts.Count; i++)
        {
            if (starts[i] <= timeOfDay && starts[i] > best)
            {
                best = starts[i];
                index = i;
            }

            if (latest < 0 || starts[i] > starts[latest])
                latest = i;
        }

        return index >= 0 ? index : latest;
    }

    internal static double MinutesUntil(float start, float timeOfDay)
    {
        var left = (start - timeOfDay + 1f) % 1f;
        return (left == 0f ? 1f : left) * MinutesPerDay;
    }

    internal static double LastStart(IReadOnlyList<float> starts, int day, float timeOfDay)
    {
        var start = starts[BlockAt(starts, timeOfDay)];
        return Minutes(start <= timeOfDay ? day : day - 1, start);
    }

    internal static bool IsTakeBackDue(IReadOnlyList<float> starts, int day, float timeOfDay, ref double leftAt)
    {
        var now = Minutes(day, timeOfDay);
        if (now < leftAt)
            leftAt = now;

        var started = LastStart(starts, day, timeOfDay);
        if (started <= leftAt)
            return false;

        leftAt = started;
        return true;
    }
}
