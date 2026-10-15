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
    public void AFrontTakesItsPicturesSize()
    {
        var faces = TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Front, "pot", 4, 20, 16), 24, 26);

        var face = Assert.Single(faces);
        Assert.Equal(TimbnFaceKind.Front, face.Kind);
        Assert.Equal((4, 20, 16, 24, 26), (face.X, face.Y, face.Z, face.Width, face.Height));
    }

    [Fact]
    public void ATopTakesItsPicturesSizeAsAcrossAndDeep()
    {
        var faces = TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Top, "shadow", 0, 1, 0), 60, 36);

        var face = Assert.Single(faces);
        Assert.Equal(TimbnFaceKind.Top, face.Kind);
        Assert.Equal((60, 36), (face.Width, face.Height));
    }

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
    public void ABlocksFaceNamesSayWhichHalfTheyAre()
    {
        var faces = TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Block, "base", depth: 2), 10, 12);

        Assert.Equal("base front", faces[0].Name);
        Assert.Equal("base top", faces[1].Name);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public void ABlockThatLeavesNoFrontRowsThrowsAndNamesIt(int depth)
    {
        var error = Assert.Throws<InvalidOperationException>(() => TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Block, "stone", depth: depth), 8, 10));

        Assert.Contains("stone", error.Message);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void ANegativePositionThrowsAndNamesTheFace(int x, int y, int z)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => TimbnFaceLayout.Validate(Request(TimbnFaceShape.Front, "pot", x, y, z)));

        Assert.Contains("pot", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ABlockNeedsADepthOfAtLeastOne(int depth)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => TimbnFaceLayout.Validate(Request(TimbnFaceShape.Block, "base", depth: depth)));

        Assert.Contains("base", error.Message);
    }

    [Fact]
    public void AFrontAndATopDoNotNeedADepth()
    {
        TimbnFaceLayout.Validate(Request(TimbnFaceShape.Front));
        TimbnFaceLayout.Validate(Request(TimbnFaceShape.Top));
    }

    [Fact]
    public void TwoFrontsOnTheSamePlaneThatOverlapThrowAndNameBoth()
    {
        List<TimbnFace> faces = [Front("left", 0, 0, 5, 10, 10), Front("right", 9, 4, 5, 10, 10)];

        var error = Assert.Throws<InvalidOperationException>(() => TimbnFaceLayout.CheckOverlaps(faces));

        Assert.Contains("left", error.Message);
        Assert.Contains("right", error.Message);
    }

    [Fact]
    public void TwoTopsAtTheSameHeightThatOverlapThrowAndNameBoth()
    {
        List<TimbnFace> faces = [Top("floor", 0, 1, 0, 20, 20), Top("rug", 5, 1, 10, 4, 4)];

        var error = Assert.Throws<InvalidOperationException>(() => TimbnFaceLayout.CheckOverlaps(faces));

        Assert.Contains("floor", error.Message);
        Assert.Contains("rug", error.Message);
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
    public void AFrontAndATopOnTheSameSpotDoNotOverlap()
    {
        List<TimbnFace> faces = [Front("front", 0, 0, 0, 10, 10), Top("top", 0, 0, 0, 10, 10)];

        Assert.Null(TimbnFaceLayout.FindOverlap(faces));
    }

    [Fact]
    public void TheTwoHalvesOfABlockNeverOverlapEachOther()
    {
        var faces = TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Block, "base", 0, 0, 4, 32), 44, 52);

        Assert.Null(TimbnFaceLayout.FindOverlap(faces));
    }

    [Fact]
    public void TwoBlocksWhoseTopsShareAHeightAndOverlapAreCaught()
    {
        var faces = TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Block, "one", 0, 0, 0, 4), 10, 14);
        faces.AddRange(TimbnFaceLayout.Resolve(Request(TimbnFaceShape.Block, "two", 5, 0, 2, 4), 10, 14));

        Assert.NotNull(TimbnFaceLayout.FindOverlap(faces));
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
    public void ExtentsOfAFrontOnlyModelHaveNoDepth()
    {
        List<TimbnFace> faces = [Front("sign", 3, 0, 7, 10, 12)];

        var extents = TimbnFaceLayout.Extents(faces);

        Assert.Equal((7, 7, 12), (extents.MinZ, extents.MaxZ, extents.MaxY));
    }

    [Fact]
    public void AFrontsCornersRunAcrossAtItsDepthAndUp()
    {
        var corners = TimbnFaceLayout.Corners(Front("pot", 4, 20, 16, 24, 26));

        Assert.Equal([(4, 20, 16), (28, 20, 16), (28, 46, 16), (4, 46, 16)], corners);
    }

    [Fact]
    public void ATopsCornersRunAcrossAtItsHeightAndBack()
    {
        var corners = TimbnFaceLayout.Corners(Top("shadow", 0, 1, 2, 60, 36));

        Assert.Equal([(0, 1, 2), (60, 1, 2), (60, 1, 38), (0, 1, 38)], corners);
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
    public void ThePictureRowsOfAWholeFaceRunFromZeroToOne()
    {
        var (low, high) = TimbnFaceLayout.PictureRows(Front("pot", 0, 0, 0, 24, 26), 26);

        Assert.Equal(0f, low);
        Assert.Equal(1f, high);
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
