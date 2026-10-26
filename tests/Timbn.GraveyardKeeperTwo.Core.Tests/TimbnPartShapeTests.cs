using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnPartShapeTests
{
    [Theory]
    [InlineData(null, 0.45f)]
    [InlineData(0.2f, 0.45f)]
    [InlineData(0.8f, 0.8f)]
    public void ASiteKeepsAtLeastTheDefaultDockDistance(float? station, float expected)
    {
        Assert.Equal(expected, TimbnPartShape.SiteDockDistance(station));
    }

    private static readonly Vector3 _bench = new(1.5f, 0.8f, 1f);

    private static TimbnPartBox Box(TimbnPartShape shape, string name) => Assert.Single(shape.Boxes, box => box.Name == name);

    [Theory]
    [InlineData(TimbnFacing.Down, 0f, 0f, -0.7f)]
    [InlineData(TimbnFacing.Left, -0.95f, 0f, 0f)]
    public void TheDockPointSitsTheDistanceBeyondTheChosenEdge(TimbnFacing side, float x, float y, float z)
    {
        var shape = TimbnPartShape.Station(_bench, 0.2f, side);

        Assert.Equal(x, shape.DockPoint!.Value.x, 4);
        Assert.Equal(y, shape.DockPoint!.Value.y, 4);
        Assert.Equal(z, shape.DockPoint!.Value.z, 4);
    }
}
