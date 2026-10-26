using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's own art and text styles the Mods window draws with, read from the game's windows. Most parts are only a
/// sprite or a text style, and the window builds its own boxes around them. The few small buttons, plates and scroll
/// areas that carry the game's own sounds and states are copied whole, and each copy is one fixed size leaf.
/// </summary>
internal static class TimbnTheme
{
    private const string _slotSelectionPath = "Selection";
    private const string _redButtonPath = "GenericWIndowLayout/Content/Keyboard/ButtonContent/RedButtonWithTextStretch";
    private const string _titlePath = "GenericWIndowLayout/Frame/HeaderGroup/Header";
    private const string _scrollPath = "GenericWIndowLayout/Content/Keyboard/Scroll";
    private const string _splitterPath = "Content/Pages/CharInspirationPage/Splitter";
    private const string _slotPlusPath = "Root/Plus";
    private const string _slatePath = "Content/Pages/CharInspirationPage/TopBack";
    private const string _goldBarPath = "Content/HeaderGroup/Background";
    private const string _tabBarPath = "Content/HeaderGroup";
    private const string _sectionPath = "Content/Pages/CharMainPage/PerksAndBuffs/Scroll View/Viewport/Content/Buffs Widget/HeaderBackGround";

    // The clear edge left and right of each kind of art, measured in game from screen pixels against the outline the
    // text and every control draw first (2 px a unit). The small arrow, minus, plus and rebind buttons keep 4, the red
    // buttons 2 and the value box half a unit.
    internal const float SmallButtonEdge = 4f;
    internal const float RedButtonEdge = 2f;
    internal const float ValueBoxEdge = 0.5f;

    private static readonly AccessTools.FieldRef<UIGameSettingsWindow, UISwitchButton> _switch =
        AccessTools.FieldRefAccess<UIGameSettingsWindow, UISwitchButton>("vSyncButton");

    private static readonly AccessTools.FieldRef<UIGameSettingsWindow, UISlider> _slider =
        AccessTools.FieldRefAccess<UIGameSettingsWindow, UISlider>("musicVolumeSlider");

    private static readonly AccessTools.FieldRef<UIItemCountWindow, UIItemCell> _itemCell =
        AccessTools.FieldRefAccess<UIItemCountWindow, UIItemCell>("itemCell");

    private static readonly AccessTools.FieldRef<UIResourceBasedCraftWindow, UIItemCell> _choiceCell =
        AccessTools.FieldRefAccess<UIResourceBasedCraftWindow, UIItemCell>("mainIngredient");

    private static readonly AccessTools.FieldRef<UISaveSlotsWindow, UISaveSlot> _slotPrefab =
        AccessTools.FieldRefAccess<UISaveSlotsWindow, UISaveSlot>("uiSaveSlotPrefab");

    private static readonly AccessTools.FieldRef<UISaveSlot, Image> _slotBack =
        AccessTools.FieldRefAccess<UISaveSlot, Image>("slotBack");

    private static readonly AccessTools.FieldRef<UISaveSlot, TextStyle> _slotNameStyle =
        AccessTools.FieldRefAccess<UISaveSlot, TextStyle>("timePart2Style");

    private static readonly AccessTools.FieldRef<UISaveSlot, TextStyle> _slotDetailStyle =
        AccessTools.FieldRefAccess<UISaveSlot, TextStyle>("daysPart2Style");

    private static readonly AccessTools.FieldRef<CharacterWindow, CharPageTabButton> _tab =
        AccessTools.FieldRefAccess<CharacterWindow, CharPageTabButton>("charPageTabButtonPrefab");

    private static readonly AccessTools.FieldRef<UIGameBindingElement, GameObject> _recordObject =
        AccessTools.FieldRefAccess<UIGameBindingElement, GameObject>("recordObject");

    private static readonly AccessTools.FieldRef<UIGameBindingSettingsWindow, LazyButton> _resetButton =
        AccessTools.FieldRefAccess<UIGameBindingSettingsWindow, LazyButton>("restoreDefaultBindings");

    private static readonly AccessTools.FieldRef<UIGameBindingElement, LazyButton> _bindButton =
        AccessTools.FieldRefAccess<UIGameBindingElement, LazyButton>("button");

    /// <summary>A save slot's name line, the yellow every setting and mod is named in.</summary>
    internal static TimbnTextStyle Name => new(Slot is { } slot ? _slotNameStyle(slot) : null, 16f);

    /// <summary>A save slot's "Days played" line, the gray of a mod's version and a setting's description.</summary>
    internal static TimbnTextStyle Detail => new(Slot is { } slot ? _slotDetailStyle(slot) : null, 16f);

    /// <summary>The bright yellow a settings row shows its value in.</summary>
    internal static TimbnTextStyle Value => StyleOf(SwitchRow?.transform.Find("Back/Value"));

    /// <summary>The plain gray a settings row names itself in.</summary>
    internal static TimbnTextStyle Label => StyleOf(SwitchRow?.transform.Find("LeftName"));

    /// <summary>A window title's text.</summary>
    internal static TimbnTextStyle Title => StyleOf(Binding?.transform.Find(_titlePath));

    /// <summary>The white of a section plate's title, such as the Character window's Buffs header.</summary>
    internal static TimbnTextStyle SectionText => StyleOf(Section?.Find("Header"));

    internal static TimbnSkin Card => new(Slot is { } slot ? _slotBack(slot) : null);

    internal static TimbnSkin CardHighlight => SkinOf(Slot?.transform.Find(_slotSelectionPath));

    internal static TimbnSkin ValueBox => SkinOf(SwitchRow?.transform.Find("Back"));

    internal static TimbnSkin SectionPlate => SkinOf(Section);

    // The game spells these "Decot".
    internal static void SectionEnds(RectTransform plate)
    {
        CopyArt(Section?.Find("DecotLeft"), plate);
        CopyArt(Section?.Find("DecotRight"), plate);
    }

    // The Character window's dark interior, which an edge to edge window uses instead of the controls window's sides.
    internal static TimbnSkin FrameBack => SkinOf(Character?.transform.Find("Content/Frame/Back"));

    // The dark panel the Save and Load window's slots sit on.
    internal static TimbnSkin ListBack => SkinOf(LazyUI.GetWindow<UISaveSlotsWindow>()?.transform.Find("GenericWIndowLayout/Frame/BackMask/Back"));

    internal static TimbnSkin Slate => SkinOf(Character?.transform.Find(_slatePath));

    internal static Transform? GoldBar => Character?.transform.Find(_goldBarPath);

    // The settings row's left arrow, or its right one, with the game's sprites, sounds and pressed states.
    internal static LazyButton Arrow(Transform parent, bool right) => Button(Part(SwitchRow, right ? "ToRight" : "ToLeft"), parent, SmallButtonEdge);

    /// <summary>The Controls window's red button that puts every key back to its default, with its turning arrow.</summary>
    internal static LazyButton Reset(Transform parent) => Button(Part(Binding is { } window ? _resetButton(window) : null, ""), parent, RedButtonEdge);

    // The settings slider's track, fill and handle with their sprites and pressed states, laid out at the game's own height.
    // The game's slider script wires it in code, so the copy calls nothing of the window it came from.
    internal static Slider SliderTrack(Transform parent)
    {
        var source = (RectTransform)Part(SliderRow, "Slider");
        var track = Clone(source.gameObject, parent);
        TimbnUi.Fixed(track.transform, height: source.rect.height);
        return track.GetComponent<Slider>();
    }

    internal static LazyButton Minus(Transform parent) => Button(Part(SliderRow, "Minus"), parent, SmallButtonEdge);

    internal static LazyButton Plus(Transform parent) => Button(Part(SliderRow, "Plus"), parent, SmallButtonEdge);

    /// <summary>A copy of the item count window's item cell, the square with an icon and a count, or null before the game's windows are made.</summary>
    internal static UIItemCell? ItemCell(Transform parent) => ItemCellPrefab is { } prefab ? Clone(prefab.gameObject, parent).GetComponent<UIItemCell>() : null;

    /// <summary>
    /// A copy of the plus the study table and firewood shed draw over their empty slot, the one the player fills from the
    /// inventory, or null when the game's window is missing it.
    /// </summary>
    internal static GameObject? SlotPlus(Transform parent) =>
        LazyUI.GetWindow<UIResourceBasedCraftWindow>() is { } window && _choiceCell(window).transform.Find(_slotPlusPath) is { } plus
            ? Clone(plus.gameObject, parent)
            : null;

    internal static GameObject RedButton(Transform parent) => Clone(Part(Binding, _redButtonPath).gameObject, parent);

    // The Inspirations page's rails with both header plates and the divider between them as its children.
    internal static GameObject Splitter(Transform parent) => Clone(Part(Character, _splitterPath).gameObject, parent);

    // Laid out at the game's own height, as wide as it is given.
    internal static GameObject TabBar(Transform parent)
    {
        var source = (RectTransform)Part(Character, _tabBarPath);
        var bar = Clone(source.gameObject, parent);
        TimbnUi.Fixed(bar.transform, height: source.rect.height);
        return bar;
    }

    internal static GameObject Scroll(Transform parent) => Clone(Part(Binding, _scrollPath).gameObject, parent);

    internal static CharPageTabButton? TabTemplate => Character is { } window ? _tab(window) : null;

    // The Controls window keeps its rows in the game's pool, so one is borrowed to copy its rebind button and recording dot
    // from and handed straight back.
    internal static (LazyButton? Button, GameObject? Recording) Rebind(Transform parent)
    {
        if (Binding == null)
            return (null, null);

        var pooled = UIPrefabsPooler.Instance.GetElementFromPool<UIGameBindingElement>(parent);
        var button = Button(_bindButton(pooled).transform, parent, SmallButtonEdge);
        var recording = Clone(_recordObject(pooled), parent);
        UIPrefabsPooler.Instance.ReleaseElementToPool(pooled);
        return (button, recording);
    }

    internal static GameObject Clone(GameObject source, Transform parent)
    {
        var copy = UnityEngine.Object.Instantiate(source, parent, false);
        copy.name = source.name;
        foreach (var label in copy.GetComponentsInChildren<LocalizedLabel>(true))
            UnityEngine.Object.DestroyImmediate(label);

        foreach (var platform in copy.GetComponentsInChildren<LazyPlatformDependentElement>(true))
            UnityEngine.Object.DestroyImmediate(platform);

        // The game's windows lay themselves out once and turn their layout off (RefreshContentFitterAndDisable), so a
        // copy taken after that could not size itself to its own text.
        foreach (var layout in copy.GetComponentsInChildren<LayoutGroup>(true))
            layout.enabled = true;

        foreach (var fitter in copy.GetComponentsInChildren<ContentSizeFitter>(true))
            fitter.enabled = true;

        // Listeners wired in the prefab would still call the game window the part was copied from.
        foreach (var button in copy.GetComponentsInChildren<Button>(true))
        {
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        }

        foreach (var selectable in copy.GetComponentsInChildren<Selectable>(true))
            selectable.navigation = TimbnUi.NoNavigation;

        copy.SetActive(true);
        return copy;
    }

    private static LazyButton Button(Transform source, Transform parent, float edge)
    {
        var button = Clone(source.gameObject, parent).GetComponent<LazyButton>();
        button.onClick.RemoveAllListeners();
        // The Controls row pins its rebind button to its right edge outside its layout, and here the button is laid out.
        TimbnUi.Element(button).ignoreLayout = false;
        // The game's own size, which is not always square (the slider's minus and plus are 25 by 26).
        var size = ((RectTransform)source).rect.size;
        TimbnUi.Fixed(button, size.x, size.y);
        TimbnUi.TrimArt((RectTransform)button.transform, edge);
        return button;
    }

    private static UIGameSettingsWindow? Settings => LazyUI.GetWindow<UIGameSettingsWindow>();

    private static UISwitchButton? SwitchRow => Settings is { } window ? _switch(window) : null;

    private static UISlider? SliderRow => Settings is { } window ? _slider(window) : null;

    private static UIItemCell? ItemCellPrefab => LazyUI.GetWindow<UIItemCountWindow>() is { } window ? _itemCell(window) : null;

    private static UISaveSlot? Slot => LazyUI.GetWindow<UISaveSlotsWindow>() is { } window ? _slotPrefab(window) : null;

    private static CharacterWindow? Character => LazyUI.GetWindow<CharacterWindow>();

    private static UIGameBindingSettingsWindow? Binding => LazyUI.GetWindow<UIGameBindingSettingsWindow>();

    private static Transform? Section => Character?.transform.Find(_sectionPath);

    // A part copied whole has nothing to fall back to, so a game update that moved it names what is gone instead of a bare
    // null reference. The page that asked for it is built inside TimbnSafe, which logs this and leaves the page as it got.
    private static Transform Part(Component? source, string path)
    {
        var part = source == null ? null : path.Length == 0 ? source.transform : source.transform.Find(path);
        return part != null ? part : throw new InvalidOperationException($"{nameof(TimbnTheme)}|The game's UI has no {(source == null ? "window to copy from" : $"{source.name}/{path}")}, so this part cannot be copied.");
    }

    private static TimbnSkin SkinOf(Transform? part) => new(part != null ? part.GetComponent<Image>() : null);

    // A piece of art placed the way the game places it in the same kind of parent, such as an end ornament.
    private static void CopyArt(Transform? source, RectTransform parent)
    {
        if (source is not RectTransform from || from.GetComponent<Image>() is not { } image)
            return;

        var art = TimbnUi.Box(parent, from.name);
        new TimbnSkin(image).Paint(art.gameObject.AddComponent<Image>()).raycastTarget = false;
        art.anchorMin = from.anchorMin;
        art.anchorMax = from.anchorMax;
        art.pivot = from.pivot;
        art.sizeDelta = from.sizeDelta;
        art.anchoredPosition = from.anchoredPosition;
        art.localScale = from.localScale;
        TimbnUi.Element(art).ignoreLayout = true;
    }

    private static TimbnTextStyle StyleOf(Transform? part)
    {
        var label = part != null ? part.GetComponent<TextMeshProUGUI>() : null;
        var style = part != null ? part.GetComponent<TextStyleComponent>()?.CurrentTextStyle : null;
        return new TimbnTextStyle(style, label != null ? label.fontSize : 16f);
    }
}

/// <summary>A sprite with the way an image draws it, copied from a game image.</summary>
internal readonly struct TimbnSkin
{
    public TimbnSkin(Image? source)
    {
        Sprite = source != null ? source.sprite : null;
        Type = source != null ? source.type : Image.Type.Simple;
        Color = source != null ? source.color : Color.clear;
        Multiplier = source != null ? source.pixelsPerUnitMultiplier : 1f;
    }

    public Sprite? Sprite { get; }

    public Image.Type Type { get; }

    public Color Color { get; }

    public float Multiplier { get; }

    public Image Paint(Image image)
    {
        image.sprite = Sprite;
        image.type = Type;
        image.color = Sprite != null ? Color : Color.clear;
        image.pixelsPerUnitMultiplier = Multiplier;
        return image;
    }
}

/// <summary>A game text style with the size the game draws it at.</summary>
internal readonly struct TimbnTextStyle
{
    public TimbnTextStyle(TextStyle? style, float size)
    {
        Style = style;
        Size = size;
    }

    public TextStyle? Style { get; }

    public float Size { get; }

    public void Apply(TMP_Text label)
    {
        if (Style != null)
            Style.ApplyStyle(label);

        label.fontSize = Size;
    }
}
