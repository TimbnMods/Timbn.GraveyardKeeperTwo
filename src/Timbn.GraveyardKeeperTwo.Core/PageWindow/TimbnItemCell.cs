using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's item cell, the square that holds an icon and a count in an inventory. Give it an item's id, such as one
/// from TimbnGameIds.Items, and it draws the item's own icon. Any other id is looked up as an icon, such as one added
/// with Sprites.Add and the Item kind, and one the game has no sprite for shows its name where the icon would be. Leave the id out for
/// an empty cell, such as one the player fills with <see cref="Chosen"/>, which shows the plus the study table shows on
/// its empty slot. The cell keeps the game's own size in the middle of its row.
/// </summary>
/// <example>
/// <code>
/// var cell = page.Add(new TimbnItemCell(TimbnGameIds.Items.Apple, 25));
/// cell.SetItem(TimbnGameIds.Items.Apple, 24);
/// </code>
/// </example>
/// <param name="id">An item's id, an icon's id, or null for an empty cell.</param>
/// <param name="count">The count printed on the cell.</param>
public sealed class TimbnItemCell(string? id = null, int count = 1) : TimbnComponent
{
    private const float _emptyCell = 42f;

    private UIItemCell? _cell;
    private IDisposable? _picker;
    private GameObject? _plus;
    private string? _id = id;
    private int _count = count;

    /// <summary>Called when the player clicks the cell, or presses A on it with a gamepad. The cell is a stop for the gamepad either way.</summary>
    public Action? Pressed { get; set; }

    /// <summary>
    /// The items the player may pick when <see cref="Chosen"/> is set, or null to let every item be picked. The window
    /// dims the items this turns down.
    /// </summary>
    public Func<string, bool>? Filter { get; set; }

    /// <summary>
    /// Makes a click on the cell, or A on a gamepad, open the game's inventory to pick an item for it. The cell shows the
    /// picked item and this is called with its id. Leave it null for a cell that only shows an item.
    /// </summary>
    public Action<string>? Chosen { get; set; }

    /// <summary>
    /// Shows another item or count in the cell, such as when the count changes. Before the cell is on a page it sets what
    /// the cell shows once it is.
    /// </summary>
    /// <param name="itemOrIconId">An item's id, an icon's id, or null to empty the cell.</param>
    /// <param name="newCount">The count printed on the cell.</param>
    public void SetItem(string? itemOrIconId, int newCount)
    {
        _id = itemOrIconId;
        _count = newCount;

        // Unity's own null check, since the cell is destroyed with its page and a mod may keep the component for the next one.
        if (_cell == null)
            return;

        if (_plus != null)
            _plus.SetActive(itemOrIconId == null);

        if (itemOrIconId == null)
            _cell.DrawEmptyInteractable();
        else if (GameBalance.Me.GetDataOrNull<ItemDef>(itemOrIconId) is not null)
            _cell.Draw(new Item(itemOrIconId, newCount));
        else
            _cell.DrawCustom(itemOrIconId, newCount, interactable: true);

        // Drawing an empty cell clears the cell's callbacks, so the press is hooked up again after every draw.
        _cell.OnItemCellPress = _ => Run(Press, "press");
    }

    private void Press()
    {
        Pressed?.Invoke();
        if (Chosen == null)
            return;

        _picker?.Dispose();
        _picker = TimbnGameWindows.ItemPicker(Filter, Choose, null, Watcher?.Logger ?? TimbnCorePlugin.Logger, () => _picker = null);
    }

    private void ClosePicker()
    {
        _picker?.Dispose();
        _picker = null;
    }

    private void Choose(string picked)
    {
        // The page may have closed while the picker was open, even with its mod unloaded, so nothing is left to tell.
        if (Root == null)
            return;

        SetItem(picked, _count);
        Chosen?.Invoke(picked);
    }

    protected internal override RectTransform Build(Transform parent)
    {
        // A page's column stretches what it holds across the page, so the cell sits in a row of its own to keep its size and
        // the gamepad's frame around the cell alone.
        var row = TimbnUi.Row(parent, "ItemCell", align: TextAnchor.MiddleCenter);
        _cell = TimbnTheme.ItemCell(row);
        // The game's cell is copied from a window that only exists once a save is loaded, so before that the page keeps an empty space where it goes.
        if (_cell == null)
        {
            TimbnUi.Fixed(TimbnUi.Box(row, "Empty"), _emptyCell, _emptyCell);
            return row;
        }

        var size = ((RectTransform)_cell.transform).rect.size;
        TimbnUi.Fixed(_cell, size.x, size.y);
        _plus = TimbnTheme.SlotPlus(_cell.transform);
        if (_plus != null)
            TimbnUi.Element(_plus.transform).ignoreLayout = true;

        // The cell looked for a gamepad stop when it woke up and found none, so it is asked again to hook its press up to this one.
        _cell.gameObject.AddComponent<GamepadNavigationItem>();
        _ = _cell.GamepadNavigationItem;
        TimbnWidget.MarkSelect(_cell);
        // Cloning a part turns off the click handlers set in the prefab, which is how the cell hears a click.
        _cell.LazyButton.onClick.AddListener(_cell.OnPress);
        // The game's picker is its own window, so the page closing, its region building again or an F6 reload leaves it open
        // with this cell's filter unless the cell closes it as it goes.
        _cell.gameObject.AddComponent<PickerCloser>().Cell = this;
        SetItem(_id, _count);
        return row;
    }

    private sealed class PickerCloser : MonoBehaviour
    {
        internal TimbnItemCell? Cell { get; set; }

        private void OnDestroy() => Cell?.ClosePicker();
    }
}
