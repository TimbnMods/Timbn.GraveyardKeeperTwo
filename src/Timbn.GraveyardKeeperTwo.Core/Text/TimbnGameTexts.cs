using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// A talk or quest line given one of these keys in place of its text shows the game's own translated line, in the
// speaker's recorded voice when the game has one.
internal static class TimbnGameTexts
{
    private static readonly HashSet<string> _keys =
    [
        .. typeof(TimbnGameIds.Texts).GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => (string)field.GetRawConstantValue()!),
    ];

    private static readonly HashSet<string> _voiced = [];

    internal static bool IsKey(string text) => _keys.Contains(text);

    // The game records its short replies once per speaker, such as larry_default_sure, but only ever asks for the plain
    // key, so a line given a game key switches to the speaker's own clip. That key's text points back at the plain one,
    // so it reads the same in every language.
    internal static string Voiced(string speakerId, string key)
    {
        if (!IsKey(key) || !TimbnGameVoices.Speakers.TryGetValue(speakerId, out var voice))
            return key;

        var clip = $"{voice}_{key}";
        if (!TimbnGameVoices.Clips.Contains(clip))
            return key;

        if (_voiced.Add(clip))
            TimbnLocale.Add(clip, $"#({key})");

        return clip;
    }
}
