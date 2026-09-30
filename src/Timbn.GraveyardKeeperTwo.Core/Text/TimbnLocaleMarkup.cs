using LazyBearTechnology;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Parses the game's text markup for icons, nested keys and replacement words, the way the game does when it loads a language.</summary>
internal static class TimbnLocaleMarkup
{
    private static readonly Regex _nested = new(@"\#\(.*?\)");
    private static readonly Regex _replacement = new(@"\@\(.*?\)");

    internal static string Parse(string key, string text, out NestedLocalesMetaInfo nested, out ReplacementKeysMetadata? replacement)
    {
        text = text.Replace("(*", "<sprite name=\"").Replace("*)", "\">").Replace('“', '"').Replace('„', '"');
        nested = new NestedLocalesMetaInfo();
        var removed = 0;
        foreach (Match match in _nested.Matches(text))
        {
            nested.AddEntry(match.Index - removed, match.Value.Substring(2, match.Value.Length - 3));
            removed += match.Value.Length;
        }

        text = _nested.Replace(text, "");
        var matches = _replacement.Matches(text);
        replacement = null;
        if (matches.Count == 0)
            return text;

        replacement = new ReplacementKeysMetadata { id = key };
        removed = 0;
        foreach (Match match in matches)
        {
            replacement.keysIndexes.Add(match.Index - removed);
            replacement.keys.Add(match.Value.Substring(2, match.Value.Length - 3));
            removed += match.Value.Length;
        }

        return _replacement.Replace(text, "");
    }
}
