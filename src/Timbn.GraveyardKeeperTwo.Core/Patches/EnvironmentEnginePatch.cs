namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Tells the clock hold when the game pauses or unpauses the clock itself, so a release does not undo the game's pause.</summary>
[HarmonyPatch(typeof(EnvironmentEngine))]
internal static class EnvironmentEnginePatch
{
    [HarmonyPatch(nameof(EnvironmentEngine.IsPaused), MethodType.Setter)]
    [HarmonyPostfix]
    private static void IsPausedSetterPostFix(bool value) =>
        TimbnClockPause.OnGameSet(value);
}
