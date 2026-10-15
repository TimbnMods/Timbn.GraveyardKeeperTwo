using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnModelBoundsTests
{
    [Fact]
    public void AModelWithNoMeshesGetsAUnitBox()
    {
        Assert.Equal(TimbnModelBounds.Fallback, TimbnModelBounds.Combine([]));
    }

    [Fact]
    public void MeshesAreMovedIntoTheRootsSpace()
    {
        var post = (Matrix4x4.Translate(new Vector3(0f, 0.48f, 0.34f)), new Bounds(Vector3.zero, new Vector3(0.1f, 0.7f, 0.1f)));
        var arm = (Matrix4x4.Translate(new Vector3(0f, 0.62f, 0.34f)), new Bounds(Vector3.zero, new Vector3(0.4f, 0.1f, 0.1f)));

        var bounds = TimbnModelBounds.Combine([post, arm]);

        Assert.Equal(-0.2f, bounds.min.x, 4);
        Assert.Equal(0.2f, bounds.max.x, 4);
        Assert.Equal(0.13f, bounds.min.y, 4);
        Assert.Equal(0.83f, bounds.max.y, 4);
        Assert.Equal(0.29f, bounds.min.z, 4);
        Assert.Equal(0.39f, bounds.max.z, 4);
    }

    [Fact]
    public void ARotatedMeshStillFits()
    {
        // A quarter turn about Y written out, since Quaternion.Euler is native and has no engine here.
        var quarterTurn = new Quaternion(0f, 0.70710678f, 0f, 0.70710678f);
        var turned = (Matrix4x4.Rotate(quarterTurn), new Bounds(Vector3.zero, new Vector3(2f, 1f, 0.5f)));

        var bounds = TimbnModelBounds.Combine([turned]);

        Assert.Equal(0.5f, bounds.size.x, 4);
        Assert.Equal(1f, bounds.size.y, 4);
        Assert.Equal(2f, bounds.size.z, 4);
    }
}
