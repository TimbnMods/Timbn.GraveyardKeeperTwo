using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the modded voice player that runs the voice line filters.</summary>
[HarmonyPatch(typeof(ModdedVoiceOverPlayer))]
internal static class ModdedVoiceOverPlayerPatch
{
    [HarmonyPatch(nameof(ModdedVoiceOverPlayer.Play), typeof(string), typeof(VoiceID))]
    [HarmonyPrefix]
    private static bool PlayPreFix(ModdedVoiceOverPlayer __instance, string id) =>
        TimbnVoice.AllowLine(__instance, id);
}
