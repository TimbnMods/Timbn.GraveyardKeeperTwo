namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

internal static class PluginConfig
{
    private const string _bugFixes = "Bug Fixes";
    private const string _softLocks = "Soft Locks";
    private const string _tweaks = "Tweaks";

    public static ConfigEntry<bool> SpecialStorageFilters { get; private set; } = null!;

    public static ConfigEntry<bool> GreenThumbTalentBonus { get; private set; } = null!;

    public static ConfigEntry<bool> CollectStrayTechPoints { get; private set; } = null!;

    public static ConfigEntry<bool> SermonFaithRounding { get; private set; } = null!;

    public static ConfigEntry<bool> StuckCarriers { get; private set; } = null!;

    public static ConfigEntry<bool> StuckResourceWorkers { get; private set; } = null!;

    public static ConfigEntry<bool> RemoveInWholeArea { get; private set; } = null!;

    public static ConfigEntry<bool> CollisionFixes { get; private set; } = null!;

    public static ConfigEntry<bool> BuiltEarlyQuests { get; private set; } = null!;

    public static ConfigEntry<bool> DismantleUnreachable { get; private set; } = null!;

    public static ConfigEntry<bool> SoftLockedBoards { get; private set; } = null!;

    public static ConfigEntry<bool> RecoverBattleRewards { get; private set; } = null!;

    public static ConfigEntry<KeyboardShortcut> UnstuckKey { get; private set; } = null!;

    public static ConfigEntry<float> UnstuckRange { get; private set; } = null!;

    public static ConfigEntry<bool> BedSaveWhenRested { get; private set; } = null!;

    public static ConfigEntry<int> TechPointCap { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        SpecialStorageFilters = config.Bind(
            _bugFixes,
            nameof(SpecialStorageFilters),
            true,
            "Stops zombies from putting the wrong item in storage meant for one thing, which they do when the "
            + "rest of the storage nearby is full. Flax in a garden well stops it giving water, and anything "
            + "but science in the study table makes every science afterwards lost. The game only limits the study "
            + "table in saves started on version 1.005 or later and garden wells built on 1.006 or later, so this "
            + "covers older ones and any other storage built before the game limited what it takes. Anything "
            + "already in the wrong place is dropped beside it when a save loads.");

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

        StuckResourceWorkers = config.Bind(
            _bugFixes,
            nameof(StuckResourceWorkers),
            true,
            "Keeps lumberjack, miner, clay and sand zombies working. Each of them walks out to one of a few "
            + "work spots, and dismantling a stand while its zombie was out left that spot taken for good. Once "
            + "every spot was taken, a zombie stood at the stockpile or its stand swinging its tool forever. Spots "
            + "are now freed when a zombie leaves its stand, spots already lost in a save are freed when it loads, "
            + "and a stranded zombie goes back to work as soon as a spot is free.");

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

        RecoverBattleRewards = config.Bind(
            _bugFixes,
            nameof(RecoverBattleRewards),
            true,
            "Going to sleep brings every item battles give as a reward (Defender's Emblem, Victory Banner, Keys "
            + "for Looters, Town Gratitude, and Zombie Goo) lying anywhere in the world back to your feet. Battle "
            + "rewards are dropped at your feet after a win, and with full bags walking into one knocks it away "
            + "until it lands somewhere you can't reach.");

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
            (SpecialStorageFilters, _bugFixes, "StudyTableScienceOnly"),
            (SpecialStorageFilters, "StudyTable", "ScienceOnly"),
            (GreenThumbTalentBonus, "GreenThumb", "TalentBonus"),
            (CollectStrayTechPoints, "TechPoints", "CollectStray"),
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
