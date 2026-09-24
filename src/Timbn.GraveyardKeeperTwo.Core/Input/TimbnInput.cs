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
        TimbnGame.IsInGame
        && LazyInput.IsInitialized
        && LazyInput.IsInputActive()
        && LazyWindowsStackController.ActiveWindow == null;

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
}
