namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSession
{
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
