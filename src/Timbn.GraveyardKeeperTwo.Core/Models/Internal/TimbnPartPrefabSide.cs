namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One facing of a part prefab, with the model it draws and the shape of its colliders, footprint and dock spot. A
/// mirrored facing draws the same model as the facing it flips, which the game flips for it.
/// </summary>
internal sealed class TimbnPartPrefabSide
{
    internal TimbnPartPrefabSide(TimbnModel model, TimbnPartShape shape, TimbnFacing facing = TimbnFacing.Down, TimbnFacing? mirrorOf = null,
        IReadOnlyList<TimbnStationOverlay>? overlays = null, IReadOnlyList<TimbnStationSlot>? slots = null)
    {
        Model = model;
        Shape = shape;
        Facing = facing;
        MirrorOf = mirrorOf;
        Overlays = overlays ?? [];
        Slots = slots ?? [];
    }

    // The extra models this facing shows under a condition. A mirrored facing has none of its own, since it draws its source's.
    internal IReadOnlyList<TimbnStationOverlay> Overlays { get; }

    // The slots built into this facing. A mirrored facing has none of its own, since the game flips its source's.
    internal IReadOnlyList<TimbnStationSlot> Slots { get; }

    internal TimbnModel Model { get; }

    internal TimbnPartShape Shape { get; }

    internal TimbnFacing Facing { get; }

    internal TimbnFacing? MirrorOf { get; }

    internal int RotationIndex => (int)Facing;
}
