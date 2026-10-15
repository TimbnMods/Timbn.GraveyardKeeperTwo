using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnConstructionSiteTests
{
    private static readonly Vector3 _desk = new(0.92f, 0.5f, 0.55f);

    [Fact]
    public void TheOutlineIsAsWideAndDeepAsTheStationInPixels()
    {
        Assert.Equal(46, TimbnConstructionSite.Width(_desk));
        Assert.Equal(22, TimbnConstructionSite.Depth(_desk));
    }

    [Fact]
    public void ATinyStationStillGetsAnOutlineWithRoomForItsDashes()
    {
        var tiny = new Vector3(0.05f, 0.05f, 0.05f);

        Assert.Equal(TimbnConstructionSite.MinimumSide, TimbnConstructionSite.Width(tiny));
        Assert.Equal(TimbnConstructionSite.MinimumSide, TimbnConstructionSite.Depth(tiny));
    }

    [Fact]
    public void TheOutlinePaintsOnlyItsBorderAndLeavesTheMiddleSeeThrough()
    {
        var width = TimbnConstructionSite.Width(_desk);
        var depth = TimbnConstructionSite.Depth(_desk);

        var outline = TimbnConstructionSite.Outline(width, depth);

        Assert.Equal(width * depth, outline.Length);
        Assert.Equal(255, outline[0].a);
        Assert.Equal(255, outline[width - 1].a);
        Assert.Equal(255, outline[(depth - 1) * width + width / 2].a);
        Assert.Equal(255, outline[depth / 2 * width].a);
        Assert.Equal(0, outline[depth / 2 * width + width / 2].a);
    }

    [Fact]
    public void TheDashesAlternateColorEveryFewPixels()
    {
        var width = TimbnConstructionSite.Width(_desk);
        var outline = TimbnConstructionSite.Outline(width, TimbnConstructionSite.Depth(_desk));

        Assert.Equal(outline[0], outline[TimbnConstructionSite.DashLength - 1]);
        Assert.NotEqual(outline[0], outline[TimbnConstructionSite.DashLength]);
    }
}
