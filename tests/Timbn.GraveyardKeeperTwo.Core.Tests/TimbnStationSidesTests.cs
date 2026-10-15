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
    public void ASideBuilderStartsWithNoModelAndNoSides()
    {
        var sides = Sides();

        Assert.False(sides.HasModel);
        Assert.Equal(0, sides.DrawnCount);
        Assert.Throws<InvalidOperationException>(() => sides.Resolve(false));
    }

    [Fact]
    public void AStationWithOnlyDownHasOneSideSoRDoesNothing()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Down, "down", _wide);

        var side = Assert.Single(sides.Resolve(false));

        Assert.Equal(TimbnFacing.Down, side.Facing);
        Assert.Equal(0, side.RotationIndex);
        Assert.Equal("down", side.Model);
        Assert.Equal(_wide, side.Size);
        Assert.False(side.IsMirrored);
    }

    [Fact]
    public void AStationNeedsDownEvenWhenOtherFacingsAreGiven()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Left, "left", _tall);

        Assert.False(sides.HasModel);
        Assert.Throws<InvalidOperationException>(() => sides.Resolve(false));
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
    }

    [Fact]
    public void AFacingKeepsItsRotationIndexWhenTheOnesBeforeItAreMissing()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Down, "down", _wide);
        sides.Set(TimbnFacing.Right, "right", _tall);

        Assert.Equal([0, 3], sides.Resolve(false).Select(side => side.RotationIndex));
    }

    [Fact]
    public void GivingAFacingAgainReplacesItsModelAndSize()
    {
        var sides = DownAndLeft();

        sides.Set(TimbnFacing.Left, "left2", _wide);

        var left = sides.Resolve(false)[1];
        Assert.Equal("left2", left.Model);
        Assert.Equal(_wide, left.Size);
        Assert.Equal(2, sides.DrawnCount);
    }

    [Theory]
    [InlineData(0f, 1f, 1f)]
    [InlineData(1f, 0f, 1f)]
    [InlineData(1f, 1f, -1f)]
    public void ASideNeedsASizeAboveZeroOnEveryAxis(float x, float y, float z)
    {
        var sides = Sides();

        Assert.Throws<ArgumentOutOfRangeException>(() => sides.Set(TimbnFacing.Down, "a", new Vector3(x, y, z)));
        Assert.Throws<ArgumentOutOfRangeException>(() => sides.Set(TimbnFacing.Left, "b", new Vector3(x, y, z)));
        Assert.False(sides.HasModel);
    }

    [Fact]
    public void EachFacingKeepsItsOwnSite()
    {
        var sides = DownAndLeft();
        sides.SetSite(TimbnFacing.Down, "downSite");
        sides.SetSite(TimbnFacing.Left, "leftSite");

        var resolved = sides.Resolve(false);

        Assert.Equal("downSite", resolved[0].Site);
        Assert.Equal("leftSite", resolved[1].Site);
    }

    [Fact]
    public void ASiteGivenBeforeTheModelIsKept()
    {
        var sides = Sides();
        sides.SetSite(TimbnFacing.Down, "downSite");
        sides.Set(TimbnFacing.Down, "down", _wide);

        Assert.Equal("downSite", Assert.Single(sides.Resolve(false)).Site);
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
    public void AMirroredSideFollowsItsSourcesSite()
    {
        var sides = DownAndLeft();
        sides.SetSite(TimbnFacing.Left, "leftSite");

        Assert.Equal("leftSite", sides.Resolve(true)[2].Site);
    }

    [Fact]
    public void ALeftAndRightThatAreBothGivenAreLeftAlone()
    {
        var sides = DownAndLeft();
        sides.Set(TimbnFacing.Right, "right", _tall);

        var resolved = sides.Resolve(true);

        Assert.Equal(["down", "left", "right"], resolved.Select(side => side.Model));
        Assert.All(resolved, side => Assert.False(side.IsMirrored));
    }

    [Fact]
    public void DownAndUpAreNeverMirrored()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Down, "down", _wide);
        sides.Set(TimbnFacing.Up, "up", _wide);

        Assert.Equal([TimbnFacing.Down, TimbnFacing.Up], sides.Resolve(true).Select(side => side.Facing));
    }

    [Fact]
    public void MirroringADownOnlyStationAddsNothing()
    {
        var sides = Sides();
        sides.Set(TimbnFacing.Down, "down", _wide);

        Assert.Single(sides.Resolve(true));
    }

    [Fact]
    public void AllFourFacingsCanBeUsedAtOnce()
    {
        var sides = DownAndLeft();
        sides.Set(TimbnFacing.Up, "up", _wide);
        sides.Set(TimbnFacing.Right, "right", _tall);

        Assert.Equal(4, sides.Resolve(true).Count);
    }

    [Fact]
    public void ResolvingTwiceGivesTheSameSides()
    {
        var sides = DownAndLeft();

        Assert.Equal(sides.Resolve(true).Select(side => side.Facing), sides.Resolve(true).Select(side => side.Facing));
    }

    [Theory]
    [InlineData(TimbnFacing.Left, TimbnFacing.Right)]
    [InlineData(TimbnFacing.Right, TimbnFacing.Left)]
    [InlineData(TimbnFacing.Down, null)]
    [InlineData(TimbnFacing.Up, null)]
    public void OnlyLeftAndRightHaveAnOpposite(TimbnFacing facing, TimbnFacing? opposite)
    {
        Assert.Equal(opposite, TimbnStationSides<string>.Opposite(facing));
    }

    [Fact]
    public void EachSideKeepsItsOwnFootprintSize()
    {
        var resolved = DownAndLeft().Resolve(true);

        Assert.Equal([_wide, _tall, _tall], resolved.Select(side => side.Size));
    }

    [Fact]
    public void AMirroredSidesDockOffsetIsItsSourcesWithXFlipped()
    {
        var resolved = DownAndLeft().Resolve(true);
        var source = TimbnPartShape.DockOffset(resolved[1].Size, 0.2f, resolved[1].Facing);
        var mirrored = TimbnPartShape.DockOffset(resolved[2].Size, 0.2f, resolved[2].Facing);

        Assert.Equal(new Vector3(-source.x, source.y, source.z), mirrored);
    }

    [Fact]
    public void AMirroredSideHasTheSameFootprintRectAsItsSource()
    {
        var resolved = DownAndLeft().Resolve(true);
        var source = TimbnPartShape.Station(resolved[1].Size, 0.2f, resolved[1].Facing).FootprintRect;
        var mirrored = TimbnPartShape.Station(resolved[2].Size, 0.2f, resolved[2].Facing).FootprintRect;

        Assert.Equal(source, mirrored);
    }
}
