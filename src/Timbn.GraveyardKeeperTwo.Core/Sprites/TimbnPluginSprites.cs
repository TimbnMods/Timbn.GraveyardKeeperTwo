namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds custom sprites for a mod, such as icons for its items and buffs. The game finds sprites by name, so a
/// sprite added here shows anywhere its name is used as an icon id. Sprites are removed and their textures freed
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
    /// Adds a PNG under a sprite name. An image smaller than 48 pixels (the size of the game's icons) is centered
    /// on a 48 pixel canvas. A larger image is squashed with a warning. A PNG that cannot be read is skipped with a
    /// warning. Sprites use point filtering and 50 pixels per unit, like the game's own art.
    /// </summary>
    /// <param name="name">The sprite name, used as an item's or buff's IconId.</param>
    /// <param name="png">The PNG file's bytes, usually read from a resource embedded in the mod's DLL.</param>
    /// <returns>A handle that removes the sprite early when disposed. You don't need to keep it.</returns>
    public IDisposable AddPng(string name, byte[] png) => _owner.Subscriptions.Add(TimbnSprites.AddPng(name, png));

    /// <summary>
    /// Adds a PNG embedded in the mod's DLL, like <see cref="AddPng"/>. The resource is found by the sprite name
    /// plus ".png", so embed icons with <c>LogicalName="%(Filename)%(Extension)"</c>.
    /// </summary>
    /// <param name="name">The sprite name and the resource's name without ".png", for example "timbn_smoked_fish".</param>
    /// <returns>A handle that removes the sprite early when disposed. You don't need to keep it.</returns>
    /// <exception cref="InvalidOperationException">The mod's DLL has no such embedded resource.</exception>
    public IDisposable AddEmbeddedPng(string name) => AddPng(name, TimbnEmbedded.Read(_owner.GetType().Assembly, name + ".png"));
}
