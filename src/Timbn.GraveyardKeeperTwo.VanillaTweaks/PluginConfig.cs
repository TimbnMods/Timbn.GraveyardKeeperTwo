namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

internal static class PluginConfig
{
    private const string _bugFixes = "Bug Fixes";
    private const string _softLocks = "Soft Locks";
    private const string _tweaks = "Tweaks";

    public static ConfigEntry<bool> StudyTableScienceOnly { get; private set; } = null!;

    public static ConfigEntry<bool> StudyTableNoStuckCrafts { get; private set; } = null!;

    public static ConfigEntry<bool> GreenThumbTalentBonus { get; private set; } = null!;

    public static ConfigEntry<bool> CollectStrayTechPoints { get; private set; } = null!;

    public static ConfigEntry<bool> OvenNoLostIngredients { get; private set; } = null!;

    public static ConfigEntry<bool> SermonFaithRounding { get; private set; } = null!;

    public static ConfigEntry<bool> StuckCarriers { get; private set; } = null!;

    public static ConfigEntry<bool> RemoveInWholeArea { get; private set; } = null!;

    public static ConfigEntry<bool> CollisionFixes { get; private set; } = null!;

    public static ConfigEntry<bool> BuiltEarlyQuests { get; private set; } = null!;

    public static ConfigEntry<bool> DismantleUnreachable { get; private set; } = null!;

    public static ConfigEntry<bool> SoftLockedBoards { get; private set; } = null!;

    public static ConfigEntry<KeyboardShortcut> UnstuckKey { get; private set; } = null!;

    public static ConfigEntry<float> UnstuckRange { get; private set; } = null!;

    public static ConfigEntry<bool> BedSaveWhenRested { get; private set; } = null!;

    public static ConfigEntry<int> TechPointCap { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        StudyTableScienceOnly = config.Bind(
            _bugFixes,
            nameof(StudyTableScienceOnly),
            true,
            "Stops zombies from putting anything but science in a study table built before version 1.005, "
            + "which they do when the rest of the tower's storage is full. The table is then unusable and every "
            + "science made afterwards is lost. Anything already in the table is dropped beside it when a save "
            + "loads. Tables built since 1.005 already only take science.");

        StudyTableNoStuckCrafts = config.Bind(
            _bugFixes,
            nameof(StudyTableNoStuckCrafts),
            true,
            "Frees a study table the game left stuck partway through a decompose or study, showing Work with "
            + "nothing to work on. Checked when a save loads. The item and science were already spent, so nothing "
            + "is lost.");

        GreenThumbTalentBonus = config.Bind(
            _bugFixes,
            nameof(GreenThumbTalentBonus),
            true,
            "Makes the Green Thumb perk grant its +2 green talent when planting. The game puts the value in a "
            + "field that planting crafts throw away, so the perk does nothing at all. Does nothing once the game "
            + "fixes it.");

        CollectStrayTechPoints = config.Bind(
            _bugFixes,
            nameof(CollectStrayTechPoints),
            true,
            "Pulls a tech point orb to you when it settles off the walkable ground, say through a wall, where "
            + "the game's magnet can never reach it. The game only does this when you sleep.");

        OvenNoLostIngredients = config.Bind(
            _bugFixes,
            nameof(OvenNoLostIngredients),
            true,
            "Gives the oven room for every ingredient its recipes need. It has one ingredient slot, so a zombie "
            + "cook's second ingredient (the oil for onion rings, say) has nowhere to go and is destroyed on "
            + "delivery, over and over. Also refuses any delivery a station has no room for, instead of "
            + "destroying it.");

        SermonFaithRounding = config.Bind(
            _bugFixes,
            nameof(SermonFaithRounding),
            true,
            "Rounds ceremony faith the way the numbers say. Priest makes Basic Prayer pay 0.35 faith per "
            + "parishioner, but 0.35 is stored as 0.3499999, so at 10, 30, 50 parishioners and so on 3.5 comes "
            + "out as 3.4999999 and rounds down, and Priest adds nothing.");

        StuckCarriers = config.Bind(
            _bugFixes,
            nameof(StuckCarriers),
            true,
            "When a carrier zombie has nothing in its room to put its item into, it shows the no storage icon "
            + "the game already uses for gardeners and walks back to its supplier station instead of freezing where it "
            + "stands. It goes on with its work as soon as a slot frees up, as it would anyway.");

        RemoveInWholeArea = config.Bind(
            _bugFixes,
            nameof(RemoveInWholeArea),
            true,
            "Allows you to remove buildings you should be allowed to but the game blocks its. Chests built in the " +
            "resurrection lab from the morgue build desk are an example.");

        CollisionFixes = config.Bind(
            _bugFixes,
            nameof(CollisionFixes),
            true,
            "Fixes spots where you can get stuck, such as the stairs from the dock up to the stone pier on the far "
            + "right of the Port Area. Each fix does nothing once the game fixes that spot.");

        BuiltEarlyQuests = config.Bind(
            _softLocks,
            nameof(BuiltEarlyQuests),
            true,
            "Finishes Agatha's choir step and the woodcarver's organ step when you built the choir or organ "
            + "before being asked. The game only checks the moment you build it, and it can be built once, so "
            + "building it early locks the quest line for good. Checked when a save loads and when the step starts.");

        DismantleUnreachable = config.Bind(
            _softLocks,
            nameof(DismantleUnreachable),
            true,
            "A station up against a fence or wall might be blocked from dismantling. This will allow you to still "
            + "work on it.");

        SoftLockedBoards = config.Bind(
            _softLocks,
            nameof(SoftLockedBoards),
            true,
            "If you are soft locked and run out of boards without a sawhorse or circular saw, Larry "
            + "will help you out.");

        UnstuckKey = config.Bind(
            _tweaks,
            nameof(UnstuckKey),
            new KeyboardShortcut(KeyCode.U, KeyCode.LeftControl),
            "Moves you to the nearest open ground when you are boxed in, say between chests or inside a "
            + "collider. Only works while you can move, so not with a window open or during dialog or a "
            + "cutscene. Clear it to turn the key off.");

        UnstuckRange = config.Bind(
            _tweaks,
            nameof(UnstuckRange),
            30f,
            new ConfigDescription(
                "How far Unstuck may move you, in world units.",
                new AcceptableValueRange<float>(1f, 100f)));

        BedSaveWhenRested = config.Bind(
            _tweaks,
            nameof(BedSaveWhenRested),
            false,
            "Saves the game when you use the bed with full energy and the Keeper refuses to sleep. "
            + "The game only saves when you actually sleep.");

        TechPointCap = config.Bind(
            _tweaks,
            nameof(TechPointCap),
            999,
            new ConfigDescription(
                "The most red, green, or blue tech points you can hold. The game caps each at 999 and throws away "
                + "anything past it. Set to 999 to keep the game's cap.",
                new AcceptableValueRange<int>(999, 999999)));

        CarryOver(
            config,
            (StudyTableScienceOnly, "StudyTable", "ScienceOnly"),
            (StudyTableNoStuckCrafts, "StudyTable", "NoStuckCrafts"),
            (GreenThumbTalentBonus, "GreenThumb", "TalentBonus"),
            (CollectStrayTechPoints, "TechPoints", "CollectStray"),
            (OvenNoLostIngredients, "Oven", "NoLostIngredients"),
            (SermonFaithRounding, "Sermons", "FaithRounding"),
            (StuckCarriers, "Carriers", "ShowWhenStuck"),
            (RemoveInWholeArea, "Building", "RemoveInWholeArea"),
            (BuiltEarlyQuests, "Quests", "BuiltEarly"),
            (DismantleUnreachable, "Building", "DismantleUnreachable"),
            (SoftLockedBoards, "Boards", "SoftLockedBoards"),
            (UnstuckKey, "Unstuck", "Key"),
            (UnstuckRange, "Unstuck", "Range"),
            (BedSaveWhenRested, "Bed", "SaveWhenRested"),
            (TechPointCap, "TechPoints", "Cap"));
    }

    private static void CarryOver(ConfigFile config, params (ConfigEntryBase Entry, string Section, string Key)[] moves)
    {
        if (AccessTools.Property(typeof(ConfigFile), "OrphanedEntries")?.GetValue(config) is not Dictionary<ConfigDefinition, string> orphaned)
            return;

        var carried = 0;
        foreach (var (entry, section, key) in moves)
        {
            var old = new ConfigDefinition(section, key);
            if (!orphaned.TryGetValue(old, out var value))
                continue;

            entry.SetSerializedValue(value);
            orphaned.Remove(old);
            carried++;
        }

        if (carried > 0)
            config.Save();
    }
}
