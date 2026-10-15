namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>What a caller asked for. A block is a front and a top cut from one picture.</summary>
internal enum TimbnFaceShape
{
    Front,
    Top,
    Block,
}

/// <summary>The two faces the game's camera can see.</summary>
internal enum TimbnFaceKind
{
    Front,
    Top,
}

/// <summary>One call to Front, Top or Block, before the picture's size is known. Positions are pixels.</summary>
internal readonly struct TimbnFaceRequest
{
    internal TimbnFaceRequest(TimbnFaceShape shape, string name, int x, int y, int z, int depth)
    {
        Shape = shape;
        Name = name;
        X = x;
        Y = y;
        Z = z;
        Depth = depth;
    }

    internal TimbnFaceShape Shape { get; }

    internal string Name { get; }

    internal int X { get; }

    internal int Y { get; }

    internal int Z { get; }

    // Only a block uses it, as the number of picture rows that are its top.
    internal int Depth { get; }
}

/// <summary>
/// One rectangle the player sees. A front stands at <c>Z</c> and is <c>Width</c> across and <c>Height</c> tall from
/// <c>Y</c>. A top lies at <c>Y</c> and is <c>Width</c> across and <c>Height</c> deep from <c>Z</c>. It paints
/// <c>Height</c> rows of its picture, counted up from the picture's bottom row starting at <c>FirstRow</c>.
/// </summary>
internal readonly struct TimbnFace
{
    internal TimbnFace(string name, TimbnFaceKind kind, int x, int y, int z, int width, int height, int firstRow)
    {
        Name = name;
        Kind = kind;
        X = x;
        Y = y;
        Z = z;
        Width = width;
        Height = height;
        FirstRow = firstRow;
    }

    internal string Name { get; }

    internal TimbnFaceKind Kind { get; }

    internal int X { get; }

    internal int Y { get; }

    internal int Z { get; }

    internal int Width { get; }

    internal int Height { get; }

    internal int FirstRow { get; }
}

/// <summary>Where a model's faces reach, in pixels.</summary>
internal readonly struct TimbnFaceExtents
{
    internal TimbnFaceExtents(int minX, int maxX, int minZ, int maxZ, int maxY)
    {
        MinX = minX;
        MaxX = maxX;
        MinZ = minZ;
        MaxZ = maxZ;
        MaxY = maxY;
    }

    internal int MinX { get; }

    internal int MaxX { get; }

    internal int MinZ { get; }

    internal int MaxZ { get; }

    internal int MaxY { get; }

    internal float CenterX => (MinX + MaxX) / 2f;

    internal float CenterZ => (MinZ + MaxZ) / 2f;
}

/// <summary>
/// The rules for a model made of pictures, kept apart from Unity so they can be tested. Each picture is exactly what
/// the player sees, so a face's size is its picture's size and the only numbers a caller gives are where it stands.
/// </summary>
internal static class TimbnFaceLayout
{
    // Corner order for every face, bottom left, bottom right, top right, top left as the player sees it. A top's
    // "bottom" is its front edge. The picture's corners follow the same order, so the texture coordinates never change.
    internal static readonly IReadOnlyList<int> Triangles = [0, 2, 1, 0, 3, 2];

    internal static void Validate(TimbnFaceRequest request)
    {
        if (request.X < 0 || request.Y < 0 || request.Z < 0)
            throw new ArgumentOutOfRangeException(nameof(request), $"{request.Name} is placed from the model's front, bottom, left corner, so its position can't be negative. Got x {request.X}, y {request.Y}, z {request.Z}.");

        if (request.Shape == TimbnFaceShape.Block && request.Depth < 1)
            throw new ArgumentOutOfRangeException(nameof(request), $"{request.Name} needs a depth of at least 1. Got {request.Depth}.");
    }

    // A block gives its top the upper depth rows of the picture and keeps the rest as its front.
    internal static List<TimbnFace> Resolve(TimbnFaceRequest request, int imageWidth, int imageHeight)
    {
        var (name, x, y, z) = (request.Name, request.X, request.Y, request.Z);
        switch (request.Shape)
        {
            case TimbnFaceShape.Front:
                return [new(name, TimbnFaceKind.Front, x, y, z, imageWidth, imageHeight, 0)];
            case TimbnFaceShape.Top:
                return [new(name, TimbnFaceKind.Top, x, y, z, imageWidth, imageHeight, 0)];
            default:
                var frontRows = imageHeight - request.Depth;
                if (frontRows < 1)
                    throw new InvalidOperationException($"{name} is {imageHeight} pixels tall, so a depth of {request.Depth} leaves no front. The top takes the upper depth rows and the front needs at least one more.");

                return
                [
                    new($"{name} front", TimbnFaceKind.Front, x, y, z, imageWidth, frontRows, 0),
                    new($"{name} top", TimbnFaceKind.Top, x, y + frontRows, z, imageWidth, request.Depth, frontRows),
                ];
        }
    }

    // Two faces on the same plane that cover the same spot fight over it. Faces on different planes may touch.
    internal static (TimbnFace First, TimbnFace Second)? FindOverlap(IReadOnlyList<TimbnFace> faces)
    {
        for (var first = 0; first < faces.Count; first++)
        {
            for (var second = first + 1; second < faces.Count; second++)
            {
                if (Overlaps(faces[first], faces[second]))
                    return (faces[first], faces[second]);
            }
        }

        return null;
    }

    internal static void CheckOverlaps(IReadOnlyList<TimbnFace> faces)
    {
        if (FindOverlap(faces) is { } overlap)
        {
            var (first, second) = overlap;
            throw new InvalidOperationException($"{first.Name} and {second.Name} lie on the same plane and overlap, so they would flicker. Move one of them or put it on a different {(first.Kind == TimbnFaceKind.Front ? "z" : "y")}.");
        }
    }

    internal static TimbnFaceExtents Extents(IReadOnlyList<TimbnFace> faces) => new(
        faces.Min(face => face.X),
        faces.Max(face => face.X + face.Width),
        faces.Min(face => face.Z),
        faces.Max(face => face.Z + (face.Kind == TimbnFaceKind.Top ? face.Height : 0)),
        faces.Max(face => face.Y + (face.Kind == TimbnFaceKind.Front ? face.Height : 0)));

    internal static (int X, int Y, int Z)[] Corners(TimbnFace face)
    {
        var (x, y, z) = (face.X, face.Y, face.Z);
        var right = x + face.Width;
        return face.Kind == TimbnFaceKind.Front
            ? [(x, y, z), (right, y, z), (right, y + face.Height, z), (x, y + face.Height, z)]
            : [(x, y, z), (right, y, z), (right, y, z + face.Height), (x, y, z + face.Height)];
    }

    // Front faces the camera, which looks along z, and top faces up.
    internal static (int X, int Y, int Z) Normal(TimbnFaceKind kind) => kind == TimbnFaceKind.Front ? (0, 0, -1) : (0, 1, 0);

    // The part of the picture the face paints, as heights from its bottom (0) to its top (1).
    internal static (float Low, float High) PictureRows(TimbnFace face, int imageHeight) =>
        (face.FirstRow / (float)imageHeight, (face.FirstRow + face.Height) / (float)imageHeight);

    // A pixel that is neither clear nor solid may not draw right on the game's prop shader.
    internal static int HalfTransparentPixels(IEnumerable<byte> alphas) => alphas.Count(alpha => alpha is > 0 and < 255);

    private static bool Overlaps(TimbnFace a, TimbnFace b)
    {
        if (a.Kind != b.Kind)
            return false;

        var sameAcross = a.X < b.X + b.Width && b.X < a.X + a.Width;
        if (a.Kind == TimbnFaceKind.Front)
            return a.Z == b.Z && sameAcross && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;

        return a.Y == b.Y && sameAcross && a.Z < b.Z + b.Height && b.Z < a.Z + a.Height;
    }
}
