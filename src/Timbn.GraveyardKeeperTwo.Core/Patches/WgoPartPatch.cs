namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(WgoPart))]
internal static class WgoPartPatch
{
    [HarmonyPatch("SetupZombieSkin")]
    [HarmonyPostfix]
    private static void SetupZombieSkinPostFix(WgoPart __instance, ZombieWgoData zombieWgoData) =>
        TimbnPatchedEvents.RaiseZombieViewReady(__instance, zombieWgoData);

    [HarmonyPatch("ApplyWgoPartState", typeof(WgoPartState))]
    [HarmonyPostfix]
    private static void ApplyWgoPartStatePostFix(WgoPart __instance) => TimbnPartPrefab.PlaceDock(__instance);
}
