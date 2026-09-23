using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds definitions to the game's balance tables on behalf of a plugin. The balance is where the game keeps
/// every item, buff, quest, vendor, craft, and building, and GameBalance.Me.GetData looks them up by id.
/// Use this for a kind of definition that has no easier Core API. Definitions are removed when the plugin
/// unloads. Reach it through the plugin's Balance property.
/// </summary>
public sealed class TimbnPluginBalance
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginBalance(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a definition to the balance. It goes in straight away if the balance has already loaded, and again
    /// every time the balance loads after that.
    /// </summary>
    /// <param name="definition">The definition to add. Its id must not belong to a definition the game ships.</param>
    /// <returns>A handle that removes the definition early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(BalanceBaseObject definition) => _owner.Subscriptions.Add(TimbnBalance.Add(definition));

    /// <summary>
    /// Adds a definition like <see cref="Add(BalanceBaseObject)"/>, and calls prepare with the loaded balance
    /// right before each insert. Use it when the definition needs something from the balance first, such as
    /// values copied from one of the game's items. If another plugin already holds the same id, this one
    /// replaces it in place, which is how a hot reloaded plugin takes over from its old copy.
    /// </summary>
    /// <param name="definition">The definition to add. Its id must not belong to a definition the game ships.</param>
    /// <param name="prepare">Called with the loaded balance just before the definition goes in, on every load. Can be null.</param>
    /// <returns>A handle that removes the definition early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(BalanceBaseObject definition, Action<GameBalance>? prepare) =>
        _owner.Subscriptions.Add(TimbnBalance.Add(definition, prepare));
}
