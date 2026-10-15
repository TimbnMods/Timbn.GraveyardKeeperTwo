namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>How a station's value is compared with the number a look condition names.</summary>
public enum TimbnComparison
{
    /// <summary>Below the number.</summary>
    Less,

    /// <summary>At or below the number.</summary>
    LessOrEqual,

    /// <summary>Exactly the number.</summary>
    Equal,

    /// <summary>At or above the number.</summary>
    GreaterOrEqual,

    /// <summary>Above the number.</summary>
    Greater,
}

/// <summary>
/// When one of a station's extra models shows, for <see cref="TimbnStationBuilder.ShowWhen(TimbnLookCondition, TimbnModel, bool)"/>.
/// Build one with <see cref="TimbnLook"/>. The game checks it again each time the station's items, resources, worker or
/// work change, so the model comes and goes by itself.
/// </summary>
public sealed class TimbnLookCondition
{
    internal TimbnLookCondition(TimbnLookKind kind, string id, TimbnComparison comparison, int value, bool inverted)
    {
        Kind = kind;
        Id = id;
        Comparison = comparison;
        Value = value;
        IsInverted = inverted;
    }

    internal TimbnLookKind Kind { get; }

    internal string Id { get; }

    internal TimbnComparison Comparison { get; }

    internal int Value { get; }

    internal bool IsInverted { get; }

    /// <summary>The opposite condition, true exactly when this one is false.</summary>
    public TimbnLookCondition Not() => new(Kind, Id, Comparison, Value, !IsInverted);
}

/// <summary>
/// Builds the conditions for a station's extra models. They use the game's own conditional drawer, the way the autopsy
/// table shows its cloth when it holds a body. Pass one to <see cref="TimbnStationBuilder.ShowWhen(TimbnLookCondition, TimbnModel, bool)"/>.
/// </summary>
public static class TimbnLook
{
    /// <summary>True while the station's own inventory holds at least this many of an item.</summary>
    /// <param name="itemId">The item's id, for example "body_zombie".</param>
    /// <param name="atLeast">How many, 1 or more.</param>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The count is below 1.</exception>
    public static TimbnLookCondition HoldsItem(string itemId, int atLeast = 1)
    {
        TimbnLookRules.CheckId(itemId, "item id");
        TimbnLookRules.CheckAtLeast(atLeast);
        return new(TimbnLookKind.HoldsItem, itemId, TimbnComparison.GreaterOrEqual, atLeast, false);
    }

    /// <summary>
    /// True while the station's own inventory holds at least this many items of an item group, such as "body", which
    /// every body and zombie item is in.
    /// </summary>
    /// <param name="groupId">The item group's id.</param>
    /// <param name="atLeast">How many, 1 or more.</param>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The count is below 1.</exception>
    public static TimbnLookCondition HoldsGroup(string groupId, int atLeast = 1)
    {
        TimbnLookRules.CheckId(groupId, "group id");
        TimbnLookRules.CheckAtLeast(atLeast);
        return new(TimbnLookKind.HoldsGroup, groupId, TimbnComparison.GreaterOrEqual, atLeast, false);
    }

    /// <summary>True while one of the station's own game resources compares as asked, for a value a mod keeps on the station with SetGameRes. A resource that was never set counts as 0.</summary>
    /// <param name="resourceId">The resource's id.</param>
    /// <param name="comparison">How the resource is compared with the value.</param>
    /// <param name="value">The number to compare with.</param>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    public static TimbnLookCondition Resource(string resourceId, TimbnComparison comparison, int value)
    {
        TimbnLookRules.CheckId(resourceId, "resource id");
        return new(TimbnLookKind.Resource, resourceId, comparison, value, false);
    }

    /// <summary>True while someone is working the station, the player at its work or a zombie at its craft.</summary>
    public static TimbnLookCondition IsWorking() => new(TimbnLookKind.Working, "", TimbnComparison.Equal, 0, false);

    /// <summary>
    /// True while a craft runs on the station, whoever or whatever runs it. The game's resurrection table shows its
    /// working effects this way.
    /// </summary>
    public static TimbnLookCondition IsCrafting() => new(TimbnLookKind.Crafting, "", TimbnComparison.Equal, 0, false);

    /// <summary>True while the station has a worker, the player or a zombie, whether or not the work is running.</summary>
    public static TimbnLookCondition HasWorker() => new(TimbnLookKind.HasWorker, "", TimbnComparison.Equal, 0, false);
}
