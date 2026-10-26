namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnFaceLayoutTests
{
    private static TimbnFaceRequest Request(TimbnFaceShape shape, string name = "face", int x = 0, int y = 0, int z = 0, int depth = 0) =>
        new(shape, name, x, y, z, depth);

    private static TimbnFace Front(string name, int x, int y, int z, int width, int height) =>
        new(name, TimbnFaceKind.Front, x, y, z, width, height, 0);

    private static TimbnFace Top(string name, int x, int y, int z, int width, int depth) =>
        new(name, TimbnFaceKind.Top, x, y, z, width, depth, 0);

    [Fact]
    public void ABlockGivesItsUpperRowsToTheTopAndTheRestToTheFront()
    {
        var faces = TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Block, "base", 0, 0, 4, 32), 44, 52);

        var front = faces[0];
        var top = faces[1];
        Assert.Equal((TimbnFaceKind.Front, 0, 0, 4, 44, 20, 0), (front.Kind, front.X, front.Y, front.Z, front.Width, front.Height, front.FirstRow));
        Assert.Equal((TimbnFaceKind.Top, 0, 20, 4, 44, 32, 20), (top.Kind, top.X, top.Y, top.Z, top.Width, top.Height, top.FirstRow));
    }

    [Fact]
    public void TwoFrontsOnTheSamePlaneThatOverlapThrowAndNameBoth()
    {
        List<TimbnFace> faces = [Front("left", 0, 0, 5, 10, 10), Front("right", 9, 4, 5, 10, 10)];

        var error = Assert.Throws<InvalidOperationException>(() => TimbnFaceLayout.CheckOverlaps(faces));

        Assert.Contains("left", error.Message);
        Assert.Contains("right", error.Message);

        List<TimbnFace> tops = [Top("floor", 0, 1, 0, 20, 20), Top("rug", 5, 1, 10, 4, 4)];
        var topError = Assert.Throws<InvalidOperationException>(() => TimbnFaceLayout.CheckOverlaps(tops));

        Assert.Contains("floor", topError.Message);
        Assert.Contains("rug", topError.Message);
    }

    [Fact]
    public void FacesOnTheSamePlaneThatOnlyTouchAreFine()
    {
        List<TimbnFace> faces = [Front("left", 0, 0, 5, 10, 10), Front("right", 10, 0, 5, 10, 10), Front("above", 0, 10, 5, 10, 4)];

        Assert.Null(TimbnFaceLayout.FindOverlap(faces));
    }

    [Fact]
    public void FacesOnDifferentPlanesNeverOverlap()
    {
        List<TimbnFace> faces =
        [
            Front("near", 0, 0, 0, 10, 10),
            Front("far", 0, 0, 1, 10, 10),
            Top("low", 0, 1, 0, 10, 10),
            Top("high", 0, 2, 0, 10, 10),
            Front("wall", 0, 0, 3, 10, 10),
        ];

        Assert.Null(TimbnFaceLayout.FindOverlap(faces));
    }

    [Fact]
    public void ExtentsCoverEveryFaceWithTopsReachingBackAndFrontsReachingUp()
    {
        List<TimbnFace> faces = [Top("shadow", 0, 1, 0, 60, 36), Front("pot", 4, 20, 16, 24, 26), Top("lid", 2, 30, 10, 5, 3)];

        var extents = TimbnFaceLayout.Extents(faces);

        Assert.Equal((0, 60, 0, 36, 46), (extents.MinX, extents.MaxX, extents.MinZ, extents.MaxZ, extents.MaxY));
        Assert.Equal(30f, extents.CenterX);
        Assert.Equal(18f, extents.CenterZ);
    }

    [Fact]
    public void AFrontsCornersRunAcrossAtItsDepthAndUp()
    {
        var corners = TimbnFaceLayout.Corners(Front("pot", 4, 20, 16, 24, 26));

        Assert.Equal([(4, 20, 16), (28, 20, 16), (28, 46, 16), (4, 46, 16)], corners);
        Assert.Equal([(0, 1, 2), (60, 1, 2), (60, 1, 38), (0, 1, 38)], TimbnFaceLayout.Corners(Top("shadow", 0, 1, 2, 60, 36)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheTriangleOrderRunsClockwiseSeenFromTheFacesNormal(bool isFront)
    {
        var face = isFront ? Front("f", 0, 0, 0, 3, 5) : Top("t", 0, 0, 0, 3, 5);
        var kind = face.Kind;
        var corners = TimbnFaceLayout.Corners(face);
        var normal = TimbnFaceLayout.Normal(kind);

        for (var triangle = 0; triangle < TimbnFaceLayout.Triangles.Count; triangle += 3)
        {
            var a = corners[TimbnFaceLayout.Triangles[triangle]];
            var b = corners[TimbnFaceLayout.Triangles[triangle + 1]];
            var c = corners[TimbnFaceLayout.Triangles[triangle + 2]];
            var (ux, uy, uz) = (b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var (vx, vy, vz) = (c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            var cross = (X: uy * vz - uz * vy, Y: uz * vx - ux * vz, Z: ux * vy - uy * vx);

            // Unity draws the side whose corners run clockwise, and with the usual cross product that side is the one it points toward.
            Assert.True(cross.X * normal.X + cross.Y * normal.Y + cross.Z * normal.Z > 0);
        }
    }

    [Fact]
    public void ABlocksFrontUsesTheLowRowsAndItsTopTheHighRows()
    {
        var faces = TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Block, "base", depth: 32), 44, 52);

        var (frontLow, frontHigh) = TimbnFaceLayout.PictureRows(faces[0], 52);
        var (topLow, topHigh) = TimbnFaceLayout.PictureRows(faces[1], 52);

        Assert.Equal(0f, frontLow);
        Assert.Equal(20f / 52f, frontHigh, 5);
        Assert.Equal(frontHigh, topLow);
        Assert.Equal(1f, topHigh);
    }

    [Fact]
    public void OnlyPixelsBetweenClearAndSolidCountAsHalfTransparent()
    {
        byte[] alphas = [0, 255, 1, 128, 254, 255, 0];

        Assert.Equal(3, TimbnFaceLayout.HalfTransparentPixels(alphas));
    }
}
