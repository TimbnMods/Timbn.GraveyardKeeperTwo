using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads where the player is in the loaded save and what they know, and makes the one shot changes that need no
/// undo, such as giving money, changing a resource, unlocking a craft or moving them. See the plugin's Player
/// property for changes that have to be undone, such as holding the player still.
/// </summary>
public static class TimbnPlayer
{
    private const string _coinsSound = TimbnGameIds.Sounds.CoinsSound;

    /// <summary>
    /// Whether the game itself has taken the player's control, such as for a cutscene, sleep, or a teleport.
    /// Control taken by mods with the plugin's Player.TakeControl does not count, so a mod holding the player can
    /// use it to notice the game stepping in.
    /// </summary>
    /// <example>
    /// Stop meditating when the game takes control for anything but an open window.
    /// <code>
    /// if (TimbnPlayer.GameHasControl(TakenControlType.ByUI))
    ///     meditation.Stop();
    /// </code>
    /// </example>
    /// <param name="ignored">The game's reasons that should not count, such as TakenControlType.ByUI for open windows.</param>
    /// <returns>True when the game has taken control for any other reason. False at the main menu.</returns>
    public static bool GameHasControl(params TakenControlType[] ignored) => TimbnStoryScene.GameHasPlayer(ignored);

    /// <summary>The player's money in the game's raw units, where 100 is one silver coin. 0 at the main menu.</summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.Money &gt;= 500)
    ///     Logger.LogInfo("The player has 5 silver.");
    /// </code>
    /// </example>
    public static int Money => TimbnGame.IsInGame ? MainGame.PlayerData.GetResInt(LazyConsts.MONEY_KEY) : 0;

    /// <summary>
    /// Takes money from the player, as much as they have up to the amount, such as a fine. The game shows the loss the
    /// way it shows any change to the player's money.
    /// </summary>
    /// <example>
    /// <code>
    /// var paid = TimbnPlayer.TakeMoney(250);
    /// </code>
    /// </example>
    /// <param name="amount">How much to take, in the game's raw units.</param>
    /// <returns>How much was taken, less than the amount when the player had less. 0 at the main menu.</returns>
    public static int TakeMoney(int amount)
    {
        var taken = Math.Min(Math.Max(amount, 0), Money);
        if (taken > 0)
            ChangeMoney(-taken);

        return taken;
    }

    /// <summary>
    /// Gives the player money, such as a customer paying, with the game's coin sound and the notice it shows for any
    /// money the player gets.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.GiveMoney(45);
    /// </code>
    /// </example>
    /// <param name="amount">How much to give, in the game's raw units. Nothing happens for 0 or less.</param>
    public static void GiveMoney(int amount)
    {
        if (!TimbnGame.IsInGame || amount <= 0)
            return;

        ChangeMoney(amount);
        LazyAudio.PlayAndForget(_coinsSound);
    }

    /// <summary>
    /// The item the player carries over their head, such as a log, or null when they carry nothing or at the main
    /// menu. A player with extra carry slots can carry several, and this is the one on top, the one the
    /// interaction key puts down first. See the plugin's Player.Carry to hand them one.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.Carried?.id == "wood")
    ///     Logger.LogInfo("The player carries a log.");
    /// </code>
    /// </example>
    public static Item? Carried => TimbnCarry.Current;

    /// <summary>
    /// Whether the player's hands are free, with nothing over their head in any carry slot and nothing held out in
    /// front of them the way they hold seeds. False at the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.HandsFree)
    ///     _mug = Player.Carry(new Item("my_mod_mug"));
    /// </code>
    /// </example>
    public static bool HandsFree =>
        TimbnGame.IsInGame && MainGame.PlayerData.OverheadCount == 0 && !MainGame.PlayerData.HasInteractingItem;

    private static void ChangeMoney(int amount)
    {
        if (PlayerMoneyGameResSystem.GetSystem() is { } money)
            money.Add(amount);
        else
            MainGame.PlayerData.AddRes(LazyConsts.MONEY_KEY, amount);
    }

    /// <summary>
    /// One of the player's resources, the string keyed numbers the game keeps on them, such as energy, insanity, a
    /// reputation (see <see cref="TimbnNpcs.ReputationOf"/>) or money. 0 for a resource they do not have and at the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.GetResource("insanity") &gt; 50f)
    ///     Logger.LogInfo("The keeper is losing it.");
    /// </code>
    /// </example>
    /// <param name="id">The resource's id, such as insanity.</param>
    /// <returns>Its value, or 0.</returns>
    public static float GetResource(string id) => TimbnGame.IsInGame ? MainGame.PlayerData.GetRes(id) : 0f;

    /// <summary>
    /// Adds to one of the player's resources, or takes from it with a negative amount, the way a quest reward or
    /// a buff does. The game's own limits apply, so energy never goes past its maximum. For money prefer
    /// <see cref="GiveMoney"/> and <see cref="TakeMoney"/>, which show the game's coin toast.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.AddResource("insanity", -10f);
    /// </code>
    /// </example>
    /// <param name="id">The resource's id, such as insanity.</param>
    /// <param name="amount">How much to add. Negative takes.</param>
    /// <returns>False at the main menu, when there is no player.</returns>
    public static bool AddResource(string id, float amount)
    {
        if (!TimbnGame.IsInGame)
            return false;

        MainGame.PlayerData.AddRes(id, amount);
        return true;
    }

    /// <summary>Sets one of the player's resources to a value, such as filling their energy. The game's own limits apply.</summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.SetResource("insanity", 0f);
    /// </code>
    /// </example>
    /// <param name="id">The resource's id, such as insanity.</param>
    /// <param name="value">The new value.</param>
    /// <returns>False at the main menu, when there is no player.</returns>
    public static bool SetResource(string id, float value)
    {
        if (!TimbnGame.IsInGame)
            return false;

        MainGame.PlayerData.SetRes(id, value);
        return true;
    }

    /// <summary>The player's energy, which the game caps at its maximum. 0 at the main menu.</summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.Energy &lt; 20f)
    ///     return;
    /// </code>
    /// </example>
    public static float Energy => GetResource(LazyConsts.ENERGY_KEY);

    /// <summary>
    /// Gives the player energy, or takes it with a negative amount, the way the game's own crafting and sleep do. The game's
    /// rules apply, so energy stops at its maximum and taking it while tired adds insanity. It suits a little each frame,
    /// such as resting by a fire, since it skips the resource changed event that <see cref="AddResource"/> fires (taking
    /// energy while tired still fires it for the insanity) and does nothing while energy is full.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.AddEnergy(Time.deltaTime * 2f);
    /// </code>
    /// </example>
    /// <param name="amount">How much to add. Negative takes.</param>
    /// <returns>False at the main menu, when there is no player.</returns>
    public static bool AddEnergy(float amount)
    {
        if (!TimbnGame.IsInGame || PlayerEnergyGameResSystem.GetSystem() is not { } energy)
            return false;

        // A full bar still redraws and starts the floating number on every add, so a trickle at the top is skipped.
        if (amount <= 0f || !energy.HasMax())
            energy.Add(amount);

        return true;
    }

    /// <summary>The player's insanity. 0 at the main menu.</summary>
    public static float Insanity => GetResource(LazyConsts.INSANITY_KEY);

    /// <summary>
    /// Gives the player insanity, or calms them with a negative amount, the way the game's own crafting does. The game's
    /// limits apply. Like <see cref="AddEnergy"/> it skips the resource changed event, so it suits a little each frame.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.AddInsanity(-Time.deltaTime);
    /// </code>
    /// </example>
    /// <param name="amount">How much to add. Negative takes.</param>
    /// <returns>False at the main menu, when there is no player.</returns>
    public static bool AddInsanity(float amount)
    {
        if (!TimbnGame.IsInGame || PlayerInsanityGameResSystem.GetSystem() is not { } insanity)
            return false;

        insanity.Add(amount);
        return true;
    }

    /// <summary>How many of an item the player carries in their inventory and bags. Storage in the world does not count.</summary>
    /// <param name="id">The item id.</param>
    public static int CountItem(string id) =>
        TimbnGame.IsInGame ? MainGame.PlayerData.inventory.Data.GetTotalCountInInventory(id) : 0;

    /// <summary>Whether the player carries at least some of an item in their inventory and bags.</summary>
    /// <param name="id">The item id.</param>
    /// <param name="count">How many they need. 1 by default.</param>
    public static bool HasItem(string id, int count = 1) => CountItem(id) >= count;

    /// <summary>
    /// Puts an item in the player's inventory and bags, and shows the note the game shows when the player picks an item up.
    /// Whatever does not fit drops in front of the player so nothing is lost, the way the game drops items, so a big stack
    /// splits into several pickups.
    /// </summary>
    /// <param name="item">The item and how many.</param>
    /// <returns>How many went into the inventory.</returns>
    public static int GiveItem(Item item) => GiveItem(item, notify: true);

    /// <summary>Puts an item in the player's inventory like <see cref="GiveItem(Item)"/>, with the pickup note only when asked for.</summary>
    /// <param name="item">The item and how many.</param>
    /// <param name="notify">Whether the game's pickup note shows, as it does when the player picks an item up.</param>
    /// <returns>How many went into the inventory.</returns>
    public static int GiveItem(Item item, bool notify)
    {
        if (!TimbnGame.IsInGame)
            return 0;

        var moved = new MultiInventory(MainGame.PlayerData, addCurrentPlayerWorldZone: false).AddItem(item);
        if (moved < item.Count)
        {
            var rest = Item.Copy(item);
            rest.Count = item.Count - moved;
            var worldId = MainGame.PlayerData.currentGameSceneId;
            var position = TimbnItems.PlayerDropPosition();
            if (!MainGame.Instance.dropSystem.DropItem(rest, worldId, position))
                TimbnItems.DropAt(rest, worldId, position);
        }

        if (notify && moved > 0)
            ShowPickupNote(item.id, moved);

        return moved;
    }

    private static readonly Action<UINotificator, List<Item>>? _showAdded =
        TimbnReflection.TryDelegate<Action<UINotificator, List<Item>>>(() => AccessTools.Method(typeof(UINotificator), "HandleAddItems"), "UINotificator.HandleAddItems");

    // The note the game shows at the side of the screen when the player picks an item up, with its pickup sound.
    private static void ShowPickupNote(string id, int count)
    {
        if (_showAdded != null && UINotificator.Instance is { } notificator)
            TimbnSafe.Run(() => _showAdded(notificator, [new Item(id, count)]), TimbnCorePlugin.Logger, $"{nameof(TimbnPlayer)}|The item added note");
    }

    /// <summary>
    /// Removes an item from the player's inventory and bags, up to the count asked for. The items are gone, so check
    /// <see cref="HasItem"/> first when the player has to have enough.
    /// </summary>
    /// <param name="id">The item id.</param>
    /// <param name="count">How many to take.</param>
    /// <returns>How many were taken, which is less than <paramref name="count"/> when the player had fewer.</returns>
    public static int TakeItem(string id, int count) =>
        TimbnGame.IsInGame
            ? new MultiInventory(MainGame.PlayerData, addCurrentPlayerWorldZone: false).RemoveItemByCount(id, count).Sum(item => item.Count)
            : 0;

    /// <summary>Whether the player has the game's tired debuff. False at the main menu.</summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.IsTired)
    ///     return;
    /// </code>
    /// </example>
    public static bool IsTired => HasPerk(TimbnGameIds.Buffs.LackOfSleepDebuff);

    /// <summary>Whether the player has bought a level of an inspiration, such as the second graveyard expansion.</summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.HasInspiration("cemetery_expander_2"))
    ///     Logger.LogInfo("The graveyard has grown twice.");
    /// </code>
    /// </example>
    /// <param name="inspirationId">The level's id, such as cemetery_expander_2.</param>
    /// <returns>False when it is not bought, the inspiration is missing, or at the main menu.</returns>
    public static bool HasInspiration(string inspirationId)
    {
        if (!TimbnGame.IsInGame)
            return false;

        var cut = inspirationId.LastIndexOf('_');
        var name = cut < 0 ? inspirationId : inspirationId[..cut];
        return TalentSystemCache.Instance.inspirations.TryGetValue(name, out var inspiration)
            && inspiration.PurchasedInspirations.Exists(level => level.id == inspirationId);
    }

    /// <summary>Whether the player has a perk.</summary>
    /// <example>
    /// <code>
    /// var greenThumb = TimbnPlayer.HasPerk("perk_green_thumb");
    /// </code>
    /// </example>
    /// <param name="id">The perk's id, such as perk_green_thumb.</param>
    /// <returns>True when the player has it. False at the main menu.</returns>
    public static bool HasPerk(string id) =>
        TimbnGame.IsInGame && MainGame.Instance.GameSave?.perkSystemData?.HasPerk(id) == true;

    /// <summary>
    /// Gives the player a perk, such as the tired debuff or a food buff, the way an item or a script does. A perk the
    /// player already has is refreshed by its own add rule. The perk is saved with the game, so use it for the game's
    /// own perks and for effects that should stay, and use Potions for a buff of your own that must end with the mod.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.AddPerk(TimbnGameIds.Buffs.LackOfSleepDebuff);
    /// </code>
    /// </example>
    /// <param name="id">The perk's id from the balance, such as lack_of_sleep_debuff.</param>
    /// <returns>False at the main menu or when no perk has that id.</returns>
    public static bool AddPerk(string id)
    {
        if (!TimbnGame.IsInGame || !PerkExists(id))
            return false;

        MainGame.Instance.GameSave.perkSystemData.AddPerk(id);
        return true;
    }

    /// <summary>
    /// Takes a perk away from the player and runs what the perk does when it ends, such as giving back a resource. A
    /// perk the player does not have is left alone.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.RemovePerk(TimbnGameIds.Buffs.LackOfSleepDebuff);
    /// </code>
    /// </example>
    /// <param name="id">The perk's id from the balance.</param>
    /// <returns>True when the player had the perk. False at the main menu or when they did not.</returns>
    public static bool RemovePerk(string id)
    {
        if (!HasPerk(id))
            return false;

        MainGame.Instance.GameSave.perkSystemData.RemovePerk(id);
        return true;
    }

    private static bool PerkExists(string id) => TimbnBalance.Loaded?.GetDataOrNull<PerkDef>(id) != null;

    /// <summary>Whether the player has researched a technology.</summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.HasTech("wood_basic"))
    ///     Logger.LogInfo("Wood basics are researched.");
    /// </code>
    /// </example>
    /// <param name="id">The technology's id, such as wood_basic.</param>
    /// <returns>True when it is researched. False at the main menu.</returns>
    public static bool HasTech(string id) =>
        TimbnGame.IsInGame && MainGame.Instance.GameSave?.knowledgeSystem?.IsTechUnlocked(id) == true;

    /// <summary>
    /// Researches a technology for the player for free, the way the game does when its tech points are spent, so the
    /// recipes, buildings, perks and other unlocks it brings come with it. A technology already researched stays as it is.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.UnlockTech("wood_basic");
    /// </code>
    /// </example>
    /// <param name="id">The technology's id, such as wood_basic.</param>
    /// <returns>True when it was researched now. False when it already was, the game has no such technology, or at the main menu.</returns>
    public static bool UnlockTech(string id)
    {
        if (!TimbnGame.IsInGame || HasTech(id) || TimbnBalance.Loaded?.GetDataOrNull<TechDef>(id) is not { } tech)
            return false;

        tech.Unlock(free: true);
        return true;
    }

    /// <summary>
    /// Whether the player can make a recipe, either one the game gives from the start or one they have unlocked, such
    /// as a dish learned from a cookbook or a tech. It reads the craft's isNeedsUnlock and the save's unlocked crafts,
    /// the way a station does before it offers the recipe.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.HasCraft(craft.id))
    ///     dishes.Add(craft.id);
    /// </code>
    /// </example>
    /// <param name="craftId">The recipe's id from the balance.</param>
    /// <returns>True when it needs no unlock or is unlocked. False at the main menu.</returns>
    public static bool HasCraft(string craftId)
    {
        if (!TimbnGame.IsInGame)
            return false;

        if (TimbnBalance.Loaded?.GetDataOrNull<CraftDef>(craftId) is { isNeedsUnlock: false })
            return true;

        return MainGame.Instance.GameSave?.knowledgeSystem?.unlockedCrafts.Contains(craftId) == true;
    }

    /// <summary>
    /// Every recipe the player can make right now, the ones the game gives from the start and the ones they have
    /// unlocked, the same set <see cref="HasCraft"/> says yes to. Pass station ids to keep only recipes made at any of
    /// them, or none for every station. The result is a snapshot, so call it again after the player learns something.
    /// </summary>
    /// <example>
    /// <code>
    /// var dishes = TimbnPlayer.KnownCrafts(
    ///     TimbnGameIds.Workstations.Placed.KitchenOven,
    ///     TimbnGameIds.Workstations.Placed.KitchenTable);
    /// </code>
    /// </example>
    /// <param name="stationIds">Station ids such as <see cref="TimbnGameIds.Workstations.Placed.KitchenOven"/>, or none for all.</param>
    /// <returns>The recipes themselves, empty at the main menu.</returns>
    public static CraftDef[] KnownCrafts(params string[] stationIds)
    {
        if (!TimbnGame.IsInGame || TimbnBalance.Loaded is not { } balance)
            return [];

        var unlocked = MainGame.Instance.GameSave?.knowledgeSystem?.unlockedCrafts;
        return [.. balance.craftDefs
            .Where(craft => stationIds.Length == 0 || craft.craftsIn.Any(stationIds.Contains))
            .Where(craft => !craft.isNeedsUnlock || unlocked?.Contains(craft.id) == true)];
    }

    /// <summary>
    /// Unlocks a recipe for the player the way a cookbook or a study does, so its station offers it from now on. A
    /// recipe already unlocked stays as it is.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.UnlockCraft("my_mod_fish_pie");
    /// </code>
    /// </example>
    /// <param name="craftId">The recipe's id from the balance.</param>
    /// <returns>False at the main menu, when there is no player to teach.</returns>
    public static bool UnlockCraft(string craftId)
    {
        if (!TimbnGame.IsInGame || MainGame.Instance.GameSave?.knowledgeSystem is not { } knowledge)
            return false;

        knowledge.UnlockCraft(craftId);
        return true;
    }

    /// <summary>
    /// Whether the player has built one of the buildings the game only allows once, such as the choir or the
    /// organ. The game marks such a building as locked once it is built, so this reads that list.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.HasBuilt("zmb_choir_unlock_s"))
    ///     TimbnQuests.FireStoryTrigger(GlobalEventsSystem.Event.Type.BuildBuilding, "zmb_choir_unlock");
    /// </code>
    /// </example>
    /// <param name="buildingId">The building's id from the balance, such as zmb_choir_unlock_s.</param>
    /// <returns>True when it has been built. False at the main menu.</returns>
    public static bool HasBuilt(string buildingId) =>
        TimbnGame.IsInGame && MainGame.Instance.GameSave?.knowledgeSystem?.lockedBuildings.Contains(buildingId) == true;

    /// <summary>The player's position in their current scene, or zero at the main menu.</summary>
    public static Vector3 Position => TimbnGame.IsInGame ? MainGame.PlayerData.position.Value : Vector3.zero;

    /// <summary>The id of the scene the player is in, such as the graveyard or an interior, or null at the main menu.</summary>
    public static string? SceneId => TimbnGame.IsInGame ? MainGame.PlayerData.currentGameSceneId : null;

    /// <summary>
    /// The scene the player is in, holding its world objects and the items dropped on its ground, or null at the
    /// main menu.
    /// </summary>
    public static GameSceneData? Scene =>
        SceneId is { Length: > 0 } id ? MainGame.WorldData.GetGameSceneDataById(id) : null;

    /// <summary>
    /// The zone the player stands in, such as the garden or the morgue, or null at the main menu and in the open
    /// between zones. Zones are the game's named areas, each with its own build area and workers.
    /// </summary>
    public static WorldZoneData? Zone => TimbnGame.IsInGame ? MainGame.PlayerData.CurrentWorldZoneData : null;

    /// <summary>The id of the zone the player stands in, such as garden, or null when they stand in none.</summary>
    /// <example>
    /// <code>
    /// var atHome = TimbnPlayer.ZoneId is "garden" or "inquisitions_base";
    /// </code>
    /// </example>
    public static string? ZoneId => Zone?.id;

    /// <summary>
    /// Moves the player to a spot in their current scene, the way the game moves them, and sets the lighting and weather
    /// for where they land the way a door does, so no rain shows indoors. Pair it with
    /// <see cref="TimbnWorld.TryFindWalkable"/> so they land on open ground.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnWorld.TryFindWalkable(TimbnPlayer.Position, 5f, out var ground))
    ///     TimbnPlayer.TeleportTo(ground);
    /// </code>
    /// </example>
    /// <param name="position">Where to put the player.</param>
    /// <returns>False at the main menu, when there is no player to move.</returns>
    public static bool TeleportTo(Vector3 position)
    {
        if (!TimbnGame.IsInGame || MainGame.PlayerController == null)
            return false;

        MainGame.PlayerController.SetPosition(position);
        EnvironmentEngine.Instance?.SetTimeOfDayPreset(TimbnWorld.EnvironmentAt(position));
        return true;
    }

    /// <summary>
    /// Moves the player to one of the game's points the way its doors do, into another scene when the point is in one,
    /// with the lighting and weather for where they land. They keep the way they were facing.
    /// </summary>
    /// <example>
    /// <code>
    /// TimbnPlayer.TeleportTo("player_spawn_point_home");
    /// </code>
    /// </example>
    /// <param name="pointId">The point's id, as <see cref="TimbnWorld.Point"/> takes it.</param>
    /// <returns>False at the main menu or when no point has that id.</returns>
    public static bool TeleportTo(string pointId) =>
        TimbnGame.IsInGame
        && TimbnWorld.Point(pointId) is { } point
        && PlayerController.Teleport(new GDPointTeleportData(point, TimbnWorld.EnvironmentAt(point.Position)));

    /// <summary>
    /// Turns the player to face a spot, such as the campfire they sit at. While a plugin holds the player's control the
    /// player keeps facing it, even as the mouse moves, until every hold has let go or a walk or slide starts. Without a
    /// hold the player turns and then moves freely again.
    /// </summary>
    /// <example>
    /// <code>
    /// _control = Player.TakeControl();
    /// TimbnPlayer.Face(fire);
    /// </code>
    /// </example>
    /// <param name="position">The spot to face. Its height is ignored.</param>
    /// <returns>False at the main menu, when there is no player to turn.</returns>
    public static bool Face(Vector3 position) =>
        TimbnGame.IsInGame && MainGame.PlayerController != null && TimbnPlayerControl.Face(MainGame.PlayerController, position);
}
