namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

internal static class PluginConfig
{
    public static ConfigEntry<float> TimeSpeed { get; private set; } = null!;
    public static ConfigEntry<float> EnergyPerDay { get; private set; } = null!;
    public static ConfigEntry<float> Radius { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        TimeSpeed = config.Bind(
            "Meditation",
            "TimeSpeed",
            7f,
            new ConfigDescription(
                "How many times faster time runs while you meditate. At 7 a day takes about 43 seconds, sleep runs at 50.",
                new AcceptableValueRange<float>(1f, 50f)));

        EnergyPerDay = config.Bind(
            "Meditation",
            "EnergyPerDay",
            100f,
            new ConfigDescription(
                "Energy regained per in game day of meditating. Sleep gives 400.",
                new AcceptableValueRange<float>(0f, 400f)));

        Radius = config.Bind(
            "Meditation",
            "Radius",
            2f,
            "How close to a campfire you must stand to meditate.");
    }
}
