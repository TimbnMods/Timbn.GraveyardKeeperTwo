namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Finds, gives, and drops items in the loaded save. At the main menu every count is zero and nothing moves.</summary>
public static class TimbnItems
{
    /// <summary>
    /// Counts an item everywhere in the world. The player's inventory, every storage and craft inventory, and
    /// every item lying on the ground or queued to drop. Stops looking once <paramref name="enough"/> is reached,
    /// so asking "is there at least this many" is cheap.
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

        foreach (var scene in MainGame.WorldData.gameSceneDataList)
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

        foreach (var each in scene is null ? MainGame.WorldData.gameSceneDataList : [scene])
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
    /// Puts an item into the player's inventory and bags, and drops whatever does not fit in front of the player
    /// so nothing is lost. The overflow is dropped the way the game drops items, so a big stack is split into several
    /// pickups. An item the game refuses to drop that way, such as fuel, still lands as a single pickup.
    /// </summary>
    /// <param name="item">The item and how many.</param>
    /// <returns>How many went into the player's inventory.</returns>
    public static int GiveToPlayer(Item item)
    {
        if (!TimbnGame.IsInGame)
            return 0;

        var moved = new MultiInventory(MainGame.PlayerData, addCurrentPlayerWorldZone: false).AddItem(item);
        if (moved < item.Count)
        {
            var rest = Item.Copy(item);
            rest.Count = item.Count - moved;
            var worldId = MainGame.PlayerData.currentGameSceneId;
            var position = PlayerDropPosition();
            if (!MainGame.Instance.dropSystem.DropItem(rest, worldId, position))
                DropAt(rest, worldId, position);
        }

        return moved;
    }

    /// <summary>Counts an item in the player's inventory and bags. Storage in the world does not count.</summary>
    /// <param name="id">The item id.</param>
    /// <returns>How many the player is carrying.</returns>
    public static int CountOnPlayer(string id) =>
        TimbnGame.IsInGame ? MainGame.PlayerData.inventory.Data.GetTotalCountInInventory(id) : 0;

    /// <summary>
    /// Removes an item from the player's inventory and bags, up to the count asked for. The items are gone, so
    /// check <see cref="CountOnPlayer"/> first when the player has to have enough.
    /// </summary>
    /// <param name="id">The item id.</param>
    /// <param name="count">How many to take.</param>
    /// <returns>How many were taken, which is less than <paramref name="count"/> when the player had fewer.</returns>
    public static int TakeFromPlayer(string id, int count) =>
        TimbnGame.IsInGame
            ? new MultiInventory(MainGame.PlayerData, addCurrentPlayerWorldZone: false).RemoveItemByCount(id, count).Sum(item => item.Count)
            : 0;

    /// <summary>Drops an item on the ground as a pickup.</summary>
    /// <param name="item">The item and how many.</param>
    /// <param name="worldId">The scene to drop in.</param>
    /// <param name="position">Where to drop it.</param>
    public static void DropAt(Item item, string worldId, Vector3 position) =>
        MainGame.Instance.dropSystem.DropItemAsDropView(item, worldId, position);

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
