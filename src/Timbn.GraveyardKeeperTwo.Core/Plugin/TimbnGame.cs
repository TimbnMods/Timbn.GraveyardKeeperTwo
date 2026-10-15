namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Whether a save is loaded and playable, and writes it on demand.</summary>
public static class TimbnGame
{
    private static bool _started;

    // The game sets gameState to MainMenu before it fires OnGoToMainMenu, so a gameState check here would already be
    // false while Core closes the save.
    /// <summary>
    /// True from GameStarted through Core's save close, so an Events.SaveClosed handler and a quest's OnEnd still see
    /// the save, and false once Core has closed it or at the main menu.
    /// </summary>
    public static bool IsInGame => _started && MainGame.Instance != null;

    /// <summary>
    /// Writes the loaded save to its slot right now, the way the game does when the player sleeps. Every plugin's
    /// Events.BeforeSave and AfterSave run around it, and Core keeps mod items out of the file as always.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.SleepRefused(wouldSave => { if (wouldSave) TimbnGame.SaveNow(); });
    /// </code>
    /// </example>
    /// <returns>False at the main menu, when there is nothing to save.</returns>
    public static bool SaveNow()
    {
        if (!IsInGame || MainGame.Instance.SaveSlotData == null || MainGame.Instance.GameSave == null)
            return false;

        SaveSystem.Save(MainGame.Instance.SaveSlotData, MainGame.Instance.GameSave);
        return true;
    }

    internal static void OnGameStarted() => _started = true;

    internal static void OnLeftGame() => _started = false;
}
