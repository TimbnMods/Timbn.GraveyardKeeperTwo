using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(FishingDef))]
internal static class FishingDefPatch
{
    [HarmonyPatch(nameof(FishingDef.GetAvailableBaits))]
    [HarmonyPrefix]
    private static void GetAvailableBaitsPreFix(List<Item> baits) => BaitInBags.AddBaggedBait(baits);
}
