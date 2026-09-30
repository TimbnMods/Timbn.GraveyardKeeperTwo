namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the interaction icon that swaps in a plugin's tinted speech bubble for its talks.</summary>
[HarmonyPatch(typeof(InteractionEvent))]
internal static class InteractionEventPatch
{
    [HarmonyPatch(nameof(InteractionEvent.CustomIcon), MethodType.Getter)]
    [HarmonyPostfix]
    private static void CustomIconPostFix(InteractionEvent __instance, ref string __result) =>
        __result = TimbnDialog.IconFor(__instance.str, __result);
}
