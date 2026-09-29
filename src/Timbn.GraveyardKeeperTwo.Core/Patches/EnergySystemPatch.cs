namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(EnergySystem))]
internal static class EnergySystemPatch
{
    [HarmonyPatch(nameof(EnergySystem.StartSleeping))]
    [HarmonyPostfix]
    private static void StartSleepingPostFix(EnergySystem __instance)
    {
        if (__instance.IsInTransitionBetweenSleep)
            TimbnPatchedEvents.RaiseSleepStarted();
    }

    [HarmonyPatch(nameof(EnergySystem.StopSleeping))]
    [HarmonyPostfix]
    private static void StopSleepingPostFix() =>
        TimbnPatchedEvents.RaiseSleepEnded();
}
