namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(GameBalance))]
internal static class GameBalancePatch
{
    [HarmonyPatch(nameof(GameBalance.LoadGameBalance))]
    [HarmonyPostfix]
    private static void LoadGameBalancePostFix()
    {
        if (TimbnBalance.Loaded is not { } balance)
            return;

        TimbnBalance.OnBalanceLoaded(balance);
        TimbnPatchedEvents.RaiseBalanceLoaded(balance);
    }
}
