namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

internal static class PluginConfig
{
    private const string _bugFixes = "Bug Fixes";
    private const string _softLocks = "Soft Locks";
    private const string _tweaks = "Tweaks";

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

    public static ConfigEntry<bool> MoveIdenticalFromBags { get; private set; } = null!;

    public static ConfigEntry<bool> SortIntoBags { get; private set; } = null!;

    public static ConfigEntry<KeyboardShortcut> UnstuckKey { get; private set; } = null!;

    public static ConfigEntry<float> UnstuckRange { get; private set; } = null!;

    public static ConfigEntry<bool> BedSaveWhenRested { get; private set; } = null!;

    public static ConfigEntry<int> TechPointCap { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        CollectStrayTechPoints = config.Bind(
            _bugFixes,
            nameof(CollectStrayTechPoints),
            true,
            "Pulls a tech point orb to you when it settles off the walkable ground and out of reach of the "
            + "game's magnet. The game only gathers those when you sleep.");

        SermonFaithRounding = config.Bind(
            _bugFixes,
            nameof(SermonFaithRounding),
            true,
            "Rounds ceremony faith the way the numbers say. Priest makes Basic Prayer pay 0.35 faith per "
            + "parishioner, but the game stores 0.35 as a number just under it, so at 10, 30, 50 parishioners "
            + "and so on a total of 3.5 can land just under 3.5 and round down, and Priest adds nothing.");

        StuckCarriers = config.Bind(
            _bugFixes,
            nameof(StuckCarriers),
            true,
            "When a zombie supplier has nowhere to put its item, it shows the no storage icon the game already "
            + "uses for gardeners and walks back to its Zombie Supplier Station instead of standing where it is. "
            + "It goes on with its work as soon as there is room, as it would anyway.");

        StuckResourceWorkers = config.Bind(
            _bugFixes,
            nameof(StuckResourceWorkers),
            true,
            "Keeps lumberjack, miner, clay digger and sand digger zombies working. Each of them walks out to one "
            + "of a few work spots, and dismantling a stand while its zombie was out left that spot taken for good. "
            + "Once every spot was taken, a zombie stood in place swinging its tool forever. Spots "
            + "are now freed when a zombie leaves its stand, spots already lost in a save are freed when it loads, "
            + "and a stranded zombie goes back to work as soon as a spot is free.");

        RemoveInWholeArea = config.Bind(
            _bugFixes,
            nameof(RemoveInWholeArea),
            true,
            "Lets you remove a building that stands inside your build area when the game refuses because it counts "
            + "the building as part of a different area than the one you are building in.");

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
            "If you have fewer than two boards and no sawhorse or circular saw, Larry will offer you two when "
            + "you talk to him.");

        LostQuestItems = config.Bind(
            _softLocks,
            nameof(LostQuestItems),
            true,
            "If you lose the Inquisitor's medallion before opening the tower prison bars, Larry will have found "
            + "it.");

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

        UnstuckRange = config.BindRange(
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

        TechPointCap = config.BindRange(
            _tweaks,
            nameof(TechPointCap),
            999,
            "The most red, green, or blue tech points you can hold. The game caps each at 999 and throws away "
            + "anything past it. Set to 999 to keep the game's cap.",
            999,
            999999);

        TimbnConfig.CarryOver(
            config,
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
