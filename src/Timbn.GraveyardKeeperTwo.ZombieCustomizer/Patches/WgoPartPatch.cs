namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer.Patches;

[HarmonyPatch(typeof(WgoPart))]
internal static class WgoPartPatch
{
    [HarmonyPatch("SetupZombieSkin")]
    [HarmonyPostfix]
    private static void SetupZombieSkinPostFix(WgoPart __instance, ZombieWgoData zombieWgoData) =>
        ZombieTint.Paint(__instance, ZombieTint.Read(zombieWgoData));
}
