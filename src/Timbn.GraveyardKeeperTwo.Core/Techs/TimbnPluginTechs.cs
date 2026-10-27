namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds technologies to the game's tech tree for a mod. The player buys them with tech points like the game's own.
///  They are removed when the mod unloads. Reach it through the Techs property. A save that bought one keeps
/// working once the mod is gone, since the game drops tech ids it doesn't know when it loads.
/// </summary>
public sealed class TimbnPluginTechs
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginTechs(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a technology to the tech tree. Call it in OnAwake before the stations and crafts that name it in their
    /// UnlockedBy, so they find it. See <see cref="TimbnTechBuilder"/> for what a tech holds.
    /// </summary>
    /// <param name="id">The tech's id, for example "timbn_infusion_rack". Keep it stable, since saves name a bought tech by it.</param>
    /// <param name="name">The name shown on the tree. Empty leaves the text to the mod's lang files under the id.</param>
    /// <param name="tech">Describes the tech.</param>
    /// <returns>A handle that removes the tech early when disposed. You don't need to keep it.</returns>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="InvalidOperationException">The tech has no cell or no tech before it.</exception>
    public IDisposable Add(string id, string name, Action<TimbnTechBuilder> tech)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("A tech needs an id.", nameof(id));

        var builder = new TimbnTechBuilder(id, name);
        tech(builder);
        return _owner.Subscriptions.Add(TimbnTechs.Add(builder));
    }
}
