namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>
/// Keeps a zombie seated at a station made with a zombie spot from taking orders. The game treats it as a caretaker and
/// asks its zone for an order every update, so the update is skipped. The zombie stays a caretaker, so the game's own
/// pickup still takes it off the seat.
/// </summary>
[HarmonyPatch(typeof(ZombieWgoData))]
internal static class ZombieWgoDataSeatPatch
{
    [HarmonyPatch("CaretakerUpdateBehaviour")]
    [HarmonyPrefix]
    private static bool CaretakerUpdateBehaviourPreFix(ZombieWgoData __instance) => !TimbnZombieSpots.KeepsSeated(__instance);
}
