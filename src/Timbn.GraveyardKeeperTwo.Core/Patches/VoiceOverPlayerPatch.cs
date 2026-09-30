using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(VoiceOverPlayer))]
internal static class VoiceOverPlayerPatch
{
    [HarmonyPatch(nameof(VoiceOverPlayer.Play), typeof(string), typeof(VoiceID))]
    [HarmonyPrefix]
    private static bool PlayPreFix(VoiceOverPlayer __instance, string id) =>
        TimbnVoice.AllowLine(__instance, id);
}
