namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Writes the game's text markup for you, for icons and other lines inside a line of text. Use it in any text a plugin
/// adds, such as quest lines or Text.Add. Lang files write the same markup by hand.
/// </summary>
/// <example>
/// <code><![CDATA[
/// $"Come back on {TimbnMarkup.Weekday(TimbnWeekday.Pride)} and bring 2 {TimbnMarkup.Icon("slv")}."
/// $"Ask {TimbnMarkup.Localized(TimbnGameIds.Npcs.Herm)} about the gold wine."
/// ]]></code>
/// </example>
public static class TimbnMarkup
{
    /// <summary>
    /// An icon from the game's text font, such as slv, gld or brz for the coins, or icon-quality-gold for the gold
    /// quality icon. The names are the sprites in the game's font sprite asset.
    /// </summary>
    /// <param name="name">The icon's name.</param>
    /// <returns>The markup, such as (*slv*).</returns>
    public static string Icon(string name) => $"(*{name}*)";

    /// <summary>The icon for a day of the week, the way the game writes days in its own text.</summary>
    /// <param name="day">The day.</param>
    /// <returns>The markup, such as (*day_pride*).</returns>
    public static string Weekday(TimbnWeekday day) => Icon($"day_{day.ToString().ToLowerInvariant()}");

    /// <summary>
    /// Another line's text, looked up each time the line shows, so it follows the player's language. Use it for the
    /// game's names, such as npc_herm.
    /// </summary>
    /// <param name="key">The text's localization key.</param>
    /// <returns>The markup, such as #(npc_herm).</returns>
    public static string Localized(string key) => $"#({key})";
}
