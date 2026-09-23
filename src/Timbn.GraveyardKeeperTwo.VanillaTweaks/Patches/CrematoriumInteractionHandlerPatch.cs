using LazyBearTechnology;
using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(CrematoriumInteractionHandler), nameof(CrematoriumInteractionHandler.Interact))]
internal static class CrematoriumInteractionHandlerPatch
{
    [HarmonyPrefix]
    private static bool BlockWithoutMastery(Wgo ___assignedWgo, ref bool __result)
    {
        if (!PluginConfig.CrematoriumNoStuckBodies.Value
            || ___assignedWgo == null
            || ___assignedWgo.Data == null
            || !CrematoriumStuckBody.LacksMastery(___assignedWgo.Data))
            return true;

        Bubble.Talk(new PhraseData(isPlayer: true, null, "gardening_no_mastery", null, null, SpeechBubbleType.Think));
        __result = false;
        return false;
    }
}
