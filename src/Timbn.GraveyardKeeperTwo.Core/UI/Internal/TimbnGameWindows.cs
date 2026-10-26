using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Opens two of the game's own windows with data Core builds, the right click menu and the item count picker.</summary>
internal static class TimbnGameWindows
{
    internal static IDisposable Menu(IReadOnlyList<TimbnMenuItem> items, Vector2? position, ManualLogSource logger, Action? closed)
    {
        var window = LazyUI.IsInitialized ? LazyUI.GetWindow<UIContextMenuWindow>() : null;
        if (!TimbnGame.IsInGame || window == null || window.IsShown || items.Count == 0)
            return TimbnUndo.None;

        var options = items.Select(item => new UIContextMenuWindowWidgetData(
            item.Label,
            () =>
            {
                window.Close();
                TimbnSafe.Run(item.OnPressed, logger, $"{nameof(TimbnPluginUI)}.{nameof(TimbnPluginUI.ShowMenu)}|The menu line '{item.Label}'");
            },
            item.Enabled)).ToList();

        // With a gamepad, or a pointer outside the window, there is nowhere to anchor it, so it opens in the middle.
        var screen = new Rect(0f, 0f, Screen.width, Screen.height);
        var pointer = (Vector2)Input.mousePosition;
        var at = position ?? (!LazyInput.IsGamepadActive && screen.Contains(pointer) ? pointer : screen.center);
        window.Open(new UIContextMenuWindowData { Options = options, Position = at }, _ => closed?.Invoke());
        return new TimbnUndo(() =>
        {
            if (window != null && window.IsShown)
                window.Close();
        });
    }

    internal static IDisposable ItemPicker(Func<string, bool>? filter, Action<string> onChosen, string? headerKey, ManualLogSource logger, Action? closed)
    {
        var window = LazyUI.IsInitialized ? LazyUI.GetWindow<UIMultiInventoryWindow>() : null;
        if (!TimbnGame.IsInGame || window == null || window.IsShown)
            return TimbnUndo.None;

        // The filter runs for every cell as the game draws its window, so one that throws turns the cell down and is logged once.
        var failed = false;

        // The game's own window for picking an item, the player's inventory and the chests nearby, where the cells the
        // condition turns down are dimmed and a click on one does nothing.
        var data = new UIMultiInventoryWindowData(
            MainGame.PlayerData,
            cell =>
            {
                if (cell.DisplayingItem is not { IsEmpty: false } item)
                    return;

                window.Close();
                TimbnSafe.Run(() => onChosen(BaseId(item)), logger, $"{nameof(TimbnPluginUI)}.{nameof(TimbnPluginUI.ShowItemPicker)}|The chosen handler");
            },
            item => item is { IsEmpty: false } && (filter == null || Allows(filter, BaseId(item), logger, ref failed)),
            true,
            headerKey);
        window.Open(data, _ => closed?.Invoke());
        return new TimbnUndo(() =>
        {
            if (window != null && window.IsShown)
                window.Close();
        });
    }

    // A stack can carry a suffix after the item's own id, such as body_certificate:3, which no item definition has.
    private static string BaseId(Item item) => item.Definition?.id ?? item.id;

    private static bool Allows(Func<string, bool> filter, string itemId, ManualLogSource logger, ref bool failed)
    {
        try
        {
            return filter(itemId);
        }
        catch (Exception exception)
        {
            if (!failed)
                logger.LogError($"{nameof(TimbnPluginUI)}.{nameof(TimbnPluginUI.ShowItemPicker)}|The filter threw, so the items it threw on are turned down. {exception}");

            failed = true;
            return false;
        }
    }

    internal static IDisposable ItemCount(string itemId, int min, int max, Action<int> onConfirm, ManualLogSource logger, Action? closed)
    {
        var window = LazyUI.IsInitialized ? LazyUI.GetWindow<UIItemCountWindow>() : null;
        if (!TimbnGame.IsInGame || window == null || window.IsShown || GameBalance.Me.GetDataOrNull<ItemDef>(itemId) is null)
            return TimbnUndo.None;

        var data = new UIItemCountWindowData
        {
            Item = new Item(itemId, max),
            Min = min,
            Max = max,
            OnConfirm = count => TimbnSafe.Run(() => onConfirm(count), logger, $"{nameof(TimbnPluginUI)}.{nameof(TimbnPluginUI.ShowItemCount)}|The confirm handler"),
            PriceCalculateDel = _ => 0,
            IsForVendor = false,
            OkBtnData = new UIDialogWindowData.ButtonData(null, LLBase.L(TimbnGameIds.Texts.BtnOk), null, true, GameKey.Select),
            BackBtnData = new UIDialogWindowData.ButtonData(null, LLBase.L(TimbnGameIds.Texts.BtnCancel), null, true, GameKey.Back),
        };
        window.Open(data, _ => closed?.Invoke());
        return new TimbnUndo(() =>
        {
            if (window != null && window.IsShown)
                window.Close();
        });
    }
}
