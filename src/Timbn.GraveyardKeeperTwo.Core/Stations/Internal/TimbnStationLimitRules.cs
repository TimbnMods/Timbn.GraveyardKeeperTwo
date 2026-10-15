namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The expressions behind a station's build limit, kept free of game statics so they can be tested. The place entry's
/// current limit expression counts the station and its construction sites standing in the world, through Core's own
/// TimbnCount expression function, so the count stays right whatever took one away.
/// </summary>
internal static class TimbnStationLimitRules
{
    internal const string CountFunction = "TimbnCount";

    internal static void Validate(int max)
    {
        if (max < 1)
            throw new ArgumentOutOfRangeException(nameof(max), $"A build limit has to be at least 1. Got {max}.");
    }

    internal static string CurrentExpression(params string[] objectIds) =>
        $"{CountFunction}({string.Join(", ", objectIds.Select(id => $"\"{id}\""))})";
}
