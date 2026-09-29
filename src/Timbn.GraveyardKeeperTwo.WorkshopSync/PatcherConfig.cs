namespace Timbn.GraveyardKeeperTwo.WorkshopSync;

internal static class PatcherConfig
{
    public static ConfigEntry<bool> Enabled { get; private set; } = null!;
    public static ConfigEntry<string> SkippedItems { get; private set; } = null!;
    public static ConfigEntry<string> SteamFolder { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Installs the BepInEx mods you subscribe to on the Steam Workshop every time the game starts, and removes them when you unsubscribe.");

        SteamFolder = config.Bind(
            "General",
            "SteamFolder",
            "",
            @"The Steam folder your Workshop downloads are in, the one that holds steamapps, such as C:\Program Files (x86)\Steam. Leave it empty to use the Steam library Graveyard Keeper 2 is installed in.");

        SkippedItems = config.Bind(
            "General",
            "SkippedItems",
            "",
            "Workshop items to leave out, as ids separated by commas. The id is the number at the end of the item's Steam page address. A skipped item is uninstalled, apart from any config files it added.");
    }

    public static HashSet<string> SkippedItemIds() =>
        [.. SkippedItems.Value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries).Select(id => id.Trim())];

    public static string SteamFolderPath() => SteamFolder.Value.Trim().Trim('"');
}
