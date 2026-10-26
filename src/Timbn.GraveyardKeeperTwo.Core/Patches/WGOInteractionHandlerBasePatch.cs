namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Draws and runs the actions plugins add to the game's objects. Handlers that have an action of their own on a key override these and are left alone.</summary>
[HarmonyPatch(typeof(WGOInteractionHandlerBase))]
internal static class WGOInteractionHandlerBasePatch
{
    [HarmonyPatch(nameof(WGOInteractionHandlerBase.GetInteractionInfos))]
    [HarmonyPostfix]
    private static void GetInteractionInfosPostFix(Wgo ___assignedWgo, InteractionInfos __result) =>
        TimbnInteractions.AddHints(___assignedWgo, __result);

    [HarmonyPatch(nameof(WGOInteractionHandlerBase.HasInteraction))]
    [HarmonyPostfix]
    private static void HasInteractionPostFix(Wgo ___assignedWgo, ref bool __result)
    {
        if (!__result && TimbnInteractions.Has(___assignedWgo, primary: true))
            __result = true;
    }

    [HarmonyPatch(nameof(WGOInteractionHandlerBase.Interact))]
    [HarmonyPrefix]
    private static bool InteractPreFix(Wgo ___assignedWgo, ref bool __result)
    {
        if (!TimbnInteractions.Interact(___assignedWgo, primary: true))
            return true;

        __result = true;
        return false;
    }

    [HarmonyPatch(nameof(WGOInteractionHandlerBase.HasInteraction2))]
    [HarmonyPostfix]
    private static void HasInteraction2PostFix(Wgo ___assignedWgo, ref bool __result)
    {
        if (!__result && TimbnInteractions.Has(___assignedWgo, primary: false))
            __result = true;
    }

    [HarmonyPatch(nameof(WGOInteractionHandlerBase.Interact2))]
    [HarmonyPrefix]
    private static bool Interact2PreFix(Wgo ___assignedWgo, ref bool __result)
    {
        if (!TimbnInteractions.Interact(___assignedWgo, primary: false))
            return true;

        __result = true;
        return false;
    }
}
