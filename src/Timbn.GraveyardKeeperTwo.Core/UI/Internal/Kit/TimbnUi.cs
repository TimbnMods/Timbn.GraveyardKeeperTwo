using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The layout kit the Mods window is built with. Every box is a row or a column, and every leaf is sized one of three
/// ways, fixed, a share of the space left over (fill) or its content's own size. A margin is a box with padding around
/// what it holds. Art goes behind a box (Skin) or over it (Overlay) and never takes part in the layout, and it may reach
/// past the box's edges.
/// </summary>
internal static class TimbnUi
{
    // The save slot's own margins, the text there starts 8 in from the left and the new game label 5 in from the top.
    internal const int CardPaddingSide = 8;
    private const int _cardPaddingVertical = 5;

    internal static RectTransform Box(Transform parent, string name)
    {
        var box = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        box.SetParent(parent, false);
        return box;
    }

    /// <summary>Children side by side at their own heights, centered, unless stretch makes them all the row's height.</summary>
    internal static RectTransform Row(Transform parent, string name, float gap = 0f, RectOffset? padding = null, TextAnchor align = TextAnchor.MiddleLeft, bool stretch = false)
    {
        var row = Group<HorizontalLayoutGroup>(parent, name, gap, padding, align);
        row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = stretch;
        return row;
    }

    /// <summary>Children side by side at their own heights that move to a new line when the next one does not fit.</summary>
    internal static RectTransform Flow(Transform parent, string name, float gap = 0f, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var box = Box(parent, name);
        var group = box.gameObject.AddComponent<TimbnFlowLayoutGroup>();
        group.Spacing = gap;
        group.childAlignment = align;
        return box;
    }

    /// <summary>Children top to bottom, each as wide as the column.</summary>
    internal static RectTransform Column(Transform parent, string name, float gap = 0f, RectOffset? padding = null, TextAnchor align = TextAnchor.UpperLeft)
    {
        var column = Group<VerticalLayoutGroup>(parent, name, gap, padding, align);
        column.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
        return column;
    }

    /// <summary>Gives a box a fixed width, height or both. A negative size leaves that side as it is.</summary>
    internal static T Fixed<T>(T box, float width = -1f, float height = -1f)
        where T : Component
    {
        var element = Element(box);
        if (width >= 0f)
        {
            element.minWidth = element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        if (height >= 0f)
        {
            element.minHeight = element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        return box;
    }

    /// <summary>
    /// Gives a box a share of the width its row has left, by weight. Its own width counts for nothing, so a long label
    /// cannot take more than its share.
    /// </summary>
    internal static T Fill<T>(T box, float weight = 1f)
        where T : Component
    {
        var element = Element(box);
        element.minWidth = element.preferredWidth = 0f;
        element.flexibleWidth = weight;
        return box;
    }

    /// <summary>Gives a box a share of the height its column has left, by weight.</summary>
    internal static T FillHeight<T>(T box, float weight = 1f)
        where T : Component
    {
        var element = Element(box);
        element.minHeight = element.preferredHeight = 0f;
        element.flexibleHeight = weight;
        return box;
    }

    /// <summary>An empty box of a fixed size, for a gap that differs from its group's spacing.</summary>
    internal static RectTransform Space(Transform parent, float width = 0f, float height = 0f) => Fixed(Box(parent, "Space"), width, height);

    /// <summary>An empty box that takes the room left in a row, pushing what comes after it to the far end.</summary>
    internal static RectTransform Spring(Transform parent) => Fill(Box(parent, "Spring"));

    /// <summary>
    /// Draws a sprite behind everything in a box, outside its layout. The outsets reach past the box's edges, left,
    /// bottom, right and top.
    /// </summary>
    internal static Image Skin(RectTransform box, TimbnSkin skin, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        var image = Art(box, skin, left, bottom, right, top);
        image.transform.SetAsFirstSibling();
        return image;
    }

    /// <summary>Draws a sprite over everything in a box, outside its layout, with outsets as for <see cref="Skin"/>.</summary>
    internal static Image Overlay(RectTransform box, TimbnSkin skin, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        var image = Art(box, skin, left, bottom, right, top);
        image.transform.SetAsLastSibling();
        return image;
    }

    /// <summary>The height an image's art is drawn at on the canvas, for art that is placed by its own size.</summary>
    internal static float NativeHeight(Image image)
    {
        if (image.sprite == null)
            return 0f;

        var perUnit = image.canvas != null ? image.canvas.referencePixelsPerUnit : image.sprite.pixelsPerUnit;
        return image.sprite.rect.height * perUnit / image.sprite.pixelsPerUnit;
    }

    /// <summary>Stretches a box over its parent, reaching past each edge by the outsets, and takes it out of the parent's layout.</summary>
    /// <summary>
    /// Takes a copied piece of the game's art in by the clear edge its sprites keep on the left and right, so what shows
    /// starts and ends where its box does and lines up with every other part of a column. The box gets narrower by the
    /// edges and the art that stretches across it reaches out past it by as much, so nothing moves on screen.
    /// </summary>
    internal static RectTransform TrimArt(RectTransform root, float edge)
    {
        var element = Element(root);
        if (element.preferredWidth > 0f)
            element.preferredWidth = element.minWidth = Mathf.Max(0f, element.preferredWidth - (2f * edge));

        foreach (RectTransform part in root)
        {
            if (part.anchorMin.x != 0f || part.anchorMax.x != 1f)
                continue;

            part.offsetMin = new Vector2(part.offsetMin.x - edge, part.offsetMin.y);
            part.offsetMax = new Vector2(part.offsetMax.x + edge, part.offsetMax.y);
        }

        return root;
    }

    internal static T Stretch<T>(T box, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        where T : Component
    {
        var rect = (RectTransform)box.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(-left, -bottom);
        rect.offsetMax = new Vector2(right, top);
        Element(box).ignoreLayout = true;
        return box;
    }

    /// <summary>
    /// A line of text in a game style. One line is as tall as its font and ends in "..." when it runs out of room, and
    /// wrapped text grows to as many lines as it needs.
    /// </summary>
    internal static TextMeshProUGUI Text(Transform parent, TimbnTextStyle style, string text, bool wrap = false, float? size = null)
    {
        var label = Box(parent, "Text").gameObject.AddComponent<TextMeshProUGUI>();
        style.Apply(label);
        if (size is { } points)
            label.fontSize = points;

        label.enableAutoSizing = false;
        label.raycastTarget = false;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;

        // Wrapped and single lines sit the same way in their box, which is as tall as the text, so a description lines up
        // with a version under a name.
        label.alignment = TextAlignmentOptions.MidlineLeft;
        // A Windows path holds a backslash and r (as in repos), which TextMesh Pro would otherwise read as a return.
        label.parseCtrlCharacters = false;
        label.text = text;
        if (wrap)
            label.gameObject.AddComponent<TimbnWholeHeight>();
        else
            Fixed(label, height: LineHeight(label));

        return label;
    }

    /// <summary>The height of one line of the label's font at its size, which is all the room the text needs.</summary>
    internal static float LineHeight(TMP_Text label)
    {
        var face = label.font.faceInfo;
        return Mathf.Round(face.lineHeight * label.fontSize / face.pointSize);
    }

    /// <summary>A name with a line of description under it, one box, which a page's entries and the mod list both are.</summary>
    internal static RectTransform Entry(Transform parent, string title, string subtitle, bool wrap = true, string marks = "", string detail = "", float indent = 0f)
    {
        var entry = Column(parent, "Entry", EntryGap);
        var lines = new List<TextMeshProUGUI> { Text(entry, TimbnTheme.Name, title) };

        // Marks such as "(Read Only, Advanced)" go on a line of their own under the name, so a long name keeps its room.
        if (marks.Length > 0)
            lines.Add(Text(entry, TimbnTheme.Detail, marks));

        if (subtitle.Length > 0)
            lines.Add(Text(entry, TimbnTheme.Detail, subtitle, wrap));

        if (detail.Length > 0)
            lines.Add(Text(entry, TimbnTheme.Detail, detail));

        TimbnTextFit.AlignLeftEdges(lines, indent);
        return entry;
    }

    /// <summary>
    /// The gap between a name and the line of description under it, wherever the two are stacked. The font's line already
    /// has room under the letters, so the two lines are one line height apart, the way a save slot sets its own.
    /// </summary>
    internal const float EntryGap = 0f;

    // The scroll's mask sprite has clear pixels along each edge, so what touches the edge of a scroll's area is cut off by this much.
    internal const float MaskClip = 1f;

    /// <summary>The share of a row's room a control takes after its title, so a description has the rest.</summary>
    internal const float ControlShare = 0.20f;

    internal const string CardName = "Card";

    /// <summary>
    /// Removes everything in a box. Destroy waits for the end of the frame and the gamepad's list of controls is read again
    /// in the same frame, so the old ones are hidden first to keep them out of it.
    /// </summary>
    internal static void Clear(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    /// <summary>A save slot card, a column on the slot's own background with its padding.</summary>
    internal static RectTransform Card(Transform parent, string name)
    {
        var card = Column(parent, name, padding: new RectOffset(CardPaddingSide, CardPaddingSide, _cardPaddingVertical, _cardPaddingVertical));
        Skin(card, TimbnTheme.Card).name = "Back";
        return card;
    }

    /// <summary>
    /// Makes a whole card a button that lights the save slot's yellow frame while the mouse or gamepad is on it, and
    /// returns the frame so a list can keep its chosen card lit. Call it after the card's content is added, so the
    /// frame is drawn over it.
    /// </summary>
    internal static GameObject MakeButton(RectTransform card, Action onClick)
    {
        var frame = Overlay(card, TimbnTheme.CardHighlight);
        frame.name = "Highlight";
        frame.gameObject.SetActive(false);
        var back = card.Find("Back")?.GetComponent<Image>();
        if (back == null)
        {
            back = Skin(card, default);
            back.name = "Back";
        }

        back.raycastTarget = true;
        card.gameObject.AddComponent<GamepadNavigationItem>();
        var button = card.gameObject.AddComponent<LazyButton>();
        button.targetGraphic = back;
        button.transition = Selectable.Transition.None;
        button.navigation = NoNavigation;
        button.onClick.AddListener(() => onClick());
        button.onEnter.AddListener(() => frame.gameObject.SetActive(true));
        button.onExit.AddListener(() => frame.gameObject.SetActive(false));
        button.SetCallbacksIntoGamepadNavigationItem();
        return frame.gameObject;
    }

    /// <summary>
    /// A copy of the controls window's scroll area, with the game's scrollbar and smooth wheel scrolling, returning its
    /// content, a column that grows with what is put in it. The caller sizes the scroll area itself.
    /// </summary>
    internal static RectTransform Scroll(Transform parent, out ScrollRect scroll)
    {
        var copy = (RectTransform)TimbnTheme.Scroll(parent).transform;
        scroll = copy.GetComponent<ScrollRect>();

        // The controls window's viewport sits a few units inside its scroll, so the first row would start below the
        // columns beside it. Its mask is clear along every edge, so it reaches out by that much and the content comes back in.
        if (scroll.viewport is { } viewport)
        {
            Stretch(viewport);
            viewport.offsetMin = new Vector2(-MaskClip, -MaskClip);
            viewport.offsetMax = new Vector2(MaskClip, MaskClip);
        }

        // The controls window's scroll shades its top and bottom edges all the time, which reads as a dark line across
        // the first and last rows here.
        foreach (var fade in new[] { "FadeTop", "FadeBot" })
        {
            if (copy.Find(fade) is { } shade)
                UnityEngine.Object.Destroy(shade.gameObject);
        }

        var list = scroll.content;
        foreach (Transform child in list)
            UnityEngine.Object.Destroy(child.gameObject);

        var layout = list.GetComponent<VerticalLayoutGroup>() ?? list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)MaskClip, (int)MaskClip, (int)MaskClip, (int)MaskClip);
        layout.spacing = 0f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = list.GetComponent<ContentSizeFitter>() ?? list.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return list;
    }

    /// <summary>
    /// The navigation every control here uses. The game moves gamepad focus with its own GamepadNavigationController, and
    /// Rewired's input module also walks Unity's selection with the stick, so a control left on automatic navigation gets
    /// selected behind the game's back. A selected text box turns the game's input off, which leaves the gamepad dead.
    /// </summary>
    internal static Navigation NoNavigation => new() { mode = Navigation.Mode.None };

    internal static LayoutElement Element(Component box) =>
        box.GetComponent<LayoutElement>() ?? box.gameObject.AddComponent<LayoutElement>();

    private static RectTransform Group<T>(Transform parent, string name, float gap, RectOffset? padding, TextAnchor align)
        where T : HorizontalOrVerticalLayoutGroup
    {
        var box = Box(parent, name);
        var group = box.gameObject.AddComponent<T>();
        group.spacing = gap;
        group.padding = padding ?? new RectOffset();
        group.childAlignment = align;
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandWidth = group.childForceExpandHeight = false;
        return box;
    }

    private static Image Art(RectTransform box, TimbnSkin skin, float left, float bottom, float right, float top)
    {
        var art = Box(box, "Art");
        var image = skin.Paint(art.gameObject.AddComponent<Image>());
        image.raycastTarget = false;
        Stretch(art, left, bottom, right, top);
        return image;
    }
}
