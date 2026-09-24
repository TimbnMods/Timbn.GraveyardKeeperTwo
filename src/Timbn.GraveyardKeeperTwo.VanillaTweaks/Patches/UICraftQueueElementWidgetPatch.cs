using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(UICraftQueueElementWidget), "ShowSelection")]
internal static class UICraftQueueElementWidgetPatch
{
    [HarmonyPostfix]
    private static void ShowInfiniteButton(LazyButton ___infCraftButton, UICraftQueueElementWidgetData ___data)
    {
        if (!PluginConfig.InfiniteCrafts.Value
            || ___infCraftButton == null
            || ___data == null
            || ___data.IsMulticraftDisabled)
            return;

        ___infCraftButton.gameObject.SetActive(true);
    }
}
