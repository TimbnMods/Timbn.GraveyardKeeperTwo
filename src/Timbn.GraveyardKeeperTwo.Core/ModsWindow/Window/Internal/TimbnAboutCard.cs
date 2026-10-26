using Steamworks;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// What the Mods window says about a mod from what the mod told it, its description line and how its links open.
/// A mod that told nothing gets a line saying why, so the card is never empty.
/// </summary>
internal static class TimbnAboutCard
{
    /// <summary>
    /// The mod's description. A mod with links and no description has none, and one with neither gets a line that says it
    /// gave no information, or that Core does not manage it when it is not a Timbn mod.
    /// </summary>
    internal static string? DescriptionFor(TimbnModsMod mod, TimbnModInfo? info, bool hasLinks)
    {
        if (!string.IsNullOrWhiteSpace(info?.Description))
            return TimbnModsText.Translated(TimbnModsText.AboutKey(mod.Guid), info!.Description!);

        if (hasLinks)
            return null;

        return mod.IsTimbn ? TimbnModsText.NoInfoTimbn : TimbnModsText.NoInfoOther;
    }

    internal static void OpenUrl(string label, string url)
    {
        if (SteamManager.Initialized && SteamUtils.IsOverlayEnabled() && label == TimbnModLinks.SteamWorkshop)
            SteamFriends.ActivateGameOverlayToWebPage(url);
        else
            Application.OpenURL(url);
    }
}
