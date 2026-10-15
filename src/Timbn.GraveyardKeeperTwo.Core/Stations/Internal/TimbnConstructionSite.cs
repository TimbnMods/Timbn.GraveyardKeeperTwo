namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The look a station gets while it is being built when its mod gave none, a dashed outline of its footprint on the
/// ground with the game's own warning sign standing at the front. The outline is painted in code, so a mod with no art
/// of its own still gets a site that matches its station's size. It is one <c>Top</c> face of a <see cref="TimbnModelFaces"/>.
/// </summary>
internal static class TimbnConstructionSite
{
    internal const int MinimumSide = 12;
    internal const int Lift = 1;
    internal const int DashLength = 4;

    private static readonly Color32 _clear = new(0, 0, 0, 0);
    private static readonly Color32 _yellow = new(232, 178, 40, 255);
    private static readonly Color32 _dark = new(70, 50, 30, 255);

    internal static int Width(Vector3 size) => Mathf.Max(MinimumSide, Mathf.RoundToInt(size.x * TimbnBuiltModel.PixelsAcross));

    internal static int Depth(Vector3 size) => Mathf.Max(MinimumSide, Mathf.RoundToInt(size.z * TimbnBuiltModel.PixelsDeep));

    internal static (TimbnModel Model, IDisposable Owner) Create(string stationId, Vector3 size)
    {
        var (width, depth) = (Width(size), Depth(size));

        // The outline lies a pixel above the ground so it never flickers against it.
        TimbnModelFaces faces = new(typeof(TimbnConstructionSite).Assembly);
        faces.Top(Png(Outline(width, depth), width, depth), 0, Lift, 0, $"{stationId} outline");

        var (model, owner) = TimbnBuiltModel.Create($"{stationId}Site", faces.Pictures);
        model.Decorate = site => TimbnVanillaSign.AddTo(site, size);
        return (model, owner);
    }

    // A border one pixel thick drawn as alternating yellow and brown dashes, like hazard tape laid on the ground.
    internal static Color32[] Outline(int width, int depth)
    {
        var picture = new Color32[width * depth];
        Array.Fill(picture, _clear);
        for (var column = 0; column < width; column++)
        {
            var color = column / DashLength % 2 == 0 ? _yellow : _dark;
            picture[column] = color;
            picture[(depth - 1) * width + column] = color;
        }

        for (var row = 0; row < depth; row++)
        {
            var color = row / DashLength % 2 == 0 ? _yellow : _dark;
            picture[row * width] = color;
            picture[row * width + width - 1] = color;
        }

        return picture;
    }

    internal static byte[] Png(Color32[] picture, int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            texture.SetPixels32(picture);
            texture.Apply();
            return texture.EncodeToPNG();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
