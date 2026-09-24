namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

internal static class PluginConfig
{
    public static ConfigEntry<bool> StudyTableScienceOnly { get; private set; } = null!;

    public static ConfigEntry<bool> StudyTableNoStuckCrafts { get; private set; } = null!;

    public static ConfigEntry<bool> GreenThumbTalentBonus { get; private set; } = null!;

    public static ConfigEntry<int> TechPointCap { get; private set; } = null!;

    public static ConfigEntry<bool> CrematoriumNoStuckBodies { get; private set; } = null!;

    public static ConfigEntry<bool> OvenNoLostIngredients { get; private set; } = null!;

    public static ConfigEntry<bool> SermonFaithRounding { get; private set; } = null!;

    public static ConfigEntry<bool> BedSaveWhenRested { get; private set; } = null!;

    public static ConfigEntry<bool> CollectStrayTechPoints { get; private set; } = null!;

    public static ConfigEntry<bool> StuckCarriers { get; private set; } = null!;

    public static ConfigEntry<bool> BuiltEarlyQuests { get; private set; } = null!;

    public static ConfigEntry<KeyboardShortcut> UnstuckKey { get; private set; } = null!;

    public static ConfigEntry<float> UnstuckRange { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        BuiltEarlyQuests = config.Bind(
            "Quests",
            "BuiltEarly",
            true,
            "Finishes Agatha's choir step and the woodcarver's organ step when you built the choir or organ "
            + "before being asked. The game only checks the moment you build it, and it can be built once, so "
            + "building it early locks the quest line for good. Checked when a save loads and when the step starts.");

        StuckCarriers = config.Bind(
            "Carriers",
            "ShowWhenStuck",
            true,
            "When a carrier zombie has nothing in its room to put its item into, it shows the no storage icon "
            + "the game already uses for gardeners and walks back to its supplier station instead of freezing where it "
            + "stands. It goes on with its work as soon as a slot frees up, as it would anyway.");

        UnstuckKey = config.Bind(
            "Unstuck",
            "Key",
            new KeyboardShortcut(KeyCode.U, KeyCode.LeftControl),
            "Moves you to the nearest open ground when you are boxed in, say between chests or inside a "
            + "collider. Only works while you can move, so not with a window open or during dialog or a "
            + "cutscene. Clear it to turn the key off.");

        UnstuckRange = config.Bind(
            "Unstuck",
            "Range",
            30f,
            new ConfigDescription(
                "How far Unstuck may move you, in world units.",
                new AcceptableValueRange<float>(1f, 100f)));

        TechPointCap = config.Bind(
            "TechPoints",
            "Cap",
            999,
            new ConfigDescription(
                "The most red, green, or blue tech points you can hold. The game caps each at 999 and throws away "
                + "anything past it. Set to 999 to keep the game's cap.",
                new AcceptableValueRange<int>(999, 999999)));

        StudyTableScienceOnly = config.Bind(
            "StudyTable",
            "ScienceOnly",
            true,
            "Lets the study table hold only science. Without it, a zombie putting items into the tower's storage "
            + "can fill the table's one slot, where the table window never shows them, and every science you make "
            + "afterwards is lost. Anything already stuck in the table is moved to your inventory when a save loads. Turning "
            + "this off takes the filter back off your tables when a save loads.");

        StudyTableNoStuckCrafts = config.Bind(
            "StudyTable",
            "NoStuckCrafts",
            true,
            "Frees a study table the game left stuck partway through a decompose or study, showing Work with "
            + "nothing to work on. Checked when a save loads. The item and science were already spent, so nothing "
            + "is lost.");

        GreenThumbTalentBonus = config.Bind(
            "GreenThumb",
            "TalentBonus",
            true,
            "Makes the Green Thumb perk grant its +2 green talent when planting. The game puts the value in a "
            + "field that planting crafts throw away, so the perk does nothing at all. Does nothing once the game "
            + "fixes it.");

        CrematoriumNoStuckBodies = config.Bind(
            "Crematorium",
            "NoStuckBodies",
            true,
            "The crematorium refuses a body until you have Anatomy 1, the level its burn needs. The game takes "
            + "the body anyway, fails to start the burn, and leaves it stuck for good. A body already stuck when a "
            + "save loads is burned.");

        CollectStrayTechPoints = config.Bind(
            "TechPoints",
            "CollectStray",
            true,
            "Pulls a tech point orb to you when it settles off the walkable ground, say through a wall, where "
            + "the game's magnet can never reach it. The game only does this when you sleep.");

        OvenNoLostIngredients = config.Bind(
            "Oven",
            "NoLostIngredients",
            true,
            "Gives the oven room for every ingredient its recipes need. It has one ingredient slot, so a zombie "
            + "cook's second ingredient (the oil for onion rings, say) has nowhere to go and is destroyed on "
            + "delivery, over and over. Also refuses any delivery a station has no room for, instead of "
            + "destroying it.");

        SermonFaithRounding = config.Bind(
            "Sermons",
            "FaithRounding",
            true,
            "Rounds ceremony faith the way the numbers say. Priest makes Basic Prayer pay 0.35 faith per "
            + "parishioner, but 0.35 is stored as 0.3499999, so at 10, 30, 50 parishioners and so on 3.5 comes "
            + "out as 3.4999999 and rounds down, and Priest adds nothing.");

        BedSaveWhenRested = config.Bind(
            "Bed",
            "SaveWhenRested",
            false,
            "Saves the game when you use the bed with full energy and the Keeper refuses to sleep. "
            + "The game only saves when you actually sleep.");
    }
}
