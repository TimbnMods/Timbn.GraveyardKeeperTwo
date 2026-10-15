using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnBuiltModelTests
{
    [Fact]
    public void TheModelIsCenteredAcrossAndDeepButSitsOnTheFloor()
    {
        List<TimbnFace> faces = [new("slab", TimbnFaceKind.Top, 0, 18, 0, 70, 34, 0), new("front", TimbnFaceKind.Front, 0, 0, 0, 70, 18, 0)];
        var extents = TimbnFaceLayout.Extents(faces);

        var corner = TimbnBuiltModel.ToWorld((0, 0, 0), extents.CenterX, extents.CenterZ);
        var far = TimbnBuiltModel.ToWorld((70, 18, 34), extents.CenterX, extents.CenterZ);

        Assert.Equal(-0.7f, corner.x, 4);
        Assert.Equal(0f, corner.y, 4);
        Assert.Equal(-0.425f, corner.z, 4);
        Assert.Equal(0.7f, far.x, 4);
        Assert.Equal(0.6f, far.y, 4);
        Assert.Equal(0.425f, far.z, 4);
    }

    [Fact]
    public void AModelWithNoFacesThrows()
    {
        Assert.Throws<InvalidOperationException>(() => TimbnBuiltModel.Create("Empty", []));
    }
}
