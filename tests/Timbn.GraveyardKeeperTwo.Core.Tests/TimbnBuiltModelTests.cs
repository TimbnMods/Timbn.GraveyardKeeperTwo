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

        Assert.Equal(-far.x, corner.x, 4);
        Assert.Equal(-far.z, corner.z, 4);
        Assert.Equal(0f, corner.y, 4);
        Assert.True(far.y > 0f);
    }
}
