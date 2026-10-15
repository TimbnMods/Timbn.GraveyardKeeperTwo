namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One facing of a rotatable station, with the model it draws, its footprint size and an optional construction site. A
/// mirrored side points at the facing it flips and shares its model and site.
/// </summary>
internal sealed class TimbnSide<TModel> where TModel : class
{
    internal TimbnSide(TimbnFacing facing, TModel? model, Vector3 size, TimbnFacing? mirrorOf = null)
    {
        Facing = facing;
        Model = model;
        Size = size;
        MirrorOf = mirrorOf;
    }

    internal TimbnFacing Facing { get; }

    internal int RotationIndex => (int)Facing;

    internal TModel? Model { get; set; }

    internal Vector3 Size { get; set; }

    internal TModel? Site { get; set; }

    internal TimbnFacing? MirrorOf { get; }

    internal bool IsMirrored => MirrorOf != null;
}

/// <summary>
/// The sides a station builder collects, kept free of game statics so the rules can be tested. A station has a slot for
/// each facing and always has Down, its own model. The others are optional and come out in the order R turns through
/// them, Down then Left then Up then Right, each keeping its facing's rotation index whether or not the ones before it
/// exist. Mirroring happens once, when the sides are resolved, and fills a missing Left from Right or a missing Right
/// from Left with a horizontally flipped copy. Down and Up are never mirrored.
/// </summary>
internal sealed class TimbnStationSides<TModel> where TModel : class
{
    private readonly TimbnSide<TModel>[] _slots =
    [
        new(TimbnFacing.Down, null, Vector3.zero),
        new(TimbnFacing.Left, null, Vector3.zero),
        new(TimbnFacing.Up, null, Vector3.zero),
        new(TimbnFacing.Right, null, Vector3.zero),
    ];

    internal bool HasModel => _slots[(int)TimbnFacing.Down].Model != null;

    internal int DrawnCount => _slots.Count(slot => slot.Model != null);

    internal static TimbnFacing? Opposite(TimbnFacing facing) => facing switch
    {
        TimbnFacing.Left => TimbnFacing.Right,
        TimbnFacing.Right => TimbnFacing.Left,
        _ => null,
    };

    internal void Set(TimbnFacing facing, TModel model, Vector3 size)
    {
        CheckSize(size);
        var slot = _slots[(int)facing];
        slot.Model = model;
        slot.Size = size;
    }

    internal void SetSite(TimbnFacing facing, TModel site) => _slots[(int)facing].Site = site;

    internal IReadOnlyList<TimbnSide<TModel>> Resolve(bool mirror)
    {
        if (!HasModel)
            throw new InvalidOperationException("A station needs a model. Call Model on the builder.");

        List<TimbnSide<TModel>> sides = [];
        foreach (var slot in _slots)
        {
            if (slot.Model != null)
            {
                sides.Add(slot);
            }
            else if (mirror && Opposite(slot.Facing) is { } source && _slots[(int)source].Model != null)
            {
                var original = _slots[(int)source];
                sides.Add(new TimbnSide<TModel>(slot.Facing, original.Model, original.Size, source) { Site = original.Site });
            }
        }

        return sides;
    }

    private static void CheckSize(Vector3 size)
    {
        if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
            throw new ArgumentOutOfRangeException(nameof(size), $"A side's size can't be zero or below on any axis. Got {size}.");
    }
}
