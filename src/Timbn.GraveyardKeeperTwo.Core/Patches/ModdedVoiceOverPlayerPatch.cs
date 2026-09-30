using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(ModdedVoiceOverPlayer))]
internal static class ModdedVoiceOverPlayerPatch
{
    [HarmonyPatch(nameof(ModdedVoiceOverPlayer.Play), typeof(string), typeof(VoiceID))]
    [HarmonyPrefix]
    private static bool PlayPreFix(ModdedVoiceOverPlayer __instance, string id) =>
        TimbnVoice.AllowLine(__instance, id);
}
