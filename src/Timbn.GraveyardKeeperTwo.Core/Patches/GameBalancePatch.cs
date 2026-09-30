namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the balance load that inserts the definitions plugins added and raises the balance loaded event.</summary>
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
