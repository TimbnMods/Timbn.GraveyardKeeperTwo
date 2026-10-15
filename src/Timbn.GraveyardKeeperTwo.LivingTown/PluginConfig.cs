namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal static class PluginConfig
{
    public static ConfigEntry<bool> DailyRoutine { get; private set; } = null!;
    public static ConfigEntry<bool> OpeningHours { get; private set; } = null!;
    public static ConfigEntry<bool> ChurchVisits { get; private set; } = null!;
    public static ConfigEntry<bool> WriteDebugLog { get; private set; } = null!;
    public static ConfigEntry<bool> Funerals { get; private set; } = null!;
    public static ConfigEntry<int> GratitudePerVisitor { get; private set; } = null!;
    public static ConfigEntry<int> MinimumChurchVisitors { get; private set; } = null!;
    public static ConfigEntry<int> MaximumChurchVisitors { get; private set; } = null!;
    public static ConfigEntry<bool> VillagerRequests { get; private set; } = null!;

    public static ConfigEntry<bool> VillagerDeliveries { get; private set; } = null!;

    public static ConfigEntry<bool> ShowGraveyardPaths { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        DailyRoutine = config.Bind(
            "People",
            "DailyRoutine",
            true,
            "Herm, Linda, Larry and the astrologer go about their day. Turning it off puts them back in their usual spots.");

        OpeningHours = config.Bind(
            "People",
            "OpeningHours",
            true,
            "Herm only trades from 6:00 until 16:48.");

        ChurchVisits = config.Bind(
            "Crowds",
            "ChurchVisits",
            true,
            "Villagers and townsfolk come to visit the graveyard each day between 6:00 and 13:00 and stay 4 to 6 hours (until 16:00 at the latest), more of them the more grateful the town is.");

        Funerals = config.Bind(
            "Crowds",
            "Funerals",
            true,
            "The day after you fill in a new grave, the villagers who visit hold a funeral for it. They gather in the church, three of them speak at the choir in turn, then everyone lines up at the grave to pay their respects. At least 10 villagers come that day whatever the weather and the visitor settings say, even with ChurchVisits off, though never more than the church has pew seats and speakers to hold. They all arrive by 9:00, mostly alone or in pairs and sometimes in groups of up to five, one group close behind the other. It never happens on the sermon day.");

        GratitudePerVisitor = config.BindRange(
            "Crowds",
            "GratitudePerVisitor",
            5,
            "How much town gratitude brings one more graveyard visitor.",
            1,
            50);

        MinimumChurchVisitors = config.BindRange(
            "Crowds",
            "MinimumChurchVisitors",
            2,
            "The minimum number of villagers that visit the graveyard in a day, however little the town thanks you.",
            0,
            30);

        MaximumChurchVisitors = config.BindRange(
            "Crowds",
            "MaximumChurchVisitors",
            15,
            "The maximum number of villagers that visit the graveyard in a day, however grateful the town is.",
            0,
            30);

        VillagerRequests = config.Bind(
            "Requests",
            "VillagerRequests",
            true,
            "About once a week someone in the village or town asks you to cook a dish you know the recipe for, and pays its sale price when you bring it.");

        VillagerDeliveries = config.Bind(
            "Requests",
            "VillagerDeliveries",
            true,
            "About once a week someone gives you something to take between the village and town, and the friend pays its sale price. It runs out after a week.");

        ShowGraveyardPaths = config.Bind(
            "Debug",
            "ShowGraveyardPaths",
            false,
            "Draws the graveyard paths visitors walk along in orange, each visitor's last walk in blue, the walk they are on right now in red, where they stand in green and the bench seats in pink.");

        WriteDebugLog = config.BindHidden(
            "Debug",
            "WriteDebugLog",
            false,
            "Writes every church visit, walk and schedule decision to the log at the debug level.");
    }
}
