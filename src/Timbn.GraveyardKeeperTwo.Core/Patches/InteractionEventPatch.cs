namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(InteractionEvent))]
internal static class InteractionEventPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(InteractionEvent.CustomIcon), MethodType.Getter)]
    private static void CustomIconPostFix(InteractionEvent __instance, ref string __result) =>
        __result = TimbnDialog.IconFor(__instance.str, __result);
}
