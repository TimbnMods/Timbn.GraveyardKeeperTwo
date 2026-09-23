namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Whether a save is loaded and playable.</summary>
public static class TimbnGame
{
    private static bool _started;

    /// <summary>True from GameStarted until the game begins returning to the main menu.</summary>
    public static bool IsInGame =>
        _started
        && MainGame.Instance != null
        && MainGame.Instance.gameState == MainGame.GameState.InGame;

    internal static void OnGameStarted() => _started = true;

    internal static void OnLeftGame() => _started = false;
}
