using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.ChestLabels;

internal static class PluginConfig
{
    public static ConfigEntry<bool> LabelBelow { get; private set; } = null!;
    // The font size of the hint text the name is drawn in, read from the game's UIQualityTooltipHintWidget.
    private const float _gameTextSize = 16f;
    private const float _smallestSize = 8f;
    private const float _largestSize = 64f;

    public static ConfigEntry<string> DefaultSize { get; private set; } = null!;

    // A value that reads as neither a percent nor a number is the game's own size.
    public static float DefaultFontSize
    {
        get
        {
            var text = DefaultSize.Value.Trim();
            var percent = text.EndsWith("%");
            if (!float.TryParse(percent ? text.TrimEnd('%') : text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0f)
                return _gameTextSize;

            return Mathf.Clamp(percent ? _gameTextSize * value / 100f : value, _smallestSize, _largestSize);
        }
    }

    public static float FontSize(int sizeModifier) => Mathf.Clamp(DefaultFontSize + sizeModifier, _smallestSize, _largestSize);
    public static ConfigEntry<string> Colors { get; private set; } = null!;

    public static string[] ColorList =>
        Colors.Value.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(color => color.Trim()).ToArray();

    public static void Bind(ConfigFile config)
    {
        LabelBelow = config.Bind(
            "Labels",
            "LabelBelow",
            true,
            "Show a chest's name under the Open and Label hints. Turn off to show it above them.");

        DefaultSize = config.Bind(
            "Labels",
            "DefaultSize",
            "100%",
            "How big a chest's name is at the Default size, as a percent of the game's text such as 150%, or a font size such as 24. The game's text is 16. Small, Large and Extra Large go from there.");

        Colors = config.Bind(
            "Labels",
            "Colors",
            "",
            "Colors to pick from when naming a chest, before the default ones. Separate them with commas, such as #FF8800, orange.");
    }
}
