using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A row of the game's own tabs on a copy of the character window's header, the brown bar with its end ornaments,
/// since the tab separators are drawn to sit on that bar. Only whole tabs are shown, centered like the game's own when
/// they all fit. When they do not all fit, they are split into pages that start at the left. A page after the first
/// opens with a "..." that turns back a page, and a page with more after it ends with a "..." that turns forward, and
/// turning a page leaves the chosen tab as it is. The game's left and right arrows show at the ends and choose the
/// previous or next tab, and the bar turns to that tab's page when it is on another one. The end ornament always comes
/// right after the last item shown, so the right end moves with how much fits. The window's own bar is stepped by the
/// bumpers and shows their icons. A bar on a page is a gamepad stop instead, stepped by left and right like a choice,
/// since the bumpers already belong to the window's bar.
/// </summary>
internal sealed class TimbnTabsWidget : TimbnWidget
{
    private const float _arrowInset = 28f;
    private const float _keyScale = 0.75f;
    private const string _moreText = "...";

    private static readonly string[] _helpers = ["NextTabGamepadHelper", "PrevTabGamepadHelper"];

    private static readonly AccessTools.FieldRef<CharPageTabButton, GameObject> _separator =
        AccessTools.FieldRefAccess<CharPageTabButton, GameObject>("rightSeparator");

    private readonly List<CharPageTabButton> _buttons = [];
    private readonly List<float> _widths = [];
    private readonly CharPageTabButton? _back;
    private readonly CharPageTabButton? _next;
    private float _moreWidth;
    private int _page;
    private int _shownSelected = -1;
    private readonly Func<int> _get;
    private readonly Action<int> _set;
    private readonly RectTransform _viewport;
    private readonly RectTransform _row;
    private readonly GameObject _left;
    private readonly GameObject _right;
    private readonly TMPro.TextMeshProUGUI _leftKey;
    private readonly TMPro.TextMeshProUGUI _rightKey;
    private bool _gamepad;
    private readonly bool _onPage;
    private readonly float _leftOverhang;
    private readonly float _rightOverhang;
    private readonly float _spacing;
    private bool _dirty = true;

    /// <summary>Whether the bar draws the bumpers' glyphs at its ends with a gamepad, so the page prints no hint for them.</summary>
    public bool ShowsBumpers { get; }

    public TimbnTabsWidget(GameObject root, IReadOnlyList<string> titles, Func<int> get, Action<int> set, bool onPage = false, bool showsBumpers = false)
        : base((RectTransform)root.transform, null)
    {
        _get = get;
        _set = set;
        _onPage = onPage;
        ShowsBumpers = showsBumpers;
        if (root.transform.Find("CloseButton") is { } close)
            UnityEngine.Object.DestroyImmediate(close.gameObject);

        // FrameDown is the splitter whose pointed ends hang onto the character window's side frame, so here they dangle.
        if (root.transform.Find("Background") is RectTransform background)
        {
            Fill(background, 0f);
            if (background.Find("FrameDown") is { } splitter)
                UnityEngine.Object.DestroyImmediate(splitter.gameObject);
        }

        _row = (RectTransform)root.transform.Find("Windows Tabs Content");
        foreach (var helper in _helpers)
        {
            if (_row.Find(helper) is { } found)
                UnityEngine.Object.DestroyImmediate(found.gameObject);
        }

        _spacing = _row.GetComponent<HorizontalLayoutGroup>()?.spacing ?? 0f;
        _leftOverhang = _row.Find("DecorLeft") is RectTransform decorLeft ? decorLeft.rect.width * decorLeft.pivot.x - decorLeft.anchoredPosition.x : 0f;
        _rightOverhang = _row.Find("DecorRight") is RectTransform decorRight ? decorRight.rect.width * (1f - decorRight.pivot.x) + decorRight.anchoredPosition.x : 0f;

        // The mask only matters for the first frame or two, before the tabs are measured and the extra ones hidden.
        _viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).transform;
        _viewport.SetParent(root.transform, false);
        Fill(_viewport, _arrowInset);
        _row.SetParent(_viewport, false);
        _row.anchorMin = _row.anchorMax = new Vector2(0f, 0.5f);
        _row.pivot = new Vector2(0f, 0.5f);
        _row.anchoredPosition = new Vector2(_leftOverhang, 0f);

        var tabs = _row.GetComponentsInChildren<CharPageTabButton>(true);
        var template = tabs.FirstOrDefault(tab => !tab.gameObject.activeSelf) ?? tabs[0];
        foreach (var tab in tabs.Where(tab => tab != template))
            UnityEngine.Object.DestroyImmediate(tab.gameObject);

        // The end ornaments come first, as in the game, so every tab draws over their dithered inner edges.
        if (titles.Count > 0)
            _back = MoreTab(template, "Back", -1);

        for (var i = 0; i < titles.Count; i++)
        {
            var index = i;
            var copy = UnityEngine.Object.Instantiate(template.gameObject, _row, false);
            copy.SetActive(true);
            copy.transform.SetAsLastSibling();
            var button = copy.GetComponent<CharPageTabButton>();
            button.Init(titles[i], () => Pick(index), i == titles.Count - 1);
            _buttons.Add(button);
        }

        if (titles.Count > 0)
            _next = MoreTab(template, "Next", 1);

        UnityEngine.Object.DestroyImmediate(template.gameObject);

        // With no tabs the two end ornaments would meet in the middle of an empty bar.
        if (titles.Count == 0)
        {
            _row.Find("DecorLeft")?.gameObject.SetActive(false);
            _row.Find("DecorRight")?.gameObject.SetActive(false);
        }

        (_left, _right) = Arrows(root.transform);
        _leftKey = KeyIcon(root.transform, 0f);
        _rightKey = KeyIcon(root.transform, 1f);
        root.AddComponent<Scroller>().Owner = this;
        if (onPage && titles.Count > 1)
            GamepadStop(Step);

        Refresh();
    }

    public override void Refresh()
    {
        var selected = _get();
        for (var i = 0; i < _buttons.Count; i++)
            _buttons[i].UpdateState(i == selected);

        _dirty = true;
    }

    // Runs after the layout pass. The tab labels have no width until their text is first drawn, so the tabs are
    // measured once they all have one, and only then are the ones that do not fit hidden.
    private void Apply()
    {
        if (!_dirty || Root.rect.width <= 1f || !Measure())
            return;

        _dirty = false;
        if (_buttons.Count == 0)
        {
            _left.SetActive(false);
            _right.SetActive(false);
            _leftKey.gameObject.SetActive(false);
            _rightKey.gameObject.SetActive(false);
            return;
        }

        // A unit of slack, since tabs that fill the bar exactly can add up a hair over it.
        var available = Root.rect.width - 2f * _arrowInset - _leftOverhang - _rightOverhang + 1f;
        var pages = Pages(available);
        var selected = Mathf.Clamp(_get(), 0, _buttons.Count - 1);
        if (selected != _shownSelected)
        {
            _shownSelected = selected;
            _page = pages.FindIndex(page => selected >= page.First && selected <= page.Last);
        }

        _page = Mathf.Clamp(_page, 0, pages.Count - 1);
        (var first, var last) = pages[_page];
        var hasBack = first > 0;
        var hasNext = last < _buttons.Count - 1;
        for (var i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].gameObject.SetActive(i >= first && i <= last);
            _separator(_buttons[i]).SetActive(i < last || i == last && hasNext);
        }

        Show(_back, hasBack);
        Show(_next, hasNext);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_row);

        // Tabs that all fit sit in the middle of the bar, like the game's own, and paged ones start at the left.
        var allFit = pages.Count == 1;
        var group = _leftOverhang + _row.rect.width + _rightOverhang;
        var x = allFit ? (_viewport.rect.width - group) / 2f + _leftOverhang : _leftOverhang;
        _row.anchoredPosition = new Vector2(x, 0f);
        // With a gamepad the bumpers step the window's tabs, so their icons stand where the arrows would. A bar on a page
        // keeps its arrows on a gamepad, as a choice does, since left and right step it.
        _gamepad = LazyInput.IsGamepadActive;
        var arrows = _onPage ? !allFit || _gamepad && _buttons.Count > 1 && !ShowsBumpers : !allFit && !_gamepad;
        _left.SetActive(arrows);
        _right.SetActive(arrows);
        var showKeys = (!_onPage || ShowsBumpers) && _gamepad && _buttons.Count > 1;
        _leftKey.gameObject.SetActive(showKeys);
        _rightKey.gameObject.SetActive(showKeys);
        _leftKey.text = LazyGameKeyTip.Get(GameKey.PrevTab, "", translate: false);
        _rightKey.text = LazyGameKeyTip.Get(GameKey.NextTab, "", translate: false);
        _left.GetComponent<LazyButton>().interactable = selected > 0;
        _right.GetComponent<LazyButton>().interactable = selected < _buttons.Count - 1;
    }

    // Fills each page with as many whole tabs as fit beside its "..." items, and always at least one.
    private List<(int First, int Last)> Pages(float available)
    {
        var pages = new List<(int First, int Last)>();
        var first = 0;
        while (first < _buttons.Count)
        {
            var last = first;
            while (last + 1 < _buttons.Count && Width(first, last + 1) + MoreRoom(first > 0) + MoreRoom(last + 1 < _buttons.Count - 1) <= available)
                last++;

            pages.Add((first, last));
            first = last + 1;
        }

        return pages;
    }

    private float MoreRoom(bool shown) => shown ? _spacing + _moreWidth : 0f;

    private static void Show(CharPageTabButton? more, bool shown)
    {
        if (more == null)
            return;

        more.gameObject.SetActive(shown);
        more.UpdateState(false);
        _separator(more).SetActive(false);
    }

    // A "..." item, never chosen itself, that turns the bar a page back or forward.
    private CharPageTabButton MoreTab(CharPageTabButton template, string name, int direction)
    {
        var copy = UnityEngine.Object.Instantiate(template.gameObject, _row, false);
        copy.name = name;
        copy.SetActive(false);
        copy.transform.SetAsLastSibling();
        var more = copy.GetComponent<CharPageTabButton>();
        more.Init(_moreText, () =>
        {
            _page += direction;
            _dirty = true;
        }, true);
        more.UpdateState(false);
        _separator(more).SetActive(false);
        return more;
    }

    private bool Measure()
    {
        if (_widths.Count == _buttons.Count)
            return true;

        // A "..." item is only measured while shown, so it is turned on for the measuring and Apply sets it after.
        _next?.gameObject.SetActive(true);
        foreach (var button in _next == null ? _buttons : [.. _buttons, _next])
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)button.transform);

        var widths = _buttons.Select(button => ((RectTransform)button.transform).rect.width).ToList();
        _moreWidth = _next == null ? 0f : ((RectTransform)_next.transform).rect.width;
        if (widths.Any(width => width <= 1f) || _next != null && _moreWidth <= 1f)
            return false;

        _widths.AddRange(widths);
        return true;
    }

    private float Width(int first, int last)
    {
        var width = 0f;
        for (var i = first; i <= last; i++)
            width += _widths[i];

        return width + _spacing * (last - first);
    }

    // The arrows step to the next tab, and the shown tabs move along when the chosen one would not be shown.
    private void Step(int direction)
    {
        var next = Mathf.Clamp(_get() + direction, 0, _buttons.Count - 1);
        if (next != _get())
            Pick(next);
    }

    // The triggers step the tabs round, the way they do the Mods window's.
    internal void Cycle(int direction)
    {
        if (_buttons.Count > 1)
            Pick((_get() + direction + _buttons.Count) % _buttons.Count);
    }

    private void Pick(int index)
    {
        _set(index);
        Refresh();
    }

    private (GameObject Left, GameObject Right) Arrows(Transform root) =>
        (Arrow(root, 0f, () => Step(-1)), Arrow(root, 1f, () => Step(1)));

    private static GameObject Arrow(Transform root, float side, Action onClick)
    {
        var button = TimbnTheme.Arrow(root, right: side > 0f);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(side, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(side == 0f ? _arrowInset / 2f : -_arrowInset / 2f, 0f);
        button.onClick.AddListener(() => onClick());
        return button.gameObject;
    }

    internal static TMPro.TextMeshProUGUI KeyIcon(Transform root, float side)
    {
        var icon = TimbnUi.Text(root, TimbnTheme.Label, string.Empty);
        icon.alignment = TMPro.TextAlignmentOptions.Center;

        // The trigger glyphs are taller than the plate, so they are drawn smaller than the text.
        icon.fontSize *= _keyScale;
        var rect = icon.rectTransform;
        TimbnUi.Element(icon).ignoreLayout = true;
        rect.anchorMin = rect.anchorMax = new Vector2(side, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(_arrowInset, _arrowInset);
        rect.anchoredPosition = new Vector2(side == 0f ? _arrowInset / 2f : -_arrowInset / 2f, 0f);
        icon.gameObject.SetActive(false);
        return icon;
    }

    private static void Fill(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, 0f);
        rect.offsetMax = new Vector2(-inset, 0f);
    }

    private sealed class Scroller : MonoBehaviour
    {
        public TimbnTabsWidget? Owner { get; set; }

        private void LateUpdate()
        {
            if (Owner == null)
                return;

            // Switching between the gamepad and the mouse swaps the bumper icons and the arrows.
            if (Owner._gamepad != LazyInput.IsGamepadActive)
                Owner.Refresh();

            Owner.Apply();
        }

        // A wider or narrower bar fits a different number of tabs.
        private void OnRectTransformDimensionsChange() => Owner?.Refresh();
    }
}
