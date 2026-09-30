namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(WgoPart))]
internal static class WgoPartPatch
{
    [HarmonyPatch("SetupZombieSkin")]
    [HarmonyPostfix]
    private static void SetupZombieSkinPostFix(WgoPart __instance, ZombieWgoData zombieWgoData) =>
        TimbnPatchedEvents.RaiseZombieViewReady(__instance, zombieWgoData);
}
