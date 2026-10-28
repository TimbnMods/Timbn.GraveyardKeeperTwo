using System.ComponentModel;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds custom sprites for a mod, such as icons for its items, stations, perks and buffs. The game finds sprites by name,
/// so a sprite added here shows anywhere its name is used as an icon id. Sprites are removed and their textures freed
/// when the mod unloads. Reach it through the Sprites property.
/// </summary>
public sealed class TimbnPluginSprites
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginSprites(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds an image under a sprite name. The <see cref="TimbnSpriteKind"/> says what the sprite is for, which sets the size
    /// the image has to be and where Core puts it, because the game draws each kind on a canvas of its own. An image that
    /// breaks its kind's rule, or cannot be read, is skipped with a warning, and the game then shows its own default
    /// icon. Sprites use point filtering and 50 pixels per unit, like the game's own art. A <see cref="TimbnImage"/>
    /// shows a sprite by name.
    /// </summary>
    /// <param name="name">The sprite name, used as an icon id such as an item's IconId or a station's Icon.</param>
    /// <param name="image">The image file's bytes, usually read from a resource embedded in the mod's DLL.</param>
    /// <param name="kind">What the sprite is for. The default keeps the image's own size.</param>
    /// <returns>A handle that removes the sprite early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(string name, byte[] image, TimbnSpriteKind kind = TimbnSpriteKind.Image) =>
        _owner.Subscriptions.Add(TimbnSprites.Add(name, image, kind));

    /// <summary>
    /// Adds an image embedded in the mod's DLL, like <see cref="Add"/>. Give the file name with its extension, which has to
    /// be .png, .jpg or .jpeg. The sprite's name is the file name without the extension, so "timbn_smoked_fish.png"
    /// adds "timbn_smoked_fish". Embed images with <c>LogicalName="%(Filename)%(Extension)"</c>. Any other extension is
    /// skipped with a warning.
    /// </summary>
    /// <param name="fileName">The embedded resource's name with its extension, for example "timbn_smoked_fish.png".</param>
    /// <param name="kind">What the sprite is for. The default keeps the image's own size.</param>
    /// <returns>A handle that removes the sprite early when disposed. You don't need to keep it.</returns>
    /// <exception cref="InvalidOperationException">The mod's DLL has no such embedded resource.</exception>
    public IDisposable AddEmbedded(string fileName, TimbnSpriteKind kind = TimbnSpriteKind.Image)
    {
        if (Path.GetExtension(fileName).ToLowerInvariant() is not (".png" or ".jpg" or ".jpeg"))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnPluginSprites)}|{fileName} is not a png or jpg. It was not added.");
            return TimbnUndo.None;
        }

        return Add(Path.GetFileNameWithoutExtension(fileName), TimbnEmbedded.Read(_owner.GetType().Assembly, fileName), kind);
    }

    /// <summary>Kept so a mod built against Core 2.1.0 still loads. It adds the image as an Image, at its own size.</summary>
    [Obsolete("Use Add with a TimbnSpriteKind.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IDisposable AddPng(string name, byte[] png) => Add(name, png);

    /// <summary>Kept so a mod built against Core 2.1.0 still loads. It adds the image as an Image, at its own size.</summary>
    [Obsolete("Use AddEmbedded with a TimbnSpriteKind.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IDisposable AddEmbeddedPng(string name) => AddEmbedded(name + ".png");
}
