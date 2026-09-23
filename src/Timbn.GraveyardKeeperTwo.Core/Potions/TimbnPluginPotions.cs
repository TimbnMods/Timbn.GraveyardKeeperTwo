namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds potions to the game on behalf of a plugin. Each potion becomes a real item, buff, and alchemy
/// formula that brews at the mixer and saves like the game's own, and is removed when the plugin unloads.
/// Reach it through the plugin's Potions property.
/// </summary>
public sealed class TimbnPluginPotions
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginPotions(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a potion as an item, buff, alchemy formula, and set of ingredient mixes, with its names and
    /// descriptions in the loaded language, and runs its buff hooks while the player has the buff. Registering
    /// an id that is already registered replaces it, ending the old copy's buff first.
    /// </summary>
    /// <param name="potion">The potion to add. Its runes must not match any other formula, or it is skipped with a warning.</param>
    /// <returns>A handle that removes the potion early when disposed. You don't need to keep it.</returns>
    public IDisposable Register(TimbnPotion potion) => _owner.Subscriptions.Add(TimbnPotions.Register(potion));
}
