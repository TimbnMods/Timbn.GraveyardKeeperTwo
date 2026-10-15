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

    public static ConfigEntry<bool> OfferMarkers { get; private set; } = null!;

    public static ConfigEntry<bool> HandInMarkers { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        DailyRoutine = config.Bind(
            "People",
            "DailyRoutine",
            true,
            "Enable to have Herm, Linda, Larry and the astrologer go about their day. Turning it off puts them back in their usual spots.");

        OpeningHours = config.Bind(
            "People",
            "OpeningHours",
            true,
            "Enable the vendors to be closed to trading outside of opening hours.");

        ChurchVisits = config.Bind(
            "Crowds",
            "ChurchVisits",
            true,
            "Enable to have villagers and townsfolk visit the graveyard each day with crowd sizes scaling up with town gratitude.");

        Funerals = config.Bind(
            "Crowds",
            "Funerals",
            true,
            "Enable villagers to hold funerals for bodies you recently buried. They gather in the church, three of them speak at the choir in turn, then everyone lines up at the grave to pay their respects. Funerals are postponed on sermon day.");

        GratitudePerVisitor = config.BindRange(
            "Crowds",
            "GratitudePerVisitor",
            7,
            "Ratio of town gratitude to additional graveyard visitors. For example, every 8 gratitude points result in one extra visitor.",
            1,
            50);

        MinimumChurchVisitors = config.BindRange(
            "Crowds",
            "MinimumChurchVisitors",
            2,
            "The minimum number of villagers that visit the graveyard in a day, regardless of town gratitude.",
            0,
            30);

        MaximumChurchVisitors = config.BindRange(
            "Crowds",
            "MaximumChurchVisitors",
            30,
            "The maximum number of villagers that visit the graveyard in a day, regardless of town gratitude.",
            0,
            50);

        VillagerRequests = config.Bind(
            "Requests",
            "VillagerRequests",
            true,
            "Enable to have villagers in the village or town ask you to cook a dish you know the recipe for, and pay its sale price when you bring it.");

        VillagerDeliveries = config.Bind(
            "Requests",
            "VillagerDeliveries",
            true,
            "Enable to have villagers in the village or town give you something to take between the village and town, and the friend pays its sale price. Deliveries expire after one week.");

        OfferMarkers = config.Bind(
            "Requests",
            "OfferMarkers",
            true,
            "Shows a marker on the villager who wants to ask you for a dish or an errand, pointing the way while they're off screen.");

        HandInMarkers = config.Bind(
            "Requests",
            "HandInMarkers",
            true,
            "Shows a marker on the villager waiting for your dish or parcel once you have it, pointing the way while they're off screen.");

        WriteDebugLog = config.BindHidden(
            "Debug",
            "WriteDebugLog",
            false,
            "Writes every church visit, walk and schedule decision to the log at the debug level.");
    }
}
