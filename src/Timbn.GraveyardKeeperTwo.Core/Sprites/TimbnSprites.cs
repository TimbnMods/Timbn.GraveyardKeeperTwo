namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSprites
{
    private const float _pixelsPerUnit = 50f;
    private const int _canvasSize = 48;

    private static readonly Dictionary<string, List<Sprite>> _sprites = [];

    internal static IDisposable AddPng(string name, byte[] png)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        if (!texture.LoadImage(png))
        {
            UnityEngine.Object.Destroy(texture);
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnSprites)}|{name} is not a readable image.");
            return new TimbnUndo(() => { });
        }

        texture = PadToCanvas(texture);
        return Register(name, Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), _pixelsPerUnit));
    }

    internal static void AddTinted(string name, Sprite source, Color tint)
    {
        var rect = source.textureRect;
        var width = (int)rect.width;
        var height = (int)rect.height;
        var target = RenderTexture.GetTemporary(source.texture.width, source.texture.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = source.texture.filterMode,
            wrapMode = TextureWrapMode.Clamp,
        };
        try
        {
            Graphics.Blit(source.texture, target);
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }

        var pixels = texture.GetPixels();
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(pixels[i].r * tint.r, pixels[i].g * tint.g, pixels[i].b * tint.b, pixels[i].a);

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Register(name, Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(source.pivot.x / width, source.pivot.y / height), source.pixelsPerUnit));
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

            Destroy(sprite);
        });
    }

    internal static bool TryGet(string? name, out Sprite sprite)
    {
        sprite = null!;
        if (name == null || !_sprites.TryGetValue(name, out var stack) || stack.Count == 0)
            return false;

        sprite = stack[stack.Count - 1];
        return sprite != null;
    }

    private static Texture2D PadToCanvas(Texture2D source)
    {
        if (source.width == _canvasSize && source.height == _canvasSize)
            return source;

        if (source.width > _canvasSize || source.height > _canvasSize)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnSprites)}|{source.name} is {source.width}x{source.height}, larger than {_canvasSize}. It will be squashed.");
            return source;
        }

        var canvas = new Texture2D(_canvasSize, _canvasSize, TextureFormat.RGBA32, false)
        {
            name = source.name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        canvas.SetPixels32(new Color32[_canvasSize * _canvasSize]);
        canvas.SetPixels32((_canvasSize - source.width) / 2, (_canvasSize - source.height) / 2, source.width, source.height, source.GetPixels32());
        canvas.Apply(false, true);
        UnityEngine.Object.Destroy(source);
        return canvas;
    }

    private static void Destroy(Sprite sprite)
    {
        if (sprite == null)
            return;

        UnityEngine.Object.Destroy(sprite.texture);
        UnityEngine.Object.Destroy(sprite);
    }
}
