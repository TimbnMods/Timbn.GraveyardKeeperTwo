namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The defaults behind a craft added through the plugin's Crafts, kept free of game statics so they can be tested.</summary>
internal static class TimbnCraftRules
{
    internal const string DefaultTemplate = TimbnGameIds.Items.WoodenPlank;

    /// <summary>
    /// Whether the craft runs on its own, which has to agree with the station. A timed station like the kitchen oven
    /// runs its crafts itself, a bench worked by hand does not, so the station decides when it is known and the template
    /// craft's own setting stays otherwise.
    /// </summary>
    internal static bool IsAuto(bool? stationRunsItself, bool templateIsAuto) => stationRunsItself ?? templateIsAuto;

    /// <summary>The text keys a craft shows under, its id for the name and id_d for the description, with empty texts left to the lang files.</summary>
    internal static Dictionary<string, string> Texts(string id, string name, string description) =>
        TimbnStationRules.Texts((id, name), (TimbnStationRules.DescriptionKey(id), description));
}
