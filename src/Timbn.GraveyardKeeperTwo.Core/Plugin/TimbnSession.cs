namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Opens and closes Core's per save state, calling each subsystem in a stated order when a save starts and ends.</summary>
internal static class TimbnSession
{
    /// <summary>Raised first on the way to the main menu, while the save is still loaded, for plugins' SaveClosed handlers.</summary>
    internal static event Action? Closing;

    internal static void Open(TimbnSubscriptions session)
    {
        TimbnGame.OnGameStarted();
        TimbnSaves.OnGameStarted();
        TimbnDialog.OnGameStarted();
        session.Add(TimbnGameEvents.QuestStarted(TimbnQuests.OnQuestStarted));
        TimbnPotions.OnGameStarted(session);
    }

    internal static void Close()
    {
        Closing?.Invoke();
        TimbnGame.OnLeftGame();
        TimbnSaves.OnLeftGame();
        TimbnGlobalSaves.Flush();
        TimbnDialog.OnLeftGame();
        ReleaseHolds();
    }

    internal static void ReleaseHolds()
    {
        TimbnMovement.ReleaseAll();
        TimbnControl.ReleaseAll();
        TimbnGameSpeed.ReleaseAll();
        TimbnClockPause.ReleaseAll();
    }
}
