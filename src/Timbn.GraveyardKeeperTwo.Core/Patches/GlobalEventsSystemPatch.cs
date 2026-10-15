namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(GlobalEventsSystem))]
internal static class GlobalEventsSystemPatch
{
    [HarmonyPatch(nameof(GlobalEventsSystem.FireTrigger))]
    [HarmonyPostfix]
    private static void FireTriggerPostFix(GlobalEventsSystem.Event.Type type, string id) =>
        TimbnPatchedEvents.RaiseTriggered(type, id);
}
