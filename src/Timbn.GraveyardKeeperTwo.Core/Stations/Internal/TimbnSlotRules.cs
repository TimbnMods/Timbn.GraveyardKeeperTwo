namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>One slot built into a station's facing, where a station or extension that builds into it goes.</summary>
internal sealed class TimbnStationSlot
{
    internal TimbnStationSlot(TimbnFacing facing, string id, Vector3 position, Vector3 size)
    {
        Facing = facing;
        Id = id;
        Position = position;
        Size = size;
    }

    internal TimbnFacing Facing { get; }

    internal string Id { get; }

    internal Vector3 Position { get; }

    internal Vector3 Size { get; }
}

/// <summary>
/// The rules behind slots built into a station, kept free of game statics so they can be tested. A slot is the game's
/// own BuildArea, and a station whose build entry names it only goes down where it covers a whole one, the way the
/// conveyor furnace takes its bellows. The game snaps both to its build grid, so a slot has to be whole cells and sit a
/// whole number of cells from the corner of the station's footprint.
/// </summary>
internal static class TimbnSlotRules
{
    // The game's BuildConsts.CELL_SIZE, 16 by 12 pixels.
    internal const float CellAcross = 0.32f;
    internal const float CellDeep = 0.3f;

    // The game's own slots are flat boxes this tall.
    internal const float AreaHeight = 0.01f;

    private const float _tolerance = 0.01f;

    internal static void CheckId(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("A slot needs an id.", nameof(slotId));
    }

    internal static void CheckSize(Vector3 size)
    {
        if (size.x <= 0f || size.z <= 0f)
            throw new ArgumentOutOfRangeException(nameof(size), $"A slot needs a width and depth above zero. Got {size}.");
    }

    /// <summary>
    /// Why the slots don't fit the station, or null when they do. A slot belongs to a drawn facing, since a mirrored
    /// facing gets its source's slots flipped, and has to land on the build grid on every facing that shows it.
    /// </summary>
    internal static string? FindProblem<TModel>(IReadOnlyCollection<TimbnStationSlot> slots, IReadOnlyList<TimbnSide<TModel>> sides) where TModel : class
    {
        foreach (var slot in slots)
        {
            var side = sides.FirstOrDefault(candidate => candidate.Facing == slot.Facing);
            if (side is null)
                return $"Slot {slot.Id} has no place, since the station has no {slot.Facing} facing. Call {(slot.Facing == TimbnFacing.Down ? "Model" : slot.Facing.ToString())} first.";

            if (side.MirrorOf is { } source)
                return $"Slot {slot.Id} for {slot.Facing} has no place, since {slot.Facing} is a flipped copy of {source}. Give it to {source} and it shows on both.";

            if (!IsWhole(slot.Size.x, CellAcross) || !IsWhole(slot.Size.z, CellDeep))
                return $"Slot {slot.Id} is {slot.Size.x:0.###} by {slot.Size.z:0.###}, which isn't whole build cells. Make it a multiple of {CellAcross} across and {CellDeep} deep, the same as the station that goes in it.";

            if (!IsWhole(Corner(slot.Position.x, slot.Size.x, side.Size.x), CellAcross) || !IsWhole(Corner(slot.Position.z, slot.Size.z, side.Size.z), CellDeep))
            {
                var near = Snap(slot.Position, slot.Size, side.Size);
                return $"Slot {slot.Id} at ({slot.Position.x:0.###}, {slot.Position.z:0.###}) is off the build grid, since its corner has to sit a multiple of {CellAcross} across and {CellDeep} deep from the corner of the station's footprint. The nearest spot is ({near.x:0.###}, {near.z:0.###}).";
            }

            var flipped = sides.FirstOrDefault(candidate => candidate.MirrorOf == slot.Facing);
            if (flipped != null && !IsWhole(side.Size.x, CellAcross))
                return $"Slot {slot.Id} lands off the build grid on the flipped {flipped.Facing} facing, since {slot.Facing} is {side.Size.x:0.###} wide. Make its width a multiple of {CellAcross}.";
        }

        return null;
    }

    internal static bool IsWhole(float length, float cell)
    {
        var cells = length / cell;
        return Math.Abs(cells - Math.Round(cells)) < _tolerance;
    }

    /// <summary>The nearest position whose slot corner is on the build grid, for the message that refuses one.</summary>
    internal static Vector3 Snap(Vector3 position, Vector3 size, Vector3 footprint) =>
        new(SnapAxis(position.x, size.x, footprint.x, CellAcross), position.y, SnapAxis(position.z, size.z, footprint.z, CellDeep));

    private static float Corner(float center, float size, float footprint) => center - size / 2f + footprint / 2f;

    private static float SnapAxis(float center, float size, float footprint, float cell) =>
        (float)Math.Round(Corner(center, size, footprint) / cell) * cell - footprint / 2f + size / 2f;
}
