namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds custom sprites on behalf of a plugin, such as icons for its items and buffs. The game finds sprites
/// by name, so a sprite added here shows anywhere its name is used as an icon id. Sprites are removed and
/// their textures freed when the plugin unloads. Reach it through the plugin's Sprites property.
/// </summary>
public sealed class TimbnPluginSprites
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginSprites(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a PNG under a sprite name. Images smaller than the game's 48 pixel icon canvas are centred on one,
    /// point filtered at 50 pixels per unit like the game's own art.
    /// </summary>
    /// <param name="name">The sprite name, used as an item's or buff's IconId.</param>
    /// <param name="png">The PNG file's bytes, usually read from a resource embedded in the plugin's DLL.</param>
    /// <returns>A handle that removes the sprite early when disposed. You don't need to keep it.</returns>
    public IDisposable AddPng(string name, byte[] png) => _owner.Subscriptions.Add(TimbnSprites.AddPng(name, png));
}
