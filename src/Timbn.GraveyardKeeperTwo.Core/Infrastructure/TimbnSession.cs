namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Opens and closes Core's per save state, calling each subsystem in a stated order when a save starts and ends.</summary>
internal static class TimbnSession
{
    internal static event Action? Closing;

    internal static void Open(TimbnSubscriptions session)
    {
        Step(TimbnGame.OnGameStarted, nameof(TimbnGame));
        Step(TimbnSaves.OnGameStarted, nameof(TimbnSaves));
        Step(TimbnModContent.OnGameStarted, nameof(TimbnModContent));
        Step(TimbnQuests.OnGameStarted, nameof(TimbnQuests));
        Step(TimbnDialog.OnGameStarted, nameof(TimbnDialog));
        Step(() => session.Add(TimbnGameEvents.QuestStarted(TimbnQuests.OnQuestStarted)), nameof(TimbnGameEvents.QuestStarted));
        Step(() => TimbnPotions.OnGameStarted(session), nameof(TimbnPotions));
        Step(TimbnNpcs.OnGameStarted, nameof(TimbnNpcs));
    }

    internal static void Close()
    {
        Step(() => Closing?.Invoke(), nameof(Closing));
        Step(TimbnQuestMarker.Clear, nameof(TimbnQuestMarker));

        // Before TimbnGame, so a quest's OnEnd still sees the save, its run and its saved data while it undoes OnStart.
        Step(TimbnQuests.OnLeftGame, nameof(TimbnQuests));
        Step(TimbnGame.OnLeftGame, nameof(TimbnGame));
        Step(TimbnNpcs.OnLeftGame, nameof(TimbnNpcs));
        Step(TimbnModels.OnLeftGame, nameof(TimbnModels));
        Step(TimbnPlayerGrid.Forget, nameof(TimbnPlayerGrid));
        Step(TimbnSaves.OnLeftGame, nameof(TimbnSaves));
        Step(TimbnGlobalSaves.Flush, nameof(TimbnGlobalSaves));
        Step(TimbnDialog.OnLeftGame, nameof(TimbnDialog));
        Step(ReleaseHolds, nameof(ReleaseHolds));
    }

    internal static void ReleaseHolds()
    {
        TimbnMovement.ReleaseAll();
        TimbnPlayerControl.ReleaseAll();
        TimbnPlayerAnimation.ReleaseAll();
        TimbnGameSpeed.ReleaseAll();
        TimbnClockPause.ReleaseAll();
    }

    // Each subsystem on its own, so one throwing never skips the ones after it.
    private static void Step(Action step, string name) => TimbnSafe.Run(step, $"{nameof(TimbnSession)}|{name}");
}
