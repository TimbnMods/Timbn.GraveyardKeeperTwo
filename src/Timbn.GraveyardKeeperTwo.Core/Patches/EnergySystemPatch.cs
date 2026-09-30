using System.Runtime.CompilerServices;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on sleeping that raises the sleep started, refused and ended events.</summary>
[HarmonyPatch(typeof(EnergySystem))]
internal static class EnergySystemPatch
{
    [HarmonyPatch(nameof(EnergySystem.StartSleeping))]
    [HarmonyPrefix]
    private static void StartSleepingPreFix(ref Action? onSleepDidNotStartedCallback, bool sleepWithoutSavingGame, out StrongBox<bool> __state)
    {
        __state = new StrongBox<bool>();
        var box = __state;
        var original = onSleepDidNotStartedCallback;
        onSleepDidNotStartedCallback = () =>
        {
            box.Value = true;
            TimbnPatchedEvents.RaiseSleepRefused(!sleepWithoutSavingGame);
            original?.Invoke();
        };
    }

    [HarmonyPatch(nameof(EnergySystem.StartSleeping))]
    [HarmonyPostfix]
    private static void StartSleepingPostFix(StrongBox<bool> __state)
    {
        if (!__state.Value)
            TimbnPatchedEvents.RaiseSleepStarted();
    }

    [HarmonyPatch(nameof(EnergySystem.StopSleeping))]
    [HarmonyPostfix]
    private static void StopSleepingPostFix() =>
        TimbnPatchedEvents.RaiseSleepEnded();
}
