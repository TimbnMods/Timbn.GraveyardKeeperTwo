namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSprites
{
    private const float _pixelsPerUnit = 50f;
    private const int _canvasSize = 48;

    private static readonly Dictionary<string, Sprite> _sprites = [];

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
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), _pixelsPerUnit);
        sprite.name = name;
        if (_sprites.TryGetValue(name, out var previous))
            Destroy(previous);

        _sprites[name] = sprite;
        return new TimbnUndo(() =>
        {
            if (_sprites.TryGetValue(name, out var current) && current == sprite)
                _sprites.Remove(name);

            Destroy(sprite);
        });
    }

    internal static bool TryGet(string? name, out Sprite sprite)
    {
        sprite = null!;
        return name != null && _sprites.TryGetValue(name, out sprite!) && sprite != null;
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
