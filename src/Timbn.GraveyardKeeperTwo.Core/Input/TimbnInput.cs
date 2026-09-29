using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Tells a mod's hotkeys when the game would take the same key press itself, so a mod key does not fire behind an
/// open window or in the middle of a cutscene. Use it in place of checking <see cref="TimbnGame.IsInGame"/> and
/// <c>LazyWindowsStackController.ActiveWindow</c> by hand.
/// </summary>
public static class TimbnInput
{
    /// <summary>
    /// True when a save is loaded, no game window (inventory, crafting, pause menu, a text field) is open, and the
    /// game is reading input. This is the check for a hotkey that should work whatever the player is doing in the
    /// world, such as an unstuck key.
    /// </summary>
    public static bool CanUseHotkeys =>
        IsReadingInput
        && TimbnUI.ActiveWindow == null;

    /// <summary>
    /// True when <see cref="CanUseHotkeys"/> is and the player can also move and act. The game takes control away
    /// during dialog, cutscenes, sleep, teleports, fishing, building, ladders, work at a station, and attacks, and
    /// its own input code checks <c>PlayerController.IsControlsEnabled</c> the same way. Use it for a hotkey that
    /// acts on the world, such as spawning an item at the player.
    /// </summary>
    public static bool PlayerHasControl =>
        CanUseHotkeys
        && MainGame.PlayerController != null
        && MainGame.PlayerController.IsControlsEnabled;

    /// <summary>
    /// True when a save is loaded and nothing but the listed reasons has taken the player's control, whether or not
    /// a window is open. Use it when some of the game's reasons should not count, such as an open window in
    /// <c>TakenControlType.ByUI</c>.
    /// </summary>
    /// <example>
    /// Stop meditating when anything but a window takes control, such as a cutscene or sleep.
    /// <code>
    /// if (!TimbnInput.PlayerHasControlExcept(TakenControlType.ByUI))
    ///     meditation.Stop();
    /// </code>
    /// </example>
    /// <param name="ignored">The reasons to ignore.</param>
    /// <returns>True when no other reason has taken control.</returns>
    public static bool PlayerHasControlExcept(params TakenControlType[] ignored) =>
        TimbnGame.IsInGame
        && MainGame.PlayerController != null
        && MainGame.PlayerController.IsControlsEnabledExcept(ignored);

    /// <summary>
    /// True when the game has its own interaction ready for the Interaction key, such as a world object in reach
    /// or a big drop to pick up. A mod that also uses the Interaction key should stay out of the way then.
    /// </summary>
    public static bool GameHasInteraction
    {
        get
        {
            var interaction = MainGame.PlayerController?.PlayerInteractionComponent;
            return interaction != null && (interaction.HasWgoUnderInteraction || interaction.BigDropUnderInteraction != null);
        }
    }

    private static bool IsReadingInput =>
        TimbnGame.IsInGame
        && LazyInput.IsInitialized
        && LazyInput.IsInputActive();

    /// <summary>
    /// Whether <paramref name="shortcut"/> was pressed this frame while <see cref="CanUseHotkeys"/> holds. Call it
    /// from <c>OnUpdate</c> in place of <c>IsDown()</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// if (PluginConfig.UnstuckKey.Value.IsDownInGame())
    ///     Unstuck.Run(PluginConfig.UnstuckRange.Value);
    /// </code>
    /// </example>
    public static bool IsDownInGame(this KeyboardShortcut shortcut) => CanUseHotkeys && shortcut.IsDown();

    /// <summary>
    /// Whether <paramref name="shortcut"/> was pressed this frame while <see cref="PlayerHasControl"/> holds, so the
    /// key does nothing during dialog, cutscenes, or any other time the player cannot act.
    /// </summary>
    public static bool IsDownWithControl(this KeyboardShortcut shortcut) => PlayerHasControl && shortcut.IsDown();

    /// <summary>
    /// Whether <paramref name="shortcut"/> was pressed this frame while a game window of the given type is open and
    /// on top, for a key that adds to one of the game's own windows.
    /// </summary>
    /// <example>
    /// <code>
    /// if (PluginConfig.TogglePin.Value.IsDownInWindow&lt;UIBuildingWindow&gt;())
    ///     buildPins.TogglePinUnderCursor();
    /// </code>
    /// </example>
    /// <typeparam name="TWindow">The window's type.</typeparam>
    public static bool IsDownInWindow<TWindow>(this KeyboardShortcut shortcut) where TWindow : LazyWidgetBase =>
        IsReadingInput && TimbnUI.IsWindowOpen<TWindow>() && shortcut.IsDown();
}
