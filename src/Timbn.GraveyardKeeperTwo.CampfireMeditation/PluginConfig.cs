namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

internal static class PluginConfig
{
    public static ConfigEntry<float> TimeSpeed { get; private set; } = null!;
    public static ConfigEntry<float> EnergyPerDay { get; private set; } = null!;
    public static ConfigEntry<float> InsanityPerDay { get; private set; } = null!;
    public static ConfigEntry<float> MaxInsanity { get; private set; } = null!;
    public static ConfigEntry<float> Radius { get; private set; } = null!;
    public static ConfigEntry<bool> HomeOnly { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        TimeSpeed = config.Bind(
            "Meditation",
            "TimeSpeed",
            7f,
            "How many times faster time runs while you meditate. At 7 a day takes about 43 seconds, sleep runs at 50.",
            1f,
            50f);

        EnergyPerDay = config.Bind(
            "Meditation",
            "EnergyPerDay",
            100f,
            "Energy regained per in game day of meditating. Sleep gives 400.",
            0f,
            400f);

        InsanityPerDay = config.Bind(
            "Meditation",
            "InsanityPerDay",
            40f,
            "Insanity removed per in game day of meditating, until MaxInsanity is reached.",
            0f,
            400f);

        MaxInsanity = config.Bind(
            "Meditation",
            "MaxInsanity",
            20f,
            "The most insanity one meditation can remove. Sleep removes 20.",
            0f,
            100f);

        Radius = config.Bind(
            "Meditation",
            "Radius",
            2f,
            "How close to a campfire you must stand to meditate.");

        HomeOnly = config.Bind(
            "Meditation",
            "HomeOnly",
            true,
            "Only the campfire at your home can be used to meditate. Turn it off to meditate at any campfire.");
    }
}
