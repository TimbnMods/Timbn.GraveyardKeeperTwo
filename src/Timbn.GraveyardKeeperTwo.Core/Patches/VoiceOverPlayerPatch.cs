using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the game's voice player that runs the voice line filters.</summary>
[HarmonyPatch(typeof(VoiceOverPlayer))]
internal static class VoiceOverPlayerPatch
{
    [HarmonyPatch(nameof(VoiceOverPlayer.Play), typeof(string), typeof(VoiceID))]
    [HarmonyPrefix]
    private static bool PlayPreFix(VoiceOverPlayer __instance, string id) =>
        TimbnVoice.AllowLine(__instance, id);
}
