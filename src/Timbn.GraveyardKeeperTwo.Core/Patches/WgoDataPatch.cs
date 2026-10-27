namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>
/// Starts a plugin's talk when the player interacts with an NPC and that talk is next in line, keeps plugin talks
/// queued behind the game's own events, tells Core who the player talks to in the game's own talks, and tells a
/// schedule when the game fires an event on a scheduled person outside the player's interaction. When an object leaves
/// the world, the navmesh under it is rebuilt without it.
/// </summary>
[HarmonyPatch(typeof(WgoData))]
internal static class WgoDataPatch
{
    [HarmonyPatch(nameof(WgoData.FireInteractionEvent))]
    [HarmonyPrefix]
    private static bool FireInteractionEventPreFix(WgoData __instance, ref bool __result, out bool __state)
    {
        TimbnGameMoves.InteractionStarted();
        __state = true;
        if (TimbnDialog.TryHandleInteraction(__instance))
        {
            __result = true;
            return false;
        }

        if (__instance.PeekFirstAddedEvent() is { isFake: false })
            TimbnNpcTalk.GameTalkStarted(__instance);

        return true;
    }

    [HarmonyPatch(nameof(WgoData.FireInteractionEvent))]
    [HarmonyFinalizer]
    private static void FireInteractionEventFinalizer(bool __state)
    {
        if (__state)
            TimbnGameMoves.InteractionEnded();
    }

    [HarmonyPatch(nameof(WgoData.FireEvent))]
    [HarmonyPrefix]
    private static void FireEventPreFix(WgoData __instance, string eventName)
    {
        if (eventName == TimbnNpcMenu.InteractionEvent)
            TimbnNpcTalk.GameTalkStarted(__instance);

        TimbnGameMoves.OnEvent(__instance, eventName);
    }

    [HarmonyPatch(nameof(WgoData.OnCraftStart))]
    [HarmonyPostfix]
    private static void OnCraftStartPostFix(WgoData __instance, CraftElementBase ce) => TimbnCraftEvents.Started(__instance, ce);

    [HarmonyPatch(nameof(WgoData.OnCraftEnd))]
    [HarmonyPostfix]
    private static void OnCraftEndPostFix(WgoData __instance, CraftElementBase ce) => TimbnCraftEvents.Finished(__instance, ce);

    [HarmonyPatch(nameof(WgoData.AddInteractionEvent))]
    [HarmonyPostfix]
    private static void AddInteractionEventPostFix(WgoData __instance, string id) =>
        TimbnDialog.OnEventAdded(__instance, id);
}
