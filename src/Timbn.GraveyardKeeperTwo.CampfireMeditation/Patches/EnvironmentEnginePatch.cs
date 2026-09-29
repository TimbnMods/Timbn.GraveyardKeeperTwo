namespace Timbn.GraveyardKeeperTwo.CampfireMeditation.Patches;

[HarmonyPatch(typeof(EnvironmentEngine))]
internal static class EnvironmentEnginePatch
{
    [HarmonyPatch("ApplyPreset")]
    [HarmonyPostfix]
    private static void ApplyPresetPostFix() =>
        MeditationLighting.Darken();
}
