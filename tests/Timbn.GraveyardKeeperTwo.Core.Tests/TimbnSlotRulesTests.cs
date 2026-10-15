using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSlotRulesTests
{
    private static readonly Vector3 _table = new(1.28f, 0.8f, 0.6f);
    private static readonly Vector3 _stand = new(0.64f, 1f, 0.6f);

    private static IReadOnlyList<TimbnSide<string>> Sides(Vector3 down, bool mirroredLeftAndRight = false)
    {
        var sides = new TimbnStationSides<string>();
        sides.Set(TimbnFacing.Down, "down", down);
        if (mirroredLeftAndRight)
            sides.Set(TimbnFacing.Left, "left", down);

        return sides.Resolve(mirroredLeftAndRight);
    }

    private static TimbnStationSlot Slot(float x, float z, Vector3? size = null, TimbnFacing facing = TimbnFacing.Down) =>
        new(facing, "timbn_stand_slot", new Vector3(x, 0f, z), size ?? _stand);

    [Fact]
    public void ASlotNeedsAnIdAndAWidthAndDepth()
    {
        TimbnStationBuilder station = new("timbn_table", "", "");

        Assert.Throws<ArgumentException>(() => station.Slot(" ", Vector3.zero, _stand));
        Assert.Throws<ArgumentOutOfRangeException>(() => station.Slot("timbn_stand_slot", Vector3.zero, new Vector3(0f, 1f, 0.6f)));
        Assert.Throws<ArgumentOutOfRangeException>(() => station.Slot("timbn_stand_slot", Vector3.zero, new Vector3(0.64f, 1f, -0.3f)));
        Assert.Throws<ArgumentException>(() => station.BuildsInSlot(""));
        Assert.Throws<ArgumentException>(() => new TimbnExtensionBuilder("timbn_rack", "", "").BuildsInSlot(""));
    }

    [Fact]
    public void ASlotWithAFlatSizeIsTaken()
    {
        TimbnStationBuilder station = new("timbn_table", "", "");

        station.Slot("timbn_stand_slot", new Vector3(-0.96f, 0f, 0f), new Vector3(0.64f, 0f, 0.6f)).BuildsInSlot("timbn_other_slot");

        Assert.Single(station.Slots);
        Assert.Equal(TimbnFacing.Down, station.Slots[0].Facing);
        Assert.Equal("timbn_other_slot", station.InSlotId);
    }

    [Fact]
    public void SlotsOnTheGridFit()
    {
        // Beside the table on both sides, in front of it, and inside its footprint.
        Assert.Null(TimbnSlotRules.FindProblem([Slot(-0.96f, 0f), Slot(0.96f, 0f), Slot(-0.32f, -0.6f), Slot(0.32f, 0f)], Sides(_table)));
        Assert.Null(TimbnSlotRules.FindProblem([], Sides(_table)));
    }

    [Fact]
    public void ASlotOffTheGridIsRefusedWithTheNearestSpot()
    {
        var problem = TimbnSlotRules.FindProblem([Slot(-0.9f, 0f)], Sides(_table));

        Assert.Contains("off the build grid", problem);
        Assert.Contains("(-0.96, 0)", problem);
    }

    [Fact]
    public void ASlotThatIsNotWholeCellsIsRefused()
    {
        var problem = TimbnSlotRules.FindProblem([Slot(-0.96f, 0f, new Vector3(0.5f, 1f, 0.6f))], Sides(_table));

        Assert.Contains("whole build cells", problem);
    }

    [Fact]
    public void ASlotForAMissingOrMirroredFacingIsRefused()
    {
        Assert.Contains("no Up facing", TimbnSlotRules.FindProblem([Slot(0f, 0.75f, facing: TimbnFacing.Up)], Sides(_table)));

        var mirrored = TimbnSlotRules.FindProblem([Slot(0f, 0.75f, facing: TimbnFacing.Right)], Sides(_table, mirroredLeftAndRight: true));
        Assert.Contains("flipped copy of Left", mirrored);
    }

    [Fact]
    public void AFlippedFacingNeedsAWholeCellWidth()
    {
        var narrow = new Vector3(1f, 0.8f, 0.6f);

        var problem = TimbnSlotRules.FindProblem([Slot(-0.82f, 0f, facing: TimbnFacing.Left)], Sides(narrow, mirroredLeftAndRight: true));

        Assert.Contains("flipped Right", problem);
        Assert.Null(TimbnSlotRules.FindProblem([Slot(-0.82f, 0f)], Sides(narrow)));
    }

    [Theory]
    [InlineData(0.64f, true)]
    [InlineData(0.96f, true)]
    [InlineData(0.6f, false)]
    [InlineData(0f, true)]
    [InlineData(-0.32f, true)]
    public void WholeCellsAllowForFloatError(float length, bool whole)
    {
        Assert.Equal(whole, TimbnSlotRules.IsWhole(length, TimbnSlotRules.CellAcross));
    }
}
