namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSprites
{
    private const float _pixelsPerUnit = 50f;
    private const int _blueprintSize = 28;
    private const int _iconCanvasSize = 48;
    private const int _iconMaxSize = 32;
    private const int _perkSize = 26;
    private const int _perkCanvasSize = 30;
    private const int _buffSize = 26;
    private const float _coloredSaturation = 0.25f;
    private const int _hueBins = 36;
    private const float _hueRange = 25f / 360f;

    private static readonly Dictionary<string, List<Sprite>> _sprites = [];

    // Removed during an F6 reload and kept alive until the reloaded plugins have added theirs.
    private static readonly List<(string Name, Sprite Sprite)> _held = [];
    private static bool _holding;

    // The game draws item icons and blueprints on 48 by 48 canvases and perks on 30 by 30 ones, with the art in the middle,
    // and its windows draw them at that size, so a sprite of any other size is stretched. Each kind has one rule for the
    // size a mod gives and where Core puts it. An image that breaks the rule is not added and the game shows its own default.
    internal static IDisposable Add(string name, byte[] image, TimbnSpriteKind kind) => kind switch
    {
        TimbnSpriteKind.Item => Place(name, image, new Rule("icon", null, _iconMaxSize, _iconCanvasSize)),
        TimbnSpriteKind.Blueprint => Place(name, image, new Rule("blueprint", _blueprintSize, _blueprintSize, _iconCanvasSize)),
        TimbnSpriteKind.Perk => Place(name, image, new Rule("perk icon", _perkSize, _perkSize, _perkCanvasSize)),
        TimbnSpriteKind.Buff => Place(name, image, new Rule("buff icon", _buffSize, _buffSize, null)),
        _ => Place(name, image, new Rule("image", null, int.MaxValue, null)),
    };

    // The released QuestPacks 1.0.0 calls this by name.
    internal static IDisposable AddPng(string name, byte[] image) => Add(name, image, TimbnSpriteKind.Image);

    private static IDisposable Place(string name, byte[] image, Rule rule)
    {
        var art = Read(name, image);
        if (art == null)
            return TimbnUndo.None;

        var wrong = rule.Exact is { } size
            ? art.width != size || art.height != size
            : art.width > rule.Max || art.height > rule.Max;
        if (wrong)
        {
            var needs = rule.Exact is { } required ? $"a {rule.Noun} must be {required}x{required}" : $"an {rule.Noun} can be at most {rule.Max}x{rule.Max}";
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnSprites)}|{name} is {art.width}x{art.height}, and {needs}. It was not added.");
            UnityEngine.Object.Destroy(art);
            return TimbnUndo.None;
        }

        if (rule.Canvas is not { } canvas)
            return Register(name, Sprite.Create(art, new Rect(0, 0, art.width, art.height), new Vector2(0.5f, 0.5f), _pixelsPerUnit));

        var texture = new Texture2D(canvas, canvas, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(new Color32[canvas * canvas]);
        texture.SetPixels32((canvas - art.width) / 2, (canvas - art.height) / 2, art.width, art.height, art.GetPixels32());
        texture.Apply(false, true);
        UnityEngine.Object.Destroy(art);
        return Register(name, Sprite.Create(texture, new Rect(0, 0, canvas, canvas), new Vector2(0.5f, 0.5f), _pixelsPerUnit));
    }

    private readonly struct Rule(string noun, int? exact, int max, int? canvas)
    {
        internal string Noun { get; } = noun;

        internal int? Exact { get; } = exact;

        internal int Max { get; } = max;

        internal int? Canvas { get; } = canvas;
    }

    private static Texture2D? Read(string name, byte[] image)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        if (texture.LoadImage(image))
            return texture;

        UnityEngine.Object.Destroy(texture);
        TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnSprites)}|{name} is not a readable image.");
        return null;
    }

    internal static void AddTinted(string name, Sprite source, Color tint)
    {
        var texture = ReadCopy(name, source);
        var pixels = texture.GetPixels();
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(pixels[i].r * tint.r, pixels[i].g * tint.g, pixels[i].b * tint.b, pixels[i].a);

        AddCopy(name, source, texture, pixels);
    }

    // Tinting muddies colored art like the game's quest marker, so this repaints only the art's main color (the tile
    // behind the marker's magnifying glass) and keeps its shading. Other colors, white, gray and black stay as they are.
    internal static void AddRecolored(string name, Sprite source, Color color)
    {
        var texture = ReadCopy(name, source);
        var pixels = texture.GetPixels();
        Color.RGBToHSV(color, out var hue, out var saturation, out var value);
        var main = MainHue(pixels);
        var brightest = 0f;
        foreach (var pixel in pixels)
        {
            if (IsMainColor(pixel, main, out var v))
                brightest = Mathf.Max(brightest, v);
        }

        for (var i = 0; i < pixels.Length; i++)
        {
            if (brightest == 0f || !IsMainColor(pixels[i], main, out var v))
                continue;

            var recolored = Color.HSVToRGB(hue, saturation, Mathf.Clamp01(value * v / brightest));
            recolored.a = pixels[i].a;
            pixels[i] = recolored;
        }

        AddCopy(name, source, texture, pixels);
    }

    private static float MainHue(Color[] pixels)
    {
        var bins = new int[_hueBins];
        foreach (var pixel in pixels)
        {
            Color.RGBToHSV(pixel, out var h, out var s, out _);
            if (pixel.a > 0f && s >= _coloredSaturation)
                bins[Mathf.Min((int)(h * _hueBins), _hueBins - 1)]++;
        }

        var best = 0;
        for (var i = 1; i < _hueBins; i++)
        {
            if (bins[i] > bins[best])
                best = i;
        }

        return (best + 0.5f) / _hueBins;
    }

    private static bool IsMainColor(Color pixel, float main, out float value)
    {
        Color.RGBToHSV(pixel, out var h, out var s, out value);
        var distance = Mathf.Abs(h - main);
        return pixel.a > 0f && s >= _coloredSaturation && Mathf.Min(distance, 1f - distance) <= _hueRange;
    }

    // Copies the whole sprite rect, padding included, since the game sizes its images to the full rect and a copy of
    // only the drawn pixels gets stretched to fill it.
    private static Texture2D ReadCopy(string name, Sprite source)
    {
        var rect = source.textureRect;
        var x = Mathf.FloorToInt(rect.x);
        var y = Mathf.FloorToInt(rect.y);
        var width = Mathf.Min(Mathf.CeilToInt(rect.xMax), source.texture.width) - x;
        var height = Mathf.Min(Mathf.CeilToInt(rect.yMax), source.texture.height) - y;
        var canvasWidth = Mathf.Max(Mathf.RoundToInt(source.rect.width), width);
        var canvasHeight = Mathf.Max(Mathf.RoundToInt(source.rect.height), height);
        var offsetX = Mathf.Clamp(Mathf.FloorToInt(source.textureRectOffset.x), 0, canvasWidth - width);
        var offsetY = Mathf.Clamp(Mathf.FloorToInt(source.textureRectOffset.y), 0, canvasHeight - height);
        var target = RenderTexture.GetTemporary(source.texture.width, source.texture.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var texture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = source.texture.filterMode,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(new Color32[canvasWidth * canvasHeight]);
        try
        {
            Graphics.Blit(source.texture, target);
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(x, y, width, height), offsetX, offsetY);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }

        return texture;
    }

    private static void AddCopy(string name, Sprite source, Texture2D texture, Color[] pixels)
    {
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Register(name, Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(source.pivot.x / texture.width, source.pivot.y / texture.height), source.pixelsPerUnit));
    }

    private static IDisposable Register(string name, Sprite sprite)
    {
        sprite.name = name;
        if (!_sprites.TryGetValue(name, out var stack))
            _sprites[name] = stack = [];

        stack.Add(sprite);
        return new TimbnUndo(() =>
        {
            if (_sprites.TryGetValue(name, out var current) && current.Remove(sprite) && current.Count == 0)
                _sprites.Remove(name);

            if (_holding)
                _held.Add((name, sprite));
            else
                Destroy(sprite);
        });
    }

    internal static void HoldRemovals() => _holding = true;

    /// <summary>
    /// Destroys every held sprite no reloaded mod added again. One added again stays alive but unlisted, since the
    /// HUD keeps drawing the old object until it redraws and a destroyed texture shows as a white square.
    /// </summary>
    internal static void ReleaseHeldRemovals()
    {
        _holding = false;
        var held = _held.ToList();
        _held.Clear();
        foreach (var (name, sprite) in held)
        {
            if (!_sprites.ContainsKey(name))
                Destroy(sprite);
        }
    }

    internal static bool TryGet(string? name, out Sprite sprite)
    {
        sprite = null!;
        if (name == null || !_sprites.TryGetValue(name, out var stack) || stack.Count == 0)
            return false;

        sprite = stack[^1];
        return sprite != null;
    }

    private static void Destroy(Sprite sprite)
    {
        if (sprite == null)
            return;

        UnityEngine.Object.Destroy(sprite.texture);
        UnityEngine.Object.Destroy(sprite);
    }
}
