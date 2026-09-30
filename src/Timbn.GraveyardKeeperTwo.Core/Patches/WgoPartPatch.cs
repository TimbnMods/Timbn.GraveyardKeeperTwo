namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on a zombie view's skin setup that raises the zombie view ready event.</summary>
[HarmonyPatch(typeof(WgoPart))]
internal static class WgoPartPatch
{
    [HarmonyPatch("SetupZombieSkin")]
    [HarmonyPostfix]
    private static void SetupZombieSkinPostFix(WgoPart __instance, ZombieWgoData zombieWgoData) =>
        TimbnPatchedEvents.RaiseZombieViewReady(__instance, zombieWgoData);
}
