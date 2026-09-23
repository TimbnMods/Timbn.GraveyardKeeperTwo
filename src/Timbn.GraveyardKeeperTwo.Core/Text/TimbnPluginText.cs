namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds or overrides the game's text on behalf of a plugin. Quest names, dialog lines, item names, and any
/// other text the game shows is looked up by localisation key, and the keys added here are written into the
/// loaded language and every language loaded later. They are removed when the plugin unloads, putting back
/// any shipped text they overrode. Reach it through the plugin's Text property.
/// </summary>
public sealed class TimbnPluginText
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginText(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>Adds or overrides one localisation key.</summary>
    /// <param name="key">The key the game looks the text up by.</param>
    /// <param name="text">The text to show.</param>
    /// <returns>A handle that removes the key early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(string key, string text) => _owner.Subscriptions.Add(TimbnLocale.Add(key, text));

    /// <summary>Adds or overrides several localisation keys. If another plugin adds the same key later, the newest text wins.</summary>
    /// <param name="texts">Text by key.</param>
    /// <returns>A handle that removes the keys early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(IReadOnlyDictionary<string, string> texts) => _owner.Subscriptions.Add(TimbnLocale.Add(texts));
}
