using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads and drives the game's own screens, such as which window is open and the fade to black. See the plugin's
/// UI property for things a plugin shows that have to be taken down again.
/// </summary>
public static class TimbnUI
{
    /// <summary>
    /// The game window on top, such as the inventory, a chest, or the pause menu, or null when the player is in the
    /// world with nothing open.
    /// </summary>
    public static LazyWidgetBase? ActiveWindow => LazyWindowsStackController.ActiveWindow;

    /// <summary>True while a screen fade to or from black is showing, when a hotkey or a scripted change should wait.</summary>
    public static bool IsScreenFading
    {
        get
        {
            var fade = LazyUI.Get<UIFade>();
            return fade != null && fade.IsFadeShowing;
        }
    }

    /// <summary>Whether the game window on top is of the given type, such as the building window.</summary>
    /// <example>
    /// <code>
    /// if (TimbnUI.IsWindowOpen&lt;UIBuildingWindow&gt;())
    ///     BuildPins.Draw();
    /// </code>
    /// </example>
    /// <typeparam name="TWindow">The window's type.</typeparam>
    /// <returns>True when that window is open and on top.</returns>
    public static bool IsWindowOpen<TWindow>() where TWindow : LazyWidgetBase => ActiveWindow is TWindow;

    /// <summary>
    /// Fades the screen to black, runs your code while it is black, and fades back in, the way the game hides a
    /// change such as the player sitting down or a teleport. When the game has no fade to show, or one is already
    /// showing, the code runs straight away.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnUI.FadeThrough(() => TimbnPlayer.MoveTo(campfire));
    /// </code>
    /// </example>
    /// <param name="whileBlack">The code to run once the screen is black.</param>
    public static void FadeThrough(Action whileBlack)
    {
        var fade = LazyUI.Get<UIFade>();
        if (fade == null || fade.IsFadeShowing)
        {
            whileBlack();
            return;
        }

        fade.Fade(onInCompleted: () => TimbnSafe.Run(whileBlack, $"{nameof(TimbnUI)}|Code run during a fade"));
    }
}
