using System.ComponentModel;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts a plugin's messages on the main menu as popups in the game's dialog window. Reach it through the
/// plugin's MainMenu property.
/// </summary>
public sealed class TimbnPluginMainMenu
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginMainMenu(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    [Obsolete("Only Timbn Core shows a line on the main menu, so this does nothing. It goes away in the next major version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IDisposable AddLine(string label, string value) => TimbnUndo.None;

    /// <summary>
    /// Shows a popup with an OK button in the game's dialog window the next time the main menu is on screen, or
    /// right away if it already is. Each popup shows once, and several queue up one after another.
    /// </summary>
    /// <param name="header">The window title. The game logs an error when it is empty.</param>
    /// <param name="text">The message.</param>
    /// <returns>A handle that cancels the popup if it has not shown yet. You don't need to keep it.</returns>
    public IDisposable AddPopup(string header, string text) => _owner.Subscriptions.Add(TimbnMainMenu.AddPopup(header, text));
}
