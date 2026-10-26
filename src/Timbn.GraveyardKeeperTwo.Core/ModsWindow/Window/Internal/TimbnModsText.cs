namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnModsText
{
    internal const string Enabled = "Enabled";
    internal const string ByAuthor = "by {0}";
    internal const string ModCount = "{0} mods";
    internal const string BackToGroup = "Back to Group";
    internal const string Overview = "Overview";
    internal const string NoInfoTimbn = "This mod uses Timbn Core and has not given any extra information about itself.";
    internal const string NoInfoOther = "This mod is not managed by Timbn Core, so there is no extra information about it.";
    internal const string Defaults = "Default All";
    internal const string ResetSetting = "Reset Setting";
    internal const string HoldToResetAll = "Hold to Reset All";
    internal const string ResetTooltipToken = "timbn_ui_reset";
    internal const string ResetTooltip = "Put this setting back to its default";
    internal static string Settings => Translated(TimbnGameIds.Texts.UiSettingsHeader, "Settings");
    internal const string GameVersion = "Game Version:";
    internal const string ModsLoaded = "{0} mods loaded";
    internal const string Advanced = "Advanced";
    internal const string ReadOnly = "Read Only";

    // A mod's lang file can carry the Mods window's descriptions in other languages, the About card's under
    // "<guid>.description" and a setting's under "<guid>.<Section>.<Key>", and the English text is the fallback. The .cfg
    // file itself always keeps the English.
    internal static string Translated(string key, string english) =>
        TimbnCorePlugin.Instance is { } core && core.Text.Has(key) ? core.Text.Get(key) : english;

    internal static string AboutKey(string guid) => $"{guid}.description";

    internal static string SettingKey(string guid, ConfigDefinition definition) => $"{guid}.{definition.Section}.{definition.Key}";
    internal const string ShowAdvanced = "Show advanced settings";
    internal const string ShowAdvancedLine = "Settings most players never need to change. The choice is kept for every mod.";
    internal const string Search = "Search settings";
    internal const string NoMatches = "No settings match \"{0}\".";
    internal const string SaveTitle = "Save changes";
    internal const string SaveMessage = "Save the changes to {0}?";
    internal const string NothingToSave = "{0} has no changes to save.";
    internal const string DefaultsTitle = "Defaults";
    internal const string DefaultsMessage = "Put every setting of {0} back to its default? The changes still wait for Save.";
    internal const string RestartTitle = "Restart needed";
    internal const string RestartMessage = "Restart the game to finish turning {0} {1}.";
    internal const string On = "on";
    internal const string Off = "off";
    internal const string NoSettings = "This mod has no settings.";
    internal static string Save => Translated(TimbnGameIds.Texts.UiSave, "Save");
    internal const string Discard = "Discard";
    internal const string UnsavedTitle = "Unsaved changes";
    internal const string UnsavedMessage = "{0} has changes that are not saved.";
    internal static string SettingCount(int count) => count == 1 ? "1 Setting" : $"{count} Settings";

    internal const string BrokenLine = "Off since a game update changed what it relies on. It comes back once the mod is updated.";
}
