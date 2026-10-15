using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Turns the game's text markup for icons, nested keys and button glyphs into what the game shows. It makes
/// the same character swaps as LLBase.AddLangString and calls the two private parsers that method uses when it
/// loads a language pack.
/// </summary>
internal static class TimbnLocaleMarkup
{
    private static readonly NestedParser? _parseNested = TimbnReflection.TryDelegate<NestedParser>(
        () => AccessTools.Method(typeof(LLBase), "GetNestedLocalesMetaInfo"),
        "LLBase.GetNestedLocalesMetaInfo");

    private static readonly ReplacementParser? _parseReplacement = TimbnReflection.TryDelegate<ReplacementParser>(
        () => AccessTools.Method(typeof(LLBase), "FormReplacementMetadataFor"),
        "LLBase.FormReplacementMetadataFor");

    private static bool _warned;

    private delegate NestedLocalesMetaInfo NestedParser(LLBase lang, ref string text);

    private delegate ReplacementKeysMetadata? ReplacementParser(LLBase lang, string key, ref string text);

    internal static string Parse(LLBase lang, string key, string text, out NestedLocalesMetaInfo nested, out ReplacementKeysMetadata? replacement)
    {
        text = text.Replace("(*", "<sprite name=\"").Replace("*)", "\">").Replace('“', '"').Replace('„', '"').Replace('・', '⊙');
        if (_parseNested is null || _parseReplacement is null)
        {
            if (!_warned)
            {
                _warned = true;
                TimbnCorePlugin.Logger.LogError($"{nameof(TimbnLocaleMarkup)}|The game's text parsers were not found, so #(key) and @(GameKeyInteraction) markup in plugin text shows as written.");
            }

            nested = new NestedLocalesMetaInfo();
            replacement = null;
            return text;
        }

        nested = _parseNested(lang, ref text);
        replacement = _parseReplacement(lang, key, ref text);
        return text;
    }
}
