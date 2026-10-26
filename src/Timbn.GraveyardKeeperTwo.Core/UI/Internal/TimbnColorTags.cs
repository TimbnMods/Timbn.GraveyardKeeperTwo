namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The color tags the color picker control works with, in the same format as a text's own color tag
/// (<c>&lt;color=#FF0000&gt;</c> or a name like <c>red</c>). Whether a tag is a color is passed in, so the choice logic can be tested outside the game.
/// </summary>
internal static class TimbnColorTags
{
    // Unity's own default colors, written as hex tags.
    public static readonly string[] Defaults =
    [
        Hex(Color.white), Hex(Color.black), Hex(Color.gray), Hex(Color.red), Hex(Color.green),
        Hex(Color.blue), Hex(Color.yellow), Hex(Color.cyan), Hex(Color.magenta),
    ];

    public static bool IsColor(string tag) => ColorUtility.TryParseHtmlString(tag, out _);

    /// <summary>Builds the picker's choices. The first is always no color (null), then the custom colors, then Unity's, then the text's own color when it is none of them.</summary>
    public static List<string?> BuildChoices(IEnumerable<string> custom, string? current, Func<string, bool> isColor)
    {
        var choices = new List<string?> { null };
        foreach (var tag in custom.Concat(Defaults))
        {
            var trimmed = tag.Trim();
            if (isColor(trimmed) && !Contains(choices, trimmed))
                choices.Add(trimmed);
        }

        if (current != null && !Contains(choices, current))
            choices.Insert(1, current);

        return choices;
    }

    public static int IndexOf(List<string?> choices, string? tag) =>
        Math.Max(0, choices.FindIndex(choice => string.Equals(choice, tag, StringComparison.OrdinalIgnoreCase)));

    private static string Hex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

    private static bool Contains(List<string?> choices, string tag) =>
        choices.Contains(tag, StringComparer.OrdinalIgnoreCase);
}
