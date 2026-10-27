using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads and drives the game's own screens, such as which window is open and the fade to black. See the UI
/// property for things a mod shows that have to be taken down again.
/// </summary>
public static class TimbnUI
{
    /// <summary>
    /// The game window on top, such as the inventory, a chest, or the pause menu, or null when the player is in the
    /// world with nothing open.
    /// </summary>
    public static LazyWidgetBase? ActiveWindow => LazyWindowsStackController.ActiveWindow;

    /// <summary>True while the screen is fading to or from black. A hotkey or a scripted change should wait until it ends.</summary>
    public static bool IsScreenFading => Fade is { } fade && fade.IsFadeShowing;

    // LazyUI.Get throws on its null element table until the game's UI is set up.
    private static UIFade? Fade => LazyUI.IsInitialized ? LazyUI.Get<UIFade>() : null;

    /// <summary>Whether the game window on top is of the given type, such as the building window.</summary>
    /// <example>
    /// <code>
    /// if (TimbnUI.IsWindowOpen&lt;UIBuildingWindow&gt;())
    ///     BuildPins.Draw();
    /// </code>
    /// </example>
    public static bool IsWindowOpen<TWindow>() where TWindow : LazyWidgetBase => ActiveWindow is TWindow;

    /// <summary>
    /// Shows a line of text in the game's own notification popup, the one it uses for things like a closed shop. The
    /// text is looked up by its localization key, so add it with Text.Add or a lang file. It can name an item or an
    /// NPC with the game's #(id) markup.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnUI.Notify("my_mod_parcel_lost");
    /// </code>
    /// </example>
    /// <param name="key">The text's localization key.</param>
    public static void Notify(string key)
    {
        if (!TimbnGame.IsInGame)
            return;

        TimbnLocale.WarnIfMissing(key);
        LazySingleton<UINotificator>.Instance?.ShowSimpleTextNotification(key);
    }

    /// <summary>Shows a line of text in the game's notification popup like <see cref="Notify(string)"/>, with an icon beside it.</summary>
    /// <example>
    /// <code>
    /// TimbnUI.Notify("my_mod_bar_open", "i_cup_beer");
    /// </code>
    /// </example>
    /// <param name="key">The text's localization key.</param>
    /// <param name="iconId">An icon shown beside the text, such as an item's i_cup_beer or one added with Sprites.AddPng.</param>
    public static void Notify(string key, string iconId)
    {
        if (!TimbnGame.IsInGame)
            return;

        TimbnLocale.WarnIfMissing(key);
        LazySingleton<UINotificator>.Instance?.ShowSimpleTextWithIconNotification(key, iconId);
    }

    /// <summary>
    /// Shows a line of text in the game's notification popup like <see cref="Notify(string)"/>, unless the same text is
    /// on screen already, for a warning that would otherwise stack up every frame.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnUI.NotifyOnce("my_mod_conveyor_loop");
    /// </code>
    /// </example>
    /// <param name="key">The text's localization key.</param>
    public static void NotifyOnce(string key)
    {
        if (!TimbnGame.IsInGame)
            return;

        TimbnLocale.WarnIfMissing(key);
        LazySingleton<UINotificator>.Instance?.ShowSimpleTextNotificationOneTime(key);
    }

    /// <summary>
    /// Fades the screen to black, shows a line of text in the game's own title style, and fades back, the way the game
    /// introduces a new day or a place. It does nothing while another fade is showing. The text is looked up by its
    /// localization key, so add it with Text.Add or a lang file.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnUI.FadeText("my_mod_three_days_later", onTextShown: () => SkipDays(3));
    /// </code>
    /// </example>
    /// <param name="key">The text's localization key.</param>
    /// <param name="onTextShown">Runs once the text has faded out and the screen is still black, which is the moment to change the world.</param>
    /// <param name="onDone">Runs when the screen is back.</param>
    public static void FadeText(string key, Action? onTextShown = null, Action? onDone = null)
    {
        if (!TimbnGame.IsInGame || !LazyUI.IsInitialized || LazyUI.Get<UIFadeWithText>() is not { } fade || fade.IsFadeShowing)
            return;

        TimbnLocale.WarnIfMissing(key);

        // The game runs these inside its tween, and a throw there stops the fade back, which leaves the screen black.
        fade.ScreenTextFade(
            key,
            onTextShown: onTextShown == null ? null : () => TimbnSafe.Run(onTextShown, TimbnCorePlugin.Logger, $"{nameof(TimbnUI)}.{nameof(FadeText)}|The text shown handler"),
            onScreenFadeOut: onDone == null ? null : () => TimbnSafe.Run(onDone, TimbnCorePlugin.Logger, $"{nameof(TimbnUI)}.{nameof(FadeText)}|The done handler"));
    }

    // Runs the code straight away when the game has no fade to show or one is already showing.
    internal static void FadeThrough(Action whileBlack)
    {
        var fade = Fade;
        if (fade == null || fade.IsFadeShowing)
        {
            whileBlack();
            return;
        }

        fade.Fade(onInCompleted: () => TimbnSafe.Run(whileBlack, $"{nameof(TimbnUI)}|Code run during a fade"));
    }
}
