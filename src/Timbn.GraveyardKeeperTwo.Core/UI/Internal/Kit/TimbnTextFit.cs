using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A fix for how Text Mesh Pro sets the game's pixel fonts. It gives each glyph its own space on the left, so text in
/// boxes that line up still does not. The fix measures the text as it was set and moves it, so it knows no font's numbers.
/// </summary>
internal static class TimbnTextFit
{
    // A readable copy of each font's atlas, and how many clear columns each letter keeps before its ink, read once.
    private static readonly Dictionary<TMP_FontAsset, Texture2D> _atlases = [];
    private static readonly Dictionary<(TMP_FontAsset Font, uint Glyph), int> _clearColumns = [];

    /// <summary>
    /// Moves lines of text until the ink of the first letter of each starts on the left edge of its box, then in by the
    /// indent, so text lines up with the art of every other part of a column. Each letter of the game's pixel fonts keeps
    /// its own few clear columns in its box (a T and a 6 or an N start a pixel or two apart), so the letter's pixels are
    /// read from the font. Only the left margin changes.
    /// </summary>
    internal static void AlignLeftEdges(IReadOnlyList<TextMeshProUGUI> lines, float indent = 0f)
    {
        foreach (var line in lines)
        {
            var margin = line.margin;
            line.margin = new Vector4(0f, margin.y, margin.z, margin.w);
            line.ForceMeshUpdate(true);
            // Measured from where the line starts and not from the box, since this can run before the layout gives the box
            // its width.
            var gap = line.textInfo.characterCount > 0 ? InkStart(line.textInfo.characterInfo[0]) : 0f;
            line.margin = new Vector4(indent - gap, margin.y, margin.z, margin.w);
        }
    }

    // How far right of the pen the letter's first pixel of ink is, in the line's own units.
    private static float InkStart(TMP_CharacterInfo letter)
    {
        var box = letter.bottomLeft.x - letter.origin;
        if (letter.elementType != TMP_TextElementType.Character || letter.fontAsset is not { } font || letter.textElement?.glyph is not { } glyph || glyph.glyphRect.width <= 0)
            return box;

        var scale = (letter.bottomRight.x - letter.bottomLeft.x) / glyph.glyphRect.width;
        return box + (ClearColumns(font, glyph) * scale);
    }

    private static int ClearColumns(TMP_FontAsset font, UnityEngine.TextCore.Glyph glyph)
    {
        if (_clearColumns.TryGetValue((font, glyph.index), out var known))
            return known;

        var atlas = Atlas(font);
        var rect = glyph.glyphRect;
        var clear = 0;
        if (atlas != null)
        {
            while (clear < rect.width && !Enumerable.Range(rect.y, rect.height).Any(y => atlas.GetPixel(rect.x + clear, y).a > 0.5f))
                clear++;

            if (clear == rect.width)
                clear = 0;
        }

        _clearColumns[(font, glyph.index)] = clear;
        return clear;
    }

    // The game ships its atlases unreadable, so a copy is drawn through a render texture once per font.
    private static Texture2D? Atlas(TMP_FontAsset font)
    {
        if (_atlases.TryGetValue(font, out var copy))
            return copy;

        var source = font.atlasTexture;
        if (source == null)
            return null;

        var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        var active = RenderTexture.active;
        Graphics.Blit(source, target);
        RenderTexture.active = target;
        copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(target);
        _atlases[font] = copy;
        return copy;
    }
}
