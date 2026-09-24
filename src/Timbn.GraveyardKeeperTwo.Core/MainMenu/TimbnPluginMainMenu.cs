namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts a plugin's messages on the main menu using the game's own UI, either as a line of text next to the
/// game's version or as a popup in the game's dialog window. Lines are removed when the plugin unloads.
/// Every Timbn plugin that starts already gets a line with its name and version, so this is for anything
/// beyond that. Reach it through the plugin's MainMenu property.
/// </summary>
public sealed class TimbnPluginMainMenu
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginMainMenu(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a line to the list of Timbn mods above the main menu's "game by" credit, drawn like the game's own
    /// "published by: tinyBuild". Lines are listed in the order they were added, and show only on the main
    /// menu, never in game.
    /// </summary>
    /// <param name="label">The plain part, for example "Vanilla Tweaks: ".</param>
    /// <param name="value">The highlighted part, for example "1.1.0".</param>
    /// <returns>A handle that removes the line early when disposed. You don't need to keep it.</returns>
    public IDisposable AddLine(string label, string value) => _owner.Subscriptions.Add(TimbnMainMenu.AddLine(label, value));

    /// <summary>
    /// Shows a popup with an OK button in the game's dialog window the next time the main menu is on screen, or
    /// right away if it already is. Each popup shows once, and several queue up one after another.
    /// </summary>
    /// <param name="header">The window title, which the game requires.</param>
    /// <param name="text">The message.</param>
    /// <returns>A handle that cancels the popup if it has not shown yet. You don't need to keep it.</returns>
    public IDisposable Popup(string header, string text) => _owner.Subscriptions.Add(TimbnMainMenu.AddPopup(header, text));
}
