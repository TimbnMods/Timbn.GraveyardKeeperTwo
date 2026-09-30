namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

internal static class PluginConfig
{
    private const string _bugFixes = "Bug Fixes";
    private const string _softLocks = "Soft Locks";
    private const string _tweaks = "Tweaks";

    public static ConfigEntry<bool> PerkTalentBonus { get; private set; } = null!;

    public static ConfigEntry<bool> CollectStrayTechPoints { get; private set; } = null!;

    public static ConfigEntry<bool> SermonFaithRounding { get; private set; } = null!;

    public static ConfigEntry<bool> StuckCarriers { get; private set; } = null!;

    public static ConfigEntry<bool> StuckResourceWorkers { get; private set; } = null!;

    public static ConfigEntry<bool> RemoveInWholeArea { get; private set; } = null!;

    public static ConfigEntry<bool> ConveyorLoopCrash { get; private set; } = null!;

    public static ConfigEntry<bool> HighlightConveyorLoops { get; private set; } = null!;

    public static ConfigEntry<bool> BuiltEarlyQuests { get; private set; } = null!;

    public static ConfigEntry<bool> DismantleUnreachable { get; private set; } = null!;

    public static ConfigEntry<bool> SoftLockedBoards { get; private set; } = null!;

    public static ConfigEntry<bool> LostQuestItems { get; private set; } = null!;

    public static ConfigEntry<bool> RecoverBattleRewards { get; private set; } = null!;

    public static ConfigEntry<bool> BaitInBags { get; private set; } = null!;

    public static ConfigEntry<bool> MoveIdenticalFromBags { get; private set; } = null!;

    public static ConfigEntry<bool> SortIntoBags { get; private set; } = null!;

    public static ConfigEntry<KeyboardShortcut> UnstuckKey { get; private set; } = null!;

    public static ConfigEntry<float> UnstuckRange { get; private set; } = null!;

    public static ConfigEntry<bool> BedSaveWhenRested { get; private set; } = null!;

    public static ConfigEntry<int> TechPointCap { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        PerkTalentBonus = config.Bind(
            _bugFixes,
            nameof(PerkTalentBonus),
            true,
            "Makes the Green Thumb, Master Brewer and Sommelier perks grant the green talent their descriptions "
            + "promise (+2 when planting, +2 when brewing beer and mead, +3 when making wine). The game gives them "
            + "a head start on the craft instead, which those crafts throw away, so the perks do nothing at all. "
            + "Leaves a perk alone once the game fixes it.");

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

        RecoverBattleRewards = config.Bind(
            _bugFixes,
            nameof(RecoverBattleRewards),
            true,
            "Going to sleep brings every item battles give as a reward (Defender's Emblem, Victory Banner, Keys "
            + "for Looters, Town Gratitude, and Zombie Goo) lying anywhere in the world back to your feet. Battle "
            + "rewards are dropped at your feet after a win, and with full bags walking into one knocks it away "
            + "until it lands somewhere you can't reach.");

        BaitInBags = config.Bind(
            _bugFixes,
            nameof(BaitInBags),
            true,
            "Bait kept in a bag counts when you start fishing. The game only looked for bait loose in your "
            + "inventory, so with all of it in a fishing bag you were told you had none.");

        ConveyorLoopCrash = config.Bind(
            _bugFixes,
            nameof(ConveyorLoopCrash),
            true,
            "Stops the game from closing on its own a few seconds after a save loads when a loop of conveyors "
            + "feeds another loop and the first one runs empty or jams, for example a splitter that sends part of "
            + "its coal back into the chest before it. Your conveyors keep moving items as before.");

        HighlightConveyorLoops = config.Bind(
            _bugFixes,
            nameof(HighlightConveyorLoops),
            true,
            "When a conveyor loop like that is found, traces it with arrows and draws a red square on the belt to remove to break it. "
            + "A notice shows either way. Needs ConveyorLoopCrash.");

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

        LostQuestItems = config.Bind(
            _softLocks,
            nameof(LostQuestItems),
            true,
            "If you lose an item the story still needs, Larry will have found it.");

        MoveIdenticalFromBags = config.Bind(
            _tweaks,
            nameof(MoveIdenticalFromBags),
            true,
            "The chest window's move all identical items button also takes matching items out of your bags, "
            + "not just your main inventory.");

        SortIntoBags = config.Bind(
            _tweaks,
            nameof(SortIntoBags),
            true,
            "Items you pick up, craft, buy, or take from a chest go straight into the bag made for them, such as "
            + "fish and bait into a fishing bag or seeds into a farming bag, while it has room. Universal bags work "
            + "as before and only take what they already hold or what no longer fits in your inventory.");

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
            "How far Unstuck may move you, in world units.",
            1f,
            100f);

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
            "The most red, green, or blue tech points you can hold. The game caps each at 999 and throws away "
            + "anything past it. Set to 999 to keep the game's cap.",
            999,
            999999);

        TimbnConfig.CarryOver(
            config,
            (PerkTalentBonus, _bugFixes, "GreenThumbTalentBonus"),
            (PerkTalentBonus, "GreenThumb", "TalentBonus"),
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
}
