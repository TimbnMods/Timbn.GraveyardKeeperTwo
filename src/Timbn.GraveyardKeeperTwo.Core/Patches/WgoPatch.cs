namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(Wgo))]
internal static class WgoPatch
{
    [HarmonyPatch(nameof(Wgo.SetSelectionTint))]
    [HarmonyPostfix]
    private static void SetSelectionTintPostFix(Wgo __instance, Color color, float amount) => TimbnModels.TintParts(__instance, color, amount);
}
