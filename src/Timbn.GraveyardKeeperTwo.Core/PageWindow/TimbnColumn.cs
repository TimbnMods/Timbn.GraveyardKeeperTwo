namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A box that lays out what is put in it top to bottom, each as wide as the column, with the same gap Core puts between a
/// page's own parts unless it asks for another. It can sit on one of the game's backgrounds.
/// <see cref="TimbnPageShorthands.Column"/> puts one on a page in a single call.
/// </summary>
/// <param name="build">Fills the column.</param>
public sealed class TimbnColumn(Action<TimbnPageBuilder> build) : TimbnBox
{
    /// <summary>The space between what is in it, or null for the page's own. A gap of zero replaces it too.</summary>
    public float? Gap { get; set; }

    /// <summary>Whether the dark navy panel the Save and Load window's slots sit on is drawn behind the column, for a list of cards.</summary>
    public bool Panel { get; set; }

    /// <summary>Whether the dark brown stone the Inspirations page's columns sit on is drawn behind the column, tiled at its own size.</summary>
    public bool Brown { get; set; }

    /// <summary>A flat color drawn behind the column, for seeing where a layout's boxes really are, or null for none.</summary>
    public Color? Backdrop { get; set; }

    /// <summary>
    /// A value or a callback that is read all the time, and while it is true a gray veil covers the whole column, for what
    /// the player has not entered yet, such as the page of a mod the focus is only passing over in a list. Null for none.
    /// </summary>
    public TimbnBinding<bool>? Dimmed { get; set; }

    protected internal override RectTransform Build(Transform parent)
    {
        var box = TimbnUi.Column(parent, "Column", TimbnPageBuilder.ColumnGap(Gap));
        Place(box);
        if (Backdrop is { } color)
        {
            var flat = box.gameObject.AddComponent<UnityEngine.UI.Image>();
            flat.color = color;
            flat.raycastTarget = false;
        }

        if (Brown)
            TimbnUi.Skin(box, TimbnTheme.FrameBack).type = UnityEngine.UI.Image.Type.Tiled;
        else if (Panel)
            TimbnUi.Skin(box, TimbnTheme.ListBack);

        build(BuilderFor(box));
        if (Dimmed is not { } dim)
            return box;

        var veil = TimbnUi.Stretch(TimbnUi.Box(box, "Veil"));
        TimbnUi.Element(veil).ignoreLayout = true;
        var art = veil.gameObject.AddComponent<UnityEngine.UI.Image>();
        art.color = new Color(0.1f, 0.1f, 0.13f, 0.65f);
        art.raycastTarget = false;
        veil.SetAsLastSibling();
        Watcher!.Watch(() => dim.Current, on => veil.gameObject.SetActive(on));
        return box;
    }
}
