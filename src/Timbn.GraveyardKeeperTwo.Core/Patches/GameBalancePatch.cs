namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
internal static class GameBalancePatch
{
    private static void Postfix()
    {
        if (TimbnBalance.Loaded is not { } balance)
            return;

        TimbnBalance.OnBalanceLoaded(balance);
        TimbnPotions.OnBalanceLoaded();
        TimbnPatchedEvents.RaiseBalanceLoaded(balance);
    }
}
