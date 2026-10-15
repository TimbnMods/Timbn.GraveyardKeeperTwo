namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The kinds of look condition, each one a condition class the game's conditional drawer already has.</summary>
internal enum TimbnLookKind
{
    HoldsItem,
    HoldsGroup,
    Resource,
    Working,
    HasWorker,
    Crafting,
}

/// <summary>One extra model of a station, with when it shows and whether it takes the place of the base model.</summary>
internal sealed class TimbnStationOverlay
{
    internal TimbnStationOverlay(TimbnFacing facing, TimbnModel model, TimbnLookCondition condition, bool replacesBase)
    {
        Facing = facing;
        Model = model;
        Condition = condition;
        ReplacesBase = replacesBase;
    }

    internal TimbnFacing Facing { get; }

    internal TimbnModel Model { get; }

    internal TimbnLookCondition Condition { get; }

    internal bool ReplacesBase { get; }
}

/// <summary>
/// The rules behind a station's extra models, kept free of game statics so they can be tested. Each extra model is
/// switched on or off by a condition the game checks again when something it watches changes, and may switch the
/// station's base model off while it shows.
/// </summary>
internal static class TimbnLookRules
{
    internal static void CheckId(string id, string what)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException($"A look condition needs a {what}.", nameof(id));
    }

    internal static void CheckAtLeast(int atLeast)
    {
        if (atLeast < 1)
            throw new ArgumentOutOfRangeException(nameof(atLeast), $"A look condition that holds items needs at least 1. Got {atLeast}.");
    }

    /// <summary>What the game has to watch for the condition to be checked again.</summary>
    internal static ConditionalEventType EventFor(TimbnLookKind kind) => kind switch
    {
        TimbnLookKind.HoldsItem or TimbnLookKind.HoldsGroup => ConditionalEventType.ItemsChanged,
        TimbnLookKind.Resource => ConditionalEventType.GameResChanged,
        TimbnLookKind.Crafting => ConditionalEventType.CraftStatusChanged,
        _ => ConditionalEventType.WorkStateChanged,
    };

    internal static ComparisonOperator OperatorFor(TimbnComparison comparison) => comparison switch
    {
        TimbnComparison.Less => ComparisonOperator.Less,
        TimbnComparison.LessOrEqual => ComparisonOperator.LessOrEqual,
        TimbnComparison.Equal => ComparisonOperator.Equal,
        TimbnComparison.GreaterOrEqual => ComparisonOperator.GreaterOrEqual,
        _ => ComparisonOperator.Greater,
    };

    /// <summary>
    /// Whether the game should show the model when the condition is false instead of true. A model that takes the
    /// base's place is shown in the base's absence, so the base model is switched the opposite way.
    /// </summary>
    internal static bool ShowsWhenFalse(TimbnLookCondition condition, bool forBase) => condition.IsInverted != forBase;

    /// <summary>
    /// How far to shift an extra model so it lines up with the base model. A model built from faces is centered on its
    /// own bounds, and every built model measures from the same corner, so the two line up once the difference between
    /// their centers is taken out.
    /// </summary>
    internal static Vector3 AlignmentOffset(Vector2? overlayCenter, Vector2? baseCenter) =>
        overlayCenter is { } overlay && baseCenter is { } basis
            ? new Vector3((overlay.x - basis.x) / TimbnBuiltModel.PixelsAcross, 0f, (overlay.y - basis.y) / TimbnBuiltModel.PixelsDeep)
            : Vector3.zero;

    /// <summary>
    /// Why the extra models don't fit the facings the station has, or null when they do. An extra model belongs to a
    /// facing that is drawn, since a mirrored facing shows its source's models flipped. Only one model per facing may
    /// replace the base, since two would fight over it.
    /// </summary>
    internal static string? FindProblem(IReadOnlyCollection<(TimbnFacing Facing, bool ReplacesBase)> overlays, IReadOnlyCollection<TimbnFacing> drawn, IReadOnlyDictionary<TimbnFacing, TimbnFacing> mirrored)
    {
        foreach (var (facing, _) in overlays)
        {
            if (mirrored.TryGetValue(facing, out var source))
                return $"A model shown for {facing} has no place, since {facing} is a flipped copy of {source}. Give it to {source} and it shows on both.";

            if (!drawn.Contains(facing))
                return $"A model shown for {facing} has no place, since the station has no {facing} facing. Call {(facing == TimbnFacing.Down ? "Model" : facing.ToString())} first.";
        }

        var doubled = overlays.Where(overlay => overlay.ReplacesBase).GroupBy(overlay => overlay.Facing).FirstOrDefault(group => group.Count() > 1);
        return doubled is null ? null : $"More than one model replaces the base model of {doubled.Key}, and they would fight over it.";
    }
}
