using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A picture, from a small icon to one that fills a panel. Give it a sprite name, such as one added with Sprites.Add
/// or any sprite the game has, or a <see cref="UnityEngine.Sprite"/> a mod holds. With no width or height it is drawn at
/// the sprite's own size, and a name the game has no sprite for leaves it empty. It only shows a picture, so it takes no
/// clicks and no gamepad focus, which lets it sit inside anything that does.
/// </summary>
/// <example>
/// <code>
/// page.Add(new TimbnImage("timbn_donkey") { Fill = 1f });
/// page.Row(row =>
/// {
///     row.Add(new TimbnImage(TimbnGameIds.Items.Apple) { Width = 48f, Height = 48f });
///     row.Add(new TimbnText("Apple"));
/// });
/// </code>
/// </example>
public sealed class TimbnImage : TimbnComponent
{
    private static readonly Dictionary<string, Sprite> _gameSprites = [];

    private Image? _image;
    private string? _name;
    private Sprite? _sprite;

    /// <summary>Makes a picture from a sprite name, or an empty one when it is null.</summary>
    /// <param name="spriteName">The sprite's name, such as one added with Sprites.Add or an item's id from TimbnGameIds.Items.</param>
    public TimbnImage(string? spriteName = null)
    {
        _name = spriteName;
    }

    /// <summary>Makes a picture from a sprite the mod holds.</summary>
    /// <param name="sprite">The sprite to draw.</param>
    public TimbnImage(Sprite sprite)
    {
        _sprite = sprite;
    }

    /// <summary>
    /// How the picture is sized in its box. By default it keeps its shape and sits in the middle of the box. Set it before
    /// the image is put on a page.
    /// </summary>
    public TimbnImageFit Fit { get; set; }

    /// <summary>A fixed width, or null to use the sprite's own, or the room the parent gives with <see cref="TimbnImageFit.Width"/>. Set it before the image is put on a page.</summary>
    public float? Width { get; set; }

    /// <summary>A fixed height, or null to use the sprite's own. <see cref="TimbnImageFit.Width"/> ignores it. Set it before the image is put on a page.</summary>
    public float? Height { get; set; }

    /// <summary>
    /// A share of the room the box's parent has left along its direction, by weight, or null for none. Set it before
    /// the image is put on a page.
    /// </summary>
    public float? Fill { get; set; }

    /// <summary>Shows nothing. A literal null cannot be passed to <see cref="SetSprite(string?)"/>, since it fits the sprite overload too.</summary>
    public void Clear() => SetSprite((string?)null);

    /// <summary>Shows another sprite by name, or nothing when the name is null. Before the image is on a page it sets what it shows once it is.</summary>
    /// <param name="spriteName">The sprite's name, such as one added with Sprites.Add or an item's id from TimbnGameIds.Items.</param>
    public void SetSprite(string? spriteName)
    {
        _name = spriteName;
        _sprite = null;
        Draw();
    }

    /// <summary>Shows a sprite the mod holds, or nothing when it is null. Before the image is on a page it sets what it shows once it is.</summary>
    /// <param name="sprite">The sprite to draw.</param>
    public void SetSprite(Sprite? sprite)
    {
        _sprite = sprite;
        _name = null;
        Draw();
    }

    protected internal override RectTransform Build(Transform parent)
    {
        var box = TimbnUi.Box(parent, "Image");
        _image = box.gameObject.AddComponent<Image>();
        _image.raycastTarget = false;
        _image.preserveAspect = Fit != TimbnImageFit.Stretch;
        Draw();

        if (Fit == TimbnImageFit.Width)
            box.gameObject.AddComponent<TimbnAspectHeight>().Source = _image;

        var height = Fit == TimbnImageFit.Width ? null : Height;
        if (Width != null || height != null)
            TimbnUi.Fixed(box, Width ?? -1f, height ?? -1f);

        TimbnBox.Share(box, Fill, InColumn);
        return box;
    }

    // Unity's own null checks, since a sprite is destroyed with its mod and the image with its page.
    private void Draw()
    {
        if (_image == null)
            return;

        var sprite = _sprite != null ? _sprite : Find(_name);
        _image.sprite = sprite;
        _image.enabled = sprite != null;
    }

    private static Sprite? Find(string? name)
    {
        if (name == null)
            return null;

        if (TimbnSprites.TryGet(name, out var own))
            return own;

        if (_gameSprites.TryGetValue(name, out var known) && known != null)
            return known;

        var sprites = LazySingletonSO<EasySpritesCollection>.Instance;
        if (!sprites.HasSprite(name))
            return null;

        // The game's atlas makes a new clone of the sprite on every call and nothing frees it, so each name asks once.
        return _gameSprites[name] = sprites.GetSprite(name);
    }
}
