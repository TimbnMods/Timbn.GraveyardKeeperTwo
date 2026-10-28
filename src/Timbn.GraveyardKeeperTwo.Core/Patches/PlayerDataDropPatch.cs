namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>
/// Keeps an item carried under a rule the way the rule says. An OnlyByCode item is never dropped, and while an
/// Exclusive or OnlyByCode item is held anything the game would put in the player's hands lands at their feet.
/// </summary>
[HarmonyPatch(typeof(PlayerData))]
internal static class PlayerDataDropPatch
{
    [HarmonyPatch(nameof(PlayerData.DropOverheadItem), typeof(Item))]
    [HarmonyPrefix]
    private static bool DropOverheadItemPreFix(Item item) =>
        !TimbnOverhead.BlocksDrop(item);

    // Picking something up runs inside the same key handling as the plain drop, and swaps what the player holds
    // through DropOverheadItem. That swap is the game's own drop, so it is not blocked, or the player would end up
    // holding both.
    [HarmonyPatch(nameof(PlayerData.AddOverheadItem))]
    [HarmonyPrefix]
    private static bool AddOverheadItemPreFix(Item item, out bool __state)
    {
        __state = TimbnOverhead.IsReadingInput;
        TimbnOverhead.IsReadingInput = false;
        if (!TimbnOverhead.HandsLocked || item == null || item.IsEmpty)
            return true;

        // Whatever called this has already taken the item out of the world, so it goes on the ground rather than away.
        TimbnItems.DropAt(item, MainGame.PlayerData.currentGameSceneId, TimbnPlayer.Position);
        return false;
    }

    [HarmonyPatch(nameof(PlayerData.AddOverheadItem))]
    [HarmonyFinalizer]
    private static void AddOverheadItemFinalizer(bool __state) =>
        TimbnOverhead.IsReadingInput = __state;
}
