namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds or overrides the game's text on behalf of a plugin. Quest names, dialog lines, item names, and any
/// other text the game shows is looked up by localization key, and the keys added here are written into the
/// loaded language and every language loaded later. They are removed when the plugin unloads, putting back
/// any shipped text they overrode. Reach it through the plugin's Text property.
/// </summary>
/// <remarks>
/// Core also loads the plugin's lang folder next to its DLL when the plugin starts, so players can translate a mod
/// without rebuilding it. Each file is named after a game language id, such as lang/en.txt, lang/de.txt, or
/// lang/pt-br.txt, and holds one "timbn_larry_boards_given = Here, take these." line per key. Lines starting with #
/// are comments, and \n in a text is a line break. The game shows the line for its current language and falls back
/// to en.txt for any key a translation lacks, so en.txt should hold every key. Switching language in the game's
/// options picks up the new text. Text from lang files wins over text added in code for the same key. Ship the
/// files by marking them CopyToOutputDirectory in the csproj.
/// </remarks>
public sealed class TimbnPluginText
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginText(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>Adds or overrides one localization key.</summary>
    /// <param name="key">The key the game looks the text up by.</param>
    /// <param name="text">The text to show.</param>
    /// <returns>A handle that removes the key early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(string key, string text) => _owner.Subscriptions.Add(TimbnLocale.Add(key, text));

    /// <summary>Adds or overrides several localization keys. When the same key is added more than once, the most recently added text wins, except that text from a lang file always beats text added in code.</summary>
    /// <param name="texts">Text by key.</param>
    /// <returns>A handle that removes the keys early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(IReadOnlyDictionary<string, string> texts) => _owner.Subscriptions.Add(TimbnLocale.Add(texts));

    internal void AddLanguageFilesIfPresent()
    {
        if (TimbnLanguageFiles.Exists(_owner.Folder))
            _owner.Subscriptions.Add(TimbnLocale.AddFromFiles(TimbnLanguageFiles.Read(_owner.Folder, _owner.PluginLogger)));
    }
}
