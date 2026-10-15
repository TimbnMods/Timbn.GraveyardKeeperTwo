namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Adds the checks of a craft's Requires to the game's own start check.</summary>
[HarmonyPatch(typeof(CraftComponent))]
internal static class CraftComponentPatch
{
    // The craft window's Start button and the queue both ask this, so a failed check greys the button and a queued craft
    // waits for it, the way a missing ingredient does. A craft already running is left alone.
    [HarmonyPatch(nameof(CraftComponent.GetStartCraftStatus))]
    [HarmonyPostfix]
    private static void GetStartCraftStatusPostFix(CraftElementBase craftElement, ICraftable ___craftableObject, ref CraftStatus __result)
    {
        if (__result == CraftStatus.OK && craftElement is { IsStarted: false } && ___craftableObject is WgoData station && !TimbnCraftRequirements.Allows(craftElement.CraftId, station))
            __result = CraftStatus.Other;
    }
}
