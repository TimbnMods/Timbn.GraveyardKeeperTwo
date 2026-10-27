using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a model made from pictures, for <see cref="TimbnPluginModels.Build"/>. Every
/// picture is exactly what the player sees, so its own size is the size of the face and you only say where it stands.
/// The game's camera tilts forward and down and never sideways, so only two kinds of face are ever seen, one standing
/// up toward the camera (<c>Front</c>) and one lying face up (<c>Top</c>), and a solid box is just
/// both of them (<c>Block</c>). Sides, backs and undersides are never built.
/// </summary>
/// <remarks>
/// Positions are in pixels from the model's front, bottom, left corner as the player sees it, standing in front, with x
/// running right, y running up and z running away from the player. They can't be negative. One world unit is 50 pixels
/// across, 30 up and 40 deep, so a point further back sits higher on screen. Two faces on the same plane can't cover the
/// same spot, since they would flicker, but faces on different planes may touch. Transparent pixels are see through,
/// and pixels that are only half see through may not draw right.
/// </remarks>
/// <example>
/// A still with a 60x36 shadow lying on the floor, a 44 wide block for its base (20 front rows under 32 top rows) and
/// a 24x26 pot standing up.
/// <code><![CDATA[
/// var still = Models.Build("SpiritStill", model => model
///     .Top("still_shadow", x: 0, y: 1, z: 0)
///     .Block("still_base", x: 0, y: 0, z: 4, depth: 32)
///     .Front("still_pot", x: 4, y: 20, z: 16));
/// ]]></code>
/// </example>
public sealed class TimbnModelFaces
{
    private readonly Assembly _assembly;

    internal TimbnModelFaces(Assembly assembly)
    {
        _assembly = assembly;
    }

    internal List<TimbnPlacedPicture> Pictures { get; } = [];

    /// <summary>
    /// Stands a picture up facing the camera, such as a monitor screen or a sign. Its width and height are the PNG's.
    /// </summary>
    /// <param name="pngName">
    /// A PNG embedded in the mod's DLL, named without ".png", for example "still_pot". Embed it with
    /// <c>LogicalName="%(Filename)%(Extension)"</c>.
    /// </param>
    /// <param name="x">Pixels from the model's left edge to the picture's left edge.</param>
    /// <param name="y">Pixels up from the floor to the picture's bottom.</param>
    /// <param name="z">Pixels back from the model's front to the picture.</param>
    /// <exception cref="ArgumentOutOfRangeException">A position is below zero.</exception>
    /// <exception cref="InvalidOperationException">The mod's DLL has no such embedded resource.</exception>
    public TimbnModelFaces Front(string pngName, int x, int y, int z) => Add(TimbnFaceShape.Front, pngName, x, y, z, 0);

    /// <summary>Stands a picture made in code up facing the camera.</summary>
    /// <param name="png">The PNG file's bytes.</param>
    /// <param name="x">Pixels from the model's left edge to the picture's left edge.</param>
    /// <param name="y">Pixels up from the floor to the picture's bottom.</param>
    /// <param name="z">Pixels back from the model's front to the picture.</param>
    /// <param name="name">What error messages call this face. It defaults to its place in the model.</param>
    /// <exception cref="ArgumentOutOfRangeException">A position is below zero.</exception>
    public TimbnModelFaces Front(byte[] png, int x, int y, int z, string? name = null) => Add(TimbnFaceShape.Front, png, name, x, y, z, 0);

    /// <summary>
    /// Lays a picture flat facing up, such as a shadow on the floor or a keyboard on a desk. The picture's bottom row
    /// is its front edge and its top row is its back edge, so it reaches from z to z plus the PNG's height. Put it 1
    /// pixel above any surface under it so the two don't flicker.
    /// </summary>
    /// <param name="pngName">A PNG embedded in the mod's DLL, named without ".png".</param>
    /// <param name="x">Pixels from the model's left edge to the picture's left edge.</param>
    /// <param name="y">Pixels up from the floor to the picture.</param>
    /// <param name="z">Pixels back from the model's front to the picture's front edge.</param>
    /// <exception cref="ArgumentOutOfRangeException">A position is below zero.</exception>
    /// <exception cref="InvalidOperationException">The mod's DLL has no such embedded resource.</exception>
    public TimbnModelFaces Top(string pngName, int x, int y, int z) => Add(TimbnFaceShape.Top, pngName, x, y, z, 0);

    /// <summary>Lays a picture made in code flat facing up.</summary>
    /// <param name="png">The PNG file's bytes.</param>
    /// <param name="x">Pixels from the model's left edge to the picture's left edge.</param>
    /// <param name="y">Pixels up from the floor to the picture.</param>
    /// <param name="z">Pixels back from the model's front to the picture's front edge.</param>
    /// <param name="name">What error messages call this face. It defaults to its place in the model.</param>
    /// <exception cref="ArgumentOutOfRangeException">A position is below zero.</exception>
    public TimbnModelFaces Top(byte[] png, int x, int y, int z, string? name = null) => Add(TimbnFaceShape.Top, png, name, x, y, z, 0);

    /// <summary>
    /// Adds a solid block from one picture of it as the player sees it, with the top face stacked above the front face.
    /// The upper <paramref name="depth"/> rows are the top and the rest are the front, so the front is the PNG's height
    /// minus the depth. Both share the block's x and width.
    /// </summary>
    /// <param name="pngName">A PNG embedded in the mod's DLL, named without ".png".</param>
    /// <param name="x">Pixels from the model's left edge to the block's left edge.</param>
    /// <param name="y">Pixels up from the floor to the block's bottom.</param>
    /// <param name="z">Pixels back from the model's front to the block's front.</param>
    /// <param name="depth">Front to back, in pixels, which is also how many rows of the picture are the top.</param>
    /// <exception cref="ArgumentOutOfRangeException">A position is below zero or the depth is below 1.</exception>
    /// <exception cref="InvalidOperationException">The mod's DLL has no such embedded resource.</exception>
    public TimbnModelFaces Block(string pngName, int x, int y, int z, int depth) => Add(TimbnFaceShape.Block, pngName, x, y, z, depth);

    /// <summary>Adds a solid block from one picture made in code.</summary>
    /// <param name="png">The PNG file's bytes.</param>
    /// <param name="x">Pixels from the model's left edge to the block's left edge.</param>
    /// <param name="y">Pixels up from the floor to the block's bottom.</param>
    /// <param name="z">Pixels back from the model's front to the block's front.</param>
    /// <param name="depth">Front to back, in pixels, which is also how many rows of the picture are the top.</param>
    /// <param name="name">What error messages call this block. It defaults to its place in the model.</param>
    /// <exception cref="ArgumentOutOfRangeException">A position is below zero or the depth is below 1.</exception>
    public TimbnModelFaces Block(byte[] png, int x, int y, int z, int depth, string? name = null) => Add(TimbnFaceShape.Block, png, name, x, y, z, depth);

    private TimbnModelFaces Add(TimbnFaceShape shape, string pngName, int x, int y, int z, int depth)
    {
        var png = TimbnEmbedded.Read(_assembly, pngName + ".png");
        return Add(shape, png, pngName, pngName, x, y, z, depth);
    }

    private TimbnModelFaces Add(TimbnFaceShape shape, byte[] png, string? name, int x, int y, int z, int depth) =>
        Add(shape, png, name, png, x, y, z, depth);

    // A name used twice is one picture used twice, so the face gets a number to tell the two apart in an error.
    private TimbnModelFaces Add(TimbnFaceShape shape, byte[] png, string? name, object key, int x, int y, int z, int depth)
    {
        name ??= $"{shape} {Pictures.Count + 1}";
        if (Pictures.Any(picture => picture.Request.Name == name))
            name = $"{name} #{Pictures.Count + 1}";

        var request = new TimbnFaceRequest(shape, name, x, y, z, depth);
        TimbnFaceLayout.Validate(request);
        Pictures.Add(new(png, key, request));
        return this;
    }
}
