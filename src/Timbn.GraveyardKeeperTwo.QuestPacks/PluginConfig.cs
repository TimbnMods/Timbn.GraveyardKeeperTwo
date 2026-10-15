namespace Timbn.GraveyardKeeperTwo.QuestPacks;

internal static class PluginConfig
{
    public static ConfigEntry<bool> ShowProblemsOnMainMenu { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        ShowProblemsOnMainMenu = config.Bind(
            "General",
            "ShowProblemsOnMainMenu",
            false,
            "Shows a popup on the main menu listing mistakes found in quest pack files. They are always written to the log.");
    }
}
