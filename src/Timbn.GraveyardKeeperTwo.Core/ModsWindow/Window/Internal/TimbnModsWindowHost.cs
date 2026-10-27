namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The Mods window's side of the game, its settings in Core's config, the buttons and text it adds when Core starts, its
/// keys and the notice for the layout debug key.
/// </summary>
internal static class TimbnModsWindowHost
{
    private const string _section = "Mods Window";

    internal static ConfigEntry<bool> ShowModSettings { get; private set; } = null!;

    internal static ConfigEntry<int> WidthPercent { get; private set; } = null!;

    internal static ConfigEntry<int> HeightPercent { get; private set; } = null!;

    internal static ConfigEntry<bool> ShowAdvanced { get; private set; } = null!;

    internal static ManualLogSource Log => TimbnCorePlugin.Instance!.PluginLogger;

    internal static void Bind(ConfigFile config)
    {
        ShowModSettings = config.Bind(
            _section,
            nameof(ShowModSettings),
            true,
            "Shows the Mod Settings button on the main menu and in the pause menu, which opens the Mods window. Turn off to hide it.");
        WidthPercent = config.BindRange(_section, nameof(WidthPercent), 85, "How much of the screen's width the Mods window takes. 100 fills it.", 40, 100);
        HeightPercent = config.BindRange(_section, nameof(HeightPercent), 85, "How much of the screen's height the Mods window takes. 100 fills it.", 40, 100);
        // Set from the switch at the bottom of a mod's settings, so it is not listed as a setting of its own.
        ShowAdvanced = config.BindHidden(_section, nameof(ShowAdvanced), false, "Shows every mod's advanced settings in the Mods window.");
    }

    internal static void Start(TimbnCorePlugin plugin)
    {
        plugin.Text.Add(TimbnModSettingsButton.LabelToken, TimbnModSettingsButton.LabelText);
        plugin.Text.Add(TimbnModsScreen.TitleToken, TimbnModsScreen.TitleText);
        plugin.Text.Add(TimbnModsText.ResetTooltipToken, TimbnModsText.ResetTooltip);
        plugin.Settings.While(ShowModSettings, () =>
        {
            TimbnModSettingsButton.AddToOpenMenus();
            return new ButtonsRemoval();
        });
    }

    private sealed class ButtonsRemoval : IDisposable
    {
        public void Dispose() => TimbnModSettingsButton.RemoveAll();
    }
}
