using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnPartShapeTests
{
    [Theory]
    [InlineData(null, 0.45f)]
    [InlineData(0.05f, 0.45f)]
    [InlineData(0.2f, 0.45f)]
    [InlineData(0.45f, 0.45f)]
    [InlineData(0.8f, 0.8f)]
    public void ASiteKeepsAtLeastTheDefaultDockDistance(float? station, float expected)
    {
        Assert.Equal(expected, TimbnPartShape.SiteDockDistance(station));
    }

    private static readonly Vector3 _bench = new(1.5f, 0.8f, 1f);

    private static TimbnPartBox Box(TimbnPartShape shape, string name) => Assert.Single(shape.Boxes, box => box.Name == name);

    [Fact]
    public void AStationCanPutItsDockPointCloserToItsFront()
    {
        var shape = TimbnPartShape.Station(_bench, 0.2f);

        Assert.Equal(-0.7f, shape.DockPoint!.Value.z, 4);
    }

    [Fact]
    public void AStationHasABodyAFootprintAnInteractAreaAndADockPointInFront()
    {
        var shape = TimbnPartShape.Station(_bench);

        Assert.Equal(_bench, shape.Footprint);
        Assert.Equal(3, shape.Boxes.Count);

        var body = Box(shape, "Obstacle");
        Assert.Equal(TimbnPartShape.ObstacleLayer, body.Layer);
        Assert.False(body.IsTrigger);
        Assert.Equal(new Vector3(0f, 0.4f, 0f), body.Center);
        Assert.Equal(new Vector3(1.2f, 0.8f, 0.8f), body.Size);

        var footprint = Box(shape, "Build Collider");
        Assert.Equal(TimbnPartShape.BuildAreaLayer, footprint.Layer);
        Assert.True(footprint.IsTrigger);
        Assert.Equal(new Vector3(1.5f, 0.2f, 1f), footprint.Size);

        var interact = Box(shape, "Interact");
        Assert.Equal(TimbnPartShape.InteractLayer, interact.Layer);
        Assert.True(interact.IsTrigger);
        Assert.Equal(new Vector3(1.8f, 0.8f, 1.3f), interact.Size);

        Assert.Equal(new Vector3(0f, 0f, -0.95f), shape.DockPoint);
    }

    [Fact]
    public void AnExtensionHasABuffAreaReachingPastTheFootprintAndNothingToInteractWith()
    {
        var shape = TimbnPartShape.Extension(new Vector3(0.5f, 1f, 0.5f), 0.64f);

        Assert.Equal(3, shape.Boxes.Count);
        Assert.DoesNotContain(shape.Boxes, box => box.Layer == TimbnPartShape.InteractLayer);
        Assert.Null(shape.DockPoint);

        var buff = Box(shape, "Buff Area");
        Assert.Equal(TimbnPartShape.BuffAreaLayer, buff.Layer);
        Assert.True(buff.IsTrigger);
        Assert.Equal(1.78f, buff.Size.x, 3);
        Assert.Equal(1.78f, buff.Size.z, 3);
        Assert.Equal(0.2f, buff.Size.y, 3);
    }

    [Fact]
    public void APartHasOnlyTheSolidBoxItWasGiven()
    {
        var solid = new Bounds(new Vector3(0f, 0.3f, 0.075f), new Vector3(0.6f, 0.6f, 1f));
        var fence = TimbnPartShape.Part(solid);
        var headstone = TimbnPartShape.Part(null);

        var body = Assert.Single(fence.Boxes);
        Assert.Equal(TimbnPartShape.ObstacleLayer, body.Layer);
        Assert.False(body.IsTrigger);
        Assert.Equal(solid.center, body.Center);
        Assert.Equal(solid.size, body.Size);
        Assert.Null(fence.Footprint);
        Assert.Null(fence.FootprintRect);
        Assert.Null(fence.DockPoint);

        Assert.Empty(headstone.Boxes);
    }

    [Theory]
    [InlineData(TimbnFacing.Down, 0f, 0f, -0.7f)]
    [InlineData(TimbnFacing.Up, 0f, 0f, 0.7f)]
    [InlineData(TimbnFacing.Left, -0.95f, 0f, 0f)]
    [InlineData(TimbnFacing.Right, 0.95f, 0f, 0f)]
    public void TheDockPointSitsTheDistanceBeyondTheChosenEdge(TimbnFacing side, float x, float y, float z)
    {
        var shape = TimbnPartShape.Station(_bench, 0.2f, side);

        Assert.Equal(x, shape.DockPoint!.Value.x, 4);
        Assert.Equal(y, shape.DockPoint!.Value.y, 4);
        Assert.Equal(z, shape.DockPoint!.Value.z, 4);
    }

    [Fact]
    public void TheDockSideDoesNotChangeTheBodyOrTheFootprint()
    {
        var front = TimbnPartShape.Station(_bench, 0.2f);
        var left = TimbnPartShape.Station(_bench, 0.2f, TimbnFacing.Left);

        Assert.Equal(front.FootprintRect, left.FootprintRect);
        Assert.Equal(front.Boxes.Count, left.Boxes.Count);
    }

    [Fact]
    public void TheFootprintRectIsCenteredOnTheObject()
    {
        var rect = TimbnPartShape.Station(_bench).FootprintRect;

        Assert.Equal(new Rect(-0.75f, -0.5f, 1.5f, 1f), rect);
    }

    [Fact]
    public void ChunkBoundsPadTheBodyOrTheModel()
    {
        var model = new Bounds(new Vector3(0f, 0.35f, 0.34f), new Vector3(0.4f, 0.7f, 0.1f));

        var station = TimbnPartShape.Station(_bench).ChunkBounds(model);
        Assert.Equal(new Vector3(0f, 0.4f, 0f), station.center);
        Assert.Equal(new Vector3(2f, 1.3f, 1.5f), station.size);

        var part = TimbnPartShape.Part(null).ChunkBounds(model);
        Assert.Equal(model.center, part.center);
        Assert.Equal(new Vector3(0.9f, 1.2f, 0.6f), part.size);
    }
}
