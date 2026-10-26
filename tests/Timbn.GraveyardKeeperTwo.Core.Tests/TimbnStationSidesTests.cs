using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnStationSidesTests
{
    private static readonly Vector3 _wide = new(1.2f, 1.4f, 0.9f);
    private static readonly Vector3 _tall = new(0.9f, 1.4f, 1.5f);

    private static TimbnStationSides<string> Sides() => new();

    private static TimbnStationSides<string> DownAndLeft()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Down, "down", _wide);
        sides.Set(TimbnFacing.Left, "left", _tall);
        return sides;
    }

    [Fact]
    public void SidesComeOutInTheOrderRTurnsThroughThemWhateverOrderTheyWereGiven()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Right, "right", _tall);
        sides.Set(TimbnFacing.Up, "up", _wide);
        sides.Set(TimbnFacing.Left, "left", _tall);
        sides.Set(TimbnFacing.Down, "down", _wide);

        Assert.Equal(["down", "left", "up", "right"], sides.Resolve(false).Select(side => side.Model));
        Assert.Equal([0, 1, 2, 3], sides.Resolve(false).Select(side => side.RotationIndex));

        var gaps = Sides();
        gaps.Set(TimbnFacing.Down, "down", _wide);
        gaps.Set(TimbnFacing.Right, "right", _tall);

        Assert.Equal([0, 3], gaps.Resolve(false).Select(side => side.RotationIndex));
    }

    [Fact]
    public void MirroringDownAndLeftGivesDownLeftAndAFlippedLeftFacingRight()
    {
        var resolved = DownAndLeft().Resolve(true);

        Assert.Equal([TimbnFacing.Down, TimbnFacing.Left, TimbnFacing.Right], resolved.Select(side => side.Facing));
        Assert.Equal(["down", "left", "left"], resolved.Select(side => side.Model));
        Assert.Equal([false, false, true], resolved.Select(side => side.IsMirrored));
        Assert.Equal(TimbnFacing.Left, resolved[2].MirrorOf);
        Assert.Equal(3, resolved[2].RotationIndex);
        Assert.Equal(_tall, resolved[2].Size);

        var withSite = DownAndLeft();
        withSite.SetSite(TimbnFacing.Left, "leftSite");

        Assert.Equal("leftSite", withSite.Resolve(true)[2].Site);
    }

    [Fact]
    public void MirroringFillsLeftFromRightToo()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Down, "down", _wide);
        sides.Set(TimbnFacing.Right, "right", _tall);

        var resolved = sides.Resolve(true);

        Assert.Equal([TimbnFacing.Down, TimbnFacing.Left, TimbnFacing.Right], resolved.Select(side => side.Facing));
        Assert.Equal(TimbnFacing.Right, resolved[1].MirrorOf);
        Assert.Equal("right", resolved[1].Model);
    }

    [Fact]
    public void ALeftAndRightThatAreBothGivenAreLeftAlone()
    {
        var sides = DownAndLeft();
        sides.Set(TimbnFacing.Right, "right", _tall);

        var resolved = sides.Resolve(true);

        Assert.Equal(["down", "left", "right"], resolved.Select(side => side.Model));
        Assert.All(resolved, side => Assert.False(side.IsMirrored));

        var downAndUp = Sides();
        downAndUp.Set(TimbnFacing.Down, "down", _wide);
        downAndUp.Set(TimbnFacing.Up, "up", _wide);

        Assert.Equal([TimbnFacing.Down, TimbnFacing.Up], downAndUp.Resolve(true).Select(side => side.Facing));

        var downOnly = Sides();
        downOnly.Set(TimbnFacing.Down, "down", _wide);

        Assert.Single(downOnly.Resolve(true));
    }

    [Fact]
    public void AMirroredSidesDockOffsetIsItsSourcesWithXFlipped()
    {
        var resolved = DownAndLeft().Resolve(true);
        var source = TimbnPartShape.DockOffset(resolved[1].Size, 0.2f, resolved[1].Facing);
        var mirrored = TimbnPartShape.DockOffset(resolved[2].Size, 0.2f, resolved[2].Facing);

        Assert.Equal(new Vector3(-source.x, source.y, source.z), mirrored);

        var sourceRect = TimbnPartShape.Station(resolved[1].Size, 0.2f, resolved[1].Facing).FootprintRect;
        var mirroredRect = TimbnPartShape.Station(resolved[2].Size, 0.2f, resolved[2].Facing).FootprintRect;

        Assert.Equal(sourceRect, mirroredRect);
    }
}
