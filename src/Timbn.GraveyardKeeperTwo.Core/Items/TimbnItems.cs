using System.ComponentModel;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Finds, gives, and drops items in the loaded save. At the main menu every count is zero and nothing moves.</summary>
public static class TimbnItems
{
    /// <summary>
    /// Counts an item everywhere in the world, meaning the player's inventory, every storage and craft inventory,
    /// and every item lying on the ground or queued to drop. It stops looking once <paramref name="enough"/> is
    /// reached, so asking "is there at least this many" is cheap.
    /// </summary>
    /// <example>
    /// <code>
    /// var hasEnough = TimbnItems.CountAnywhere("flitch", 2) >= 2;
    /// </code>
    /// </example>
    /// <param name="id">The item id.</param>
    /// <param name="enough">The count at which to stop looking. The default counts everything.</param>
    /// <returns>The count found, which is at least <paramref name="enough"/> if it stopped early.</returns>
    public static int CountAnywhere(string id, int enough = int.MaxValue)
    {
        if (!TimbnGame.IsInGame)
            return 0;

        var count = MainGame.PlayerData.inventory.Data.GetTotalCountInInventory(id);
        if (count >= enough)
            return count;

        foreach (var scene in TimbnWorld.Scenes)
        {
            foreach (var wgo in scene.wgoDataList)
            {
                if (wgo.Definition?.hasRefToOtherWgoInventory == true)
                    continue;

                count += wgo.Inventory?.Data?.GetTotalCountInInventory(id) ?? 0;
                count += wgo.CraftInventory?.Data?.GetTotalCountInInventory(id) ?? 0;
            }

            count += CountDrops(scene.droppedItems, id) + CountDrops(scene.queuedDrops, id);
            if (count >= enough)
                return count;
        }

        return count;
    }

    private static int CountDrops(List<DropData>? drops, string id) =>
        drops?.Where(drop => IsLive(drop) && drop.Id == id).Sum(drop => drop.Count) ?? 0;

    /// <summary>
    /// Finds items lying on the ground as pickups, including ones queued to drop in a scene that is not loaded
    /// right now. Drops already being picked up or removed are skipped.
    /// </summary>
    /// <example>
    /// Every log on the ground in the player's scene.
    /// <code>
    /// var logs = TimbnItems.FindDrops(drop => drop.Id == "wood", TimbnPlayer.Scene);
    /// </code>
    /// </example>
    /// <param name="match">Which drops to keep, such as by their Id.</param>
    /// <param name="scene">Only look in this scene. Leave it out to look in every scene of the world.</param>
    /// <returns>Each matching drop with the scene it lies in, which is what <see cref="RemoveDrops"/> takes.</returns>
    public static List<(GameSceneData Scene, DropData Drop)> FindDrops(Func<DropData, bool> match, GameSceneData? scene = null)
    {
        List<(GameSceneData Scene, DropData Drop)> found = [];
        if (!TimbnGame.IsInGame)
            return found;

        foreach (var each in scene is null ? TimbnWorld.Scenes : [scene])
        {
            foreach (var drop in (each.droppedItems ?? []).Concat(each.queuedDrops ?? []))
            {
                if (IsLive(drop) && match(drop))
                    found.Add((each, drop));
            }
        }

        return found;
    }

    /// <summary>Removes drops found with <see cref="FindDrops"/> from the ground. The items are gone.</summary>
    /// <param name="drops">The drops and the scenes they lie in.</param>
    /// <returns>How many items were removed, counting each drop's stack.</returns>
    public static int RemoveDrops(IEnumerable<(GameSceneData Scene, DropData Drop)> drops)
    {
        var removed = 0;
        foreach (var (scene, drop) in drops.ToList())
        {
            if (!IsLive(drop))
                continue;

            removed += drop.Count;
            scene.RemoveDrop(drop);
        }

        return removed;
    }

    private static bool IsLive(DropData? drop) => drop?.Item != null && !drop.IsRemoving;

    /// <summary>
    /// What one of an item sells for, the price the town pallets and the game's orders pay, in the game's raw money
    /// units where 100 is one silver coin.
    /// </summary>
    /// <example>
    /// <code>
    /// var price = TimbnItems.SalePrice("cook_muffin");
    /// </code>
    /// </example>
    /// <param name="id">The item id.</param>
    /// <returns>The price, or 0 for an item the game doesn't have or sells for nothing.</returns>
    public static int SalePrice(string id) => TimbnBalance.Loaded?.GetDataOrNull<ItemDef>(id)?.basePrice ?? 0;

    /// <summary>
    /// The sprite id of an item's icon, the one its inventory cell shows, for anything that takes a sprite id, such as
    /// a quest's Icon.
    /// </summary>
    /// <example>
    /// <code>
    /// .Icon(run => TimbnItems.IconId(run.Item("dish")))
    /// </code>
    /// </example>
    /// <param name="id">The item id.</param>
    /// <returns>The sprite id, or an empty string for an item the game doesn't have.</returns>
    public static string IconId(string id) => TimbnBalance.Loaded?.GetDataOrNull<ItemDef>(id) is { } item ? TimbnBalance.GetIcon(item) : "";

    // Released mods such as Living Town 1.0.0 still call these, so they forward instead of doing nothing.
    [Obsolete("Use TimbnPlayer.GiveItem. This goes away in the next major version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int GiveToPlayer(Item item) => TimbnPlayer.GiveItem(item, notify: false);

    [Obsolete("Use TimbnPlayer.CountItem. This goes away in the next major version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int CountOnPlayer(string id) => TimbnPlayer.CountItem(id);

    [Obsolete("Use TimbnPlayer.TakeItem. This goes away in the next major version.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int TakeFromPlayer(string id, int count) => TimbnPlayer.TakeItem(id, count);

    /// <summary>Drops an item on the ground as a pickup. Nothing happens at the main menu.</summary>
    /// <param name="item">The item and how many.</param>
    /// <param name="sceneId">The scene to drop in.</param>
    /// <param name="position">Where to drop it.</param>
    public static void DropAt(Item item, string sceneId, Vector3 position)
    {
        if (TimbnGame.IsInGame)
            MainGame.Instance.dropSystem.DropItemAsDropView(item, sceneId, position);
    }

    /// <summary>
    /// The spot in front of the player where the game itself drops things, or the player's own position when that
    /// spot is blocked.
    /// </summary>
    /// <returns>A position in the player's current scene, or zero at the main menu.</returns>
    public static Vector3 PlayerDropPosition()
    {
        if (!TimbnGame.IsInGame)
            return Vector3.zero;

        var player = MainGame.PlayerData;
        var position = player.position.Value;
        return SpecialPhysicsCastUtils.GetPlayerDropPosition(position, player.Direction, out var drop) ? drop : position;
    }
}
