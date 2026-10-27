namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>
/// Picking a seed or fertilizer to plant puts down whatever the player holds and then sets the seed. A held item
/// under an Exclusive or OnlyByCode rule is either kept (OnlyByCode) or dropped without the mod asking, so the pick is
/// refused while one is held. Otherwise the seed would sit beside an item that was meant to be alone in their hands.
/// </summary>
[HarmonyPatch(typeof(PlayerData))]
internal static class PlayerDataInteractingItemPatch
{
    [HarmonyPatch(nameof(PlayerData.SetInteractingItem))]
    [HarmonyPrefix]
    private static bool SetInteractingItemPreFix() =>
        !TimbnOverhead.HandsLocked;
}
