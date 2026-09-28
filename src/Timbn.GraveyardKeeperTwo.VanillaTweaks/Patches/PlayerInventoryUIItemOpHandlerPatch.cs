using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(PlayerInventoryUIItemOpHandler))]
internal static class PlayerInventoryUIItemOpHandlerPatch
{
    private static readonly AccessTools.FieldRef<LazyWidget<UIContextMenuWindowData>, UIContextMenuWindowData?> _menuData =
        AccessTools.FieldRefAccess<LazyWidget<UIContextMenuWindowData>, UIContextMenuWindowData?>("data");

    [HarmonyPatch("TryDestroyItem")]
    [HarmonyPrefix]
    private static bool TryDestroyItemPreFix(UIItemCell cell)
    {
        if (!IsBlocked(cell.DisplayingItem))
            return true;

        Plugin.Logger.LogInfo($"Kept {cell.DisplayingItem.id} from being destroyed with items still inside.");
        return false;
    }

    [HarmonyPatch(nameof(PlayerInventoryUIItemOpHandler.OnPlayerInvItemPressed2))]
    [HarmonyPostfix]
    private static void OnPlayerInvItemPressed2PostFix(PlayerInventoryUIItemOpHandler __instance, UIItemCell cell)
    {
        if (__instance.IsBagShown || !IsBlocked(cell.DisplayingItem))
            return;

        var window = LazyUI.GetWindow<UIContextMenuWindow>();
        var destroy = LLBase.L("ui_destroy");
        var option = _menuData(window)?.Options?.Find(o => o.name == destroy);
        if (option == null)
            return;

        option.enabled = false;
        window.Redraw();
    }

    private static bool IsBlocked(Item? bag)
    {
        if (bag == null || !bag.IsBag)
            return false;

        return PluginConfig.BagDestroy.Value switch
        {
            BagDestroyBlocking.BlockIfNotEmpty => bag.Inventory.Any(item => !item.IsEmpty),
            BagDestroyBlocking.BlockIfImportant => bag.Inventory.Any(item => !item.IsEmpty && item.Definition?.CanNotBeDestroyed == true),
            _ => false,
        };
    }
}
