using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Makes one of Core's windows out of a copy of the game's controls window, so it keeps the game's frame, title bar,
/// close button, window stack and gamepad navigation. The caller swaps in its own content, and this puts the Character
/// window's gold title bar on it and clears the room the content goes in.
/// </summary>
internal static class TimbnWindowFrame
{
    private const float _frameInset = 13f;
    private const float _bandGap = 2f;
    private const float _tipsGap = 4f;
    private const string _backName = "TimbnBack";
    private const string _tipsPath = "GenericWIndowLayout/Content/Gamepad/ButtonContent/ButtonTipsStr";

    private static readonly AccessTools.FieldRef<LazyWindow<LazyWidgetDataBase>, LazyButton> _closeButton =
        AccessTools.FieldRefAccess<LazyWindow<LazyWidgetDataBase>, LazyButton>("closeButton");

    internal static LazyButton CloseButton(LazyWindow<LazyWidgetDataBase> window) => _closeButton(window);

    /// <summary>Whether the game has made the controls window a copy is taken from.</summary>
    internal static bool CanCopy => LazyUI.IsInitialized && LazyUI.GetWindow<UIGameBindingSettingsWindow>() != null;

    /// <summary>
    /// Copies the game's controls window and puts the given window class on the copy in place of the game's. Nothing is
    /// built in it yet, and the copy is inactive until it opens. A miss is null, when the game has not made the source.
    /// </summary>
    internal static T? Copy<T>(string name)
        where T : LazyWindow<LazyWidgetDataBase>
    {
        // Never ask for a type of ours before it exists, since a miss tries an addressable load and caches null.
        var source = LazyUI.GetWindow<UIGameBindingSettingsWindow>();
        if (source == null)
            return null;

        var copy = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent, false);
        copy.SetActive(false);
        copy.name = name;
        var game = copy.GetComponent<UIGameBindingSettingsWindow>();
        var close = _closeButton(game);
        UnityEngine.Object.DestroyImmediate(game);

        var window = copy.AddComponent<T>();
        _closeButton(window) = close;
        return window;
    }

    /// <summary>
    /// The title bar, which takes the Character window's gold bar and ornaments since the controls window's bar is the same
    /// brown as the column headers below it. The window keeps the game's own padding around the content, the same as the
    /// pause menu's.
    /// </summary>
    internal static RectTransform Header(Transform window)
    {
        var header = (RectTransform)window.Find("GenericWIndowLayout/Frame/HeaderGroup");
        var gold = TimbnTheme.GoldBar;
        Reskin(header.Find("Background"), gold);
        Reskin(header.Find("DecorCommonLeft"), gold?.Find("DecorLeft"), 0f);
        Reskin(header.Find("DecorCommonRight"), gold?.Find("DecorRight"), 1f);
        return header;
    }

    /// <summary>
    /// Swaps the controls window's dark side panels for the slate the Inspirations page has behind its top row, tiled inside
    /// the frame, or puts them back. The frame itself draws the thin lip along its inner edge, so the slate stops short of it.
    /// </summary>
    internal static void UseCharacterBack(Transform window, bool on, Color? flat)
    {
        if (window.Find("GenericWIndowLayout/Frame") is not RectTransform frame)
            return;

        var back = frame.Find(_backName) as RectTransform;
        var skin = TimbnTheme.Slate;
        if (on && back == null && skin.Sprite != null)
        {
            back = TimbnUi.Stretch(TimbnUi.Box(frame, _backName), -_frameInset, -_frameInset, -_frameInset, -_frameInset);
            TimbnUi.Element(back).ignoreLayout = true;
            var image = back.gameObject.AddComponent<Image>();
            image.sprite = skin.Sprite;
            image.type = skin.Type;
            image.color = skin.Color;
            image.raycastTarget = false;
            back.SetAsFirstSibling();
        }

        on &= back != null;
        if (on)
        {
            back!.offsetMin = new Vector2(_frameInset, _frameInset);
            back.offsetMax = new Vector2(-_frameInset, -_frameInset);
        }

        if (on && back!.GetComponent<Image>() is { } art)
        {
            art.sprite = flat == null ? skin.Sprite : null;
            art.type = flat == null ? skin.Type : Image.Type.Simple;
            art.color = flat ?? skin.Color;
        }

        frame.Find("BackMask")?.gameObject.SetActive(!on);
        back?.gameObject.SetActive(on);
    }

    /// <summary>
    /// Pulls the content in close to the frame and just under the title bar, for a window whose content brings its own
    /// margins, such as columns with plates and rails that reach out to the frame.
    /// </summary>
    internal static void Tighten(Transform window, RectTransform header)
    {
        var inset = (int)_frameInset;
        var layout = window.Find("GenericWIndowLayout").GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(inset, inset, (int)(-header.offsetMin.y + _bandGap), inset);
    }

    /// <summary>
    /// The window's content column, set up so its children keep their own size and the content sizes itself. The game's
    /// own children are returned for the caller to destroy once its replacement is in.
    /// </summary>
    internal static (Transform Content, List<Transform> Old) Content(Transform window)
    {
        var content = window.Find("GenericWIndowLayout/Content");
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.padding = new RectOffset();
        layout.spacing = 0f;

        // A page that changes height has the window one frame behind, and a centered page would jump for that frame.
        layout.childAlignment = TextAnchor.UpperCenter;
        return (content, content.Cast<Transform>().ToList());
    }

    /// <summary>
    /// The window base finds its gamepad hints once in Init, so they move out of the old content before it goes. They
    /// hang under the window's frame, out of its layout, the way the game's own windows show them.
    /// </summary>
    internal static void KeepTips(Transform window)
    {
        if (window.Find(_tipsPath) is not RectTransform tips)
            return;

        tips.SetParent(window.Find("GenericWIndowLayout"), false);
        TimbnUi.Element(tips).ignoreLayout = true;
        tips.anchorMin = tips.anchorMax = new Vector2(0.5f, 0f);
        tips.pivot = new Vector2(0.5f, 1f);
        tips.anchoredPosition = new Vector2(0f, -_tipsGap);
    }

    // An end ornament (side 0 for left, 1 for right) is drawn at its own size against its end of the bar, since the
    // Character window's ornaments are wider than the slots they go in.
    private static void Reskin(Transform? target, Transform? source, float side = -1f)
    {
        if (target?.GetComponent<Image>() is not { } image || source?.GetComponent<Image>() is not { } from)
            return;

        image.sprite = from.sprite;
        image.type = from.type;
        image.color = from.color;
        if (side < 0f)
            return;

        var rect = image.rectTransform;
        var y = rect.anchoredPosition.y;
        image.SetNativeSize();
        var width = rect.rect.width;
        rect.anchoredPosition = new Vector2(side == 0f ? width * rect.pivot.x : -width * (1f - rect.pivot.x), y);
    }
}
