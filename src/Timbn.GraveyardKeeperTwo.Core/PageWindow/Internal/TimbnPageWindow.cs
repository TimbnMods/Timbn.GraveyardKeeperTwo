using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The popup a <see cref="TimbnPage"/> is shown in. It is a copy of the game's controls window with its own window class
/// put in place of the game's, so it keeps the game's frame, close button, window stack, Esc, B and gamepad navigation,
/// with the Character window's gold title bar. The content is one area the page fills. Each window is made the first
/// time a page needs it and kept. A page shown while another is open takes its place, and one shown over others opens in
/// the next window up, so questions can stack over a page and over each other.
/// </summary>
internal sealed class TimbnPageWindow : LazyWindow<LazyWidgetDataBase>
{
    private const int _edgeTopTrim = 2;
    private const int _topPadding = 2;
    private const int _bottomPadding = 3;
    private const string _titlePath = "GenericWIndowLayout/Frame/HeaderGroup/Header";

    // By how far up they stack, the bottom one for a page that takes the place of what is open.
    private static readonly List<TimbnPageWindow> _windows = [];

    private RectTransform _root = null!;
    private TMP_Text? _title;
    private TimbnPage? _page;
    private Action? _onClosed;
    private ManualLogSource _logger = null!;
    private IDisposable? _pause;
    private GamepadNavigationItem? _lastFocus;
    private readonly Dictionary<TimbnFaceButton, int> _actionFrame = [];
    private readonly Dictionary<TimbnFaceButton, float> _heldSince = [];
    private const float _holdSeconds = 0.6f;
    private bool _takesSelect;
    private bool _hasSteppers;
    private bool _triggerSteps;
    private RectTransform? _area;

    // The first measure happens while the last page's layout is still settling, so a small page shown after a tall one, such
    // as the Mods window, would be placed for the tall one. It is placed again once the frame's layout is done, before it is drawn.
    private bool _placeAgain;
    private List<TimbnPageWatcher>? _watchers;
    private RectOffset? _gamePadding;
    private RectTransform? _header;
    private int _actionsSeen;
    private TimbnLength _appliedWidth;
    private TimbnLength _appliedHeight;
    private float? _raise;
    private float _pageHeight;

    // Closes every open page, the topmost first, without calling their dismiss handlers.
    internal static void CloseAll()
    {
        for (var depth = _windows.Count - 1; depth >= 0; depth--)
            _windows[depth]?.CloseNow();
    }

    /// <summary>Whether a page can open now, which needs a window already made or the game's window to copy one from.</summary>
    internal static bool CanShow => _windows.Any(window => window != null) || TimbnWindowFrame.CanCopy;

    internal static bool AnyOpen => _windows.Any(window => window != null && window._page != null);

    internal static void DestroyInstance()
    {
        for (var depth = _windows.Count - 1; depth >= 0; depth--)
        {
            if (_windows[depth] == null)
                continue;

            _windows[depth].CloseNow();
            Destroy(_windows[depth].gameObject);
        }

        _windows.Clear();
    }

    /// <summary>
    /// Opens the page, or returns a handle that does nothing when the game has not made the window to copy yet. A page
    /// shown over others opens in the window above the topmost open one without taking its place, and any other page takes
    /// the place of the bottom one and closes what was stacked on it.
    /// </summary>
    internal static IDisposable Show(TimbnPage page, ManualLogSource logger, Action? closed = null, bool overOthers = false)
    {
        var depth = overOthers ? _windows.FindLastIndex(open => open != null && open._page != null) + 1 : 0;
        var window = depth < _windows.Count && _windows[depth] != null ? _windows[depth] : Create(depth);
        if (window == null)
        {
            logger.LogWarning($"The page '{page.Title}' cannot open yet, since the game has not made the window it copies.");
            return TimbnUndo.None;
        }

        // A page already open is replaced, and its handle then does nothing.
        for (var above = _windows.Count - 1; above >= depth; above--)
            _windows[above]?.CloseNow();

        window._page = page;
        window._onClosed = closed;
        window._logger = logger;
        window.Open(null!);
        return new TimbnUndo(() =>
        {
            if (window._page == page)
                window.CloseNow();
        });
    }

    public override void Open(LazyWidgetDataBase data)
    {
        var page = _page!;
        _lastFocus = null;
        _title!.text = page.Title;
        _pause = page.PauseGame ? TimbnCorePlugin.Instance!.Clock.Freeze() : null;

        base.Open(data);
        var area = NewArea(!page.Height.IsAuto);
        // A close kept by the page and called later only closes this page, not one that took its place in the window.
        void Close()
        {
            if (_page == page)
                CloseNow();
        }

        TimbnSafe.Run(() => page.Mount(area, Close, _logger), _logger, $"{nameof(TimbnPageWindow)}|Building the page '{page.Title}'");
        ShowActions(area);
        TimbnWindowFrame.CloseButton(this).gameObject.SetActive(page.AcceptsBackButton);
        ApplySize(page);
        _placeAgain = true;
        _triggerSteps = page.StepWithTriggers ?? TimbnPageNavigation.AnyStepperSharesLine(this);
        PrintTips();

        GamepadNavigationController.ReinitItems(focusOnFirstActive: false);
        if (!LazyInput.IsGamepadActive)
            return;

        if (StartItem() is { } start)
            GamepadNavigationController.SetFocusedItem(start);
        else
            GamepadNavigationController.FocusOnFirstActive(0);
    }

    // Esc, B, the close button and the pause menu's path all come here, so each one is the player backing out. A page with
    // a Secondary button lets that button answer, and a page that must be answered ignores it.
    public override void Close()
    {
        var page = _page;
        if (page == null || !page.AcceptsBackButton)
            return;

        // Esc or B while a key row waits for a key leaves the row as it was and keeps the page.
        if (TimbnKeyCapture.CancelAny())
            return;

        // A Secondary button answers Back while the player can see it, and a dimmed one keeps the page open. One that is
        // hidden, or gone with a region that built again, leaves Back to the page.
        if (ActionFor(TimbnFaceButton.Secondary) is { } secondary)
        {
            RunAction(secondary);
            return;
        }

        TimbnSafe.Run(() => page.RequestDismiss(() =>
        {
            if (_page != page)
                return;

            CloseNow();
            TimbnSafe.Run(page.Dismissed, _logger, $"{nameof(TimbnPageWindow)}|The page's dismiss handler");
        }), _logger, $"{nameof(TimbnPageWindow)}|The page's dismiss request");
    }

    protected override bool OnPressedBack()
    {
        Close();
        return true;
    }

    protected override void PrintTips()
    {
        var tips = new List<LazyGameKeyTip>();
        if (Tabs() is { ShowsBumpers: false })
        {
            tips.Add(new LazyGameKeyTip(GameKey.PrevTab, TimbnGameIds.Texts.TipPrev));
            tips.Add(new LazyGameKeyTip(GameKey.NextTab, TimbnGameIds.Texts.TipNext));
        }

        // The triggers come before Back the way the game's customization window has them, and the d-pad after it, the way
        // its settings window has it.
        if (_hasSteppers && _triggerSteps)
            TimbnPageNavigation.AddStepTips(tips, triggers: true);

        if (!_triggerSteps && List() is { ShowsHint: true })
        {
            tips.Add(new LazyGameKeyTip(GameKey.PrevSubTab, TimbnGameIds.Texts.TipPrev));
            tips.Add(new LazyGameKeyTip(GameKey.NextSubTab, TimbnGameIds.Texts.TipNext));
        }

        if (_takesSelect)
            tips.Add(LazyGameKeyTip.Select());

        // A button that goes to the bottom with a gamepad leaves its key as a hint under the window, the way the vendor and
        // battle windows do.
        foreach (var action in Actions)
        {
            if (action.Placement == TimbnGamepadPlacement.Bottom && (action.Face != TimbnFaceButton.Secondary || _page!.AcceptsBackButton) && action.Shown)
                tips.Add(new LazyGameKeyTip(action.Key, action.HoldRun != null ? $"{action.Label} ({action.HoldLabel})" : action.Label, action.Enabled, translate: false));
        }

        // A Secondary button says what Back does, so a second hint would name the same key twice.
        if (_page is { AcceptsBackButton: true } && ActionFor(TimbnFaceButton.Secondary) is null)
            tips.Add(LazyGameKeyTip.Back());

        if (_hasSteppers && !_triggerSteps)
            TimbnPageNavigation.AddStepTips(tips, triggers: false);

        lazyButtonTips.Print(tips);

        // The hint bar is one text the game keeps 200 wide, so a longer hint would wrap onto a second line.
        if (lazyButtonTips.GetComponent<TextMeshProUGUI>() is { } text)
            text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    // The tips are printed again for the new input.
    protected override void UpdateGamepadDependentStuff()
    {
        base.UpdateGamepadDependentStuff();
        if (_page == null)
            return;

        PrintTips();
    }

    protected override void OnFocusedItemChanged(GamepadNavigationItem gamepadNavigationItem)
    {
        base.OnFocusedItemChanged(gamepadNavigationItem);

        if (gamepadNavigationItem == null || !LazyInput.IsGamepadActive)
            return;

        TimbnPageNavigation.KeepInView(gamepadNavigationItem.FocusRectTransform);
        if (gamepadNavigationItem.group != 0)
            return;

        _lastFocus = gamepadNavigationItem;
        TimbnPageNavigation.Aim(this, gamepadNavigationItem);
    }

    // Puts a gamepad's focus on the item, or with a mouse in use keeps it as where the gamepad picks up, so a pick made with
    // the mouse, such as entering a mod, lands the stick where A would have.
    internal static void FocusOrResume(GamepadNavigationItem? item)
    {
        if (item == null)
            return;

        if (LazyInput.IsGamepadActive)
        {
            item.GetComponentInParent<GamepadNavigationController>()?.SetFocusedItem(item);
            return;
        }

        if (item.GetComponentInParent<TimbnPageWindow>() is { } window)
            window._lastFocus = item;
    }

    // The window's input controller asks for this when the gamepad comes back after the mouse, which moves focus to
    // whatever it passes over, so focus returns to the last control.
    protected override GamepadNavigationItem GetFocusedNavigationItem()
    {
        // The mouse may have picked another line of a list since, and the focus goes to that one.
        if (_lastFocus != null && Widgets.OfType<TimbnListWidget>().FirstOrDefault(list => _lastFocus.transform.IsChildOf(list.Root)) is { ChosenItem: { } chosen })
            return chosen;

        if (_lastFocus != null && _lastFocus.isActiveAndEnabled)
            return _lastFocus;

        return StartItem() ?? GetComponentsInChildren<GamepadNavigationItem>().FirstOrDefault(item => item.group == 0)!;
    }

    // A list that asks for it takes the first focus, such as the Mods window's list of mods.
    private GamepadNavigationItem? StartItem() =>
        Widgets.OfType<TimbnListWidget>().FirstOrDefault(list => list.FocusWhenShown && list.Root.gameObject.activeInHierarchy)?.ChosenItem;

    protected override Dictionary<GameKey, Func<bool>> GetGameKeyDelegates()
    {
        var delegates = base.GetGameKeyDelegates();

        // The stick sends Left and Right and the d-pad DpadLeft and DpadRight. Either one changes the value of a switch,
        // choice or slider, the way the game's settings do, and anywhere else moves focus as the game would.
        delegates[GameKey.Left] = () => Side(-1, GUIDirection.Left);
        delegates[GameKey.DpadLeft] = () => Side(-1, GUIDirection.Left);
        delegates[GameKey.Right] = () => Side(1, GUIDirection.Right);
        delegates[GameKey.DpadRight] = () => Side(1, GUIDirection.Right);
        delegates[GameKey.Select] = PressSelect;
        delegates[GameKey.Action] = () => PressSlot(TimbnFaceButton.PrimarySpecial);
        delegates[GameKey.SaveImport] = () => PressSlot(TimbnFaceButton.SecondarySpecial);
        delegates[GameKey.PrevTab] = () => StepTab(-1);
        delegates[GameKey.NextTab] = () => StepTab(1);
        delegates[GameKey.PrevSubTab] = () => _triggerSteps ? TimbnPageNavigation.Trigger(GamepadNavigationController, -1) : StepList(-1);
        delegates[GameKey.NextSubTab] = () => _triggerSteps ? TimbnPageNavigation.Trigger(GamepadNavigationController, 1) : StepList(1);
        delegates[GameKey.Up] = () => Move(GUIDirection.Up);
        delegates[GameKey.DpadUp] = () => Move(GUIDirection.Up);
        delegates[GameKey.Down] = () => Move(GUIDirection.Down);
        delegates[GameKey.DpadDown] = () => Move(GUIDirection.Down);
        return delegates;
    }

    protected override void TestDraw()
    {
    }

    private static TimbnPageWindow? Create(int depth)
    {
        var window = TimbnWindowFrame.Copy<TimbnPageWindow>(depth == 0 ? nameof(TimbnPageWindow) : $"{nameof(TimbnPageWindow)}{depth}");
        if (window == null)
            return null;

        window.Build();
        window.Init();
        while (_windows.Count <= depth)
            _windows.Add(null!);

        _windows[depth] = window;
        return window;
    }

    private void Build()
    {
        _header = TimbnWindowFrame.Header(transform);

        // The title is whatever page is showing, so the game's localized label is taken off it.
        var header = transform.Find(_titlePath);
        foreach (var label in header.GetComponents<LocalizedLabel>())
            DestroyImmediate(label);

        _title = header.GetComponentInChildren<TMP_Text>(true);

        var (content, old) = TimbnWindowFrame.Content(transform);
        _root = TimbnUi.Column(content, "Page");
        TimbnWindowFrame.KeepTips(transform);

        // The Controls window's list wraps from its top to its bottom, which on a page would jump from the first
        // control to the last.
        GetComponent<GamepadNavigationController>().loopVerticalNavigation = false;

        foreach (var child in old)
            DestroyImmediate(child.gameObject);
    }

    // A page closed from code, by its handle, a late close or its mod unloading, takes every page stacked on it first, the
    // topmost first, so a question or typing window is never left open over a page that is gone. The player can only
    // close the topmost one.
    private void CloseNow()
    {
        var depth = _windows.IndexOf(this);
        for (var above = _windows.Count - 1; depth >= 0 && above > depth; above--)
            _windows[above]?.CloseNow();

        var closed = _onClosed;
        var page = _page;
        _onClosed = null;
        _page = null;
        if (!IsShown)
        {
            closed?.Invoke();
            return;
        }

        _area = null;
        _watchers = null;
        _lastFocus = null;
        _heldSince.Clear();

        // The page goes with the window, so it can clean up when its objects are destroyed.
        TimbnKeyCapture.CancelAny();
        Clear(_root);
        _pause?.Dispose();
        _pause = null;
        CloseWindow();
        if (page != null)
            TimbnSafe.Run(page.Closed, _logger, $"{nameof(TimbnPageWindow)}|The page's closed handler");

        closed?.Invoke();
    }

    // Closing a window gives player control back, and the game's handler for that throws once the player is destroyed
    // (the main menu, or a quit), whether or not the game still counts as in a save. The window is already hidden by then, so
    // the throw is logged for debugging only and the rest of the close runs. TimbnSafe.Run is not used here since it logs
    // an error, and this one throw is expected on every quit, so it names the one exception type.
    private void CloseWindow()
    {
        try
        {
            base.Close();
        }
        catch (ArgumentNullException exception)
        {
            _logger?.LogDebug($"{nameof(TimbnPageWindow)}|The game's close handler found a destroyed object. {exception.Message}");
        }
    }

    // A page can change height while it is open, such as a card that shows and hides, and the window's own layout is
    // not told, so it is rebuilt here and the window grows or shrinks from its top edge.
    private void LateUpdate()
    {
        if (_page == null)
            return;

        if (_placeAgain)
        {
            _placeAgain = false;
            ApplySize(_page);
        }

        var height = LayoutUtility.GetPreferredHeight(_root);
        if (Mathf.Approximately(height, _pageHeight))
            return;

        _pageHeight = height;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform.Find("GenericWIndowLayout"));
    }

    // The page gets a fresh empty column every time it is shown, so nothing left from an earlier one is still there. It
    // takes the room the root has left when the page asked for a height, and is as tall as its content when it did not.
    // Controls here are as tall as their art, so the gap between two of them is this gap whatever they are. The pause
    // menu's buttons sit in boxes a little taller than their art, so the same distance from the frame takes a little
    // padding above and below.
    private RectTransform NewArea(bool fixedHeight)
    {
        // An edge to edge page runs to the frame's lip, so the area adds nothing under it.
        var bottom = _page is { EdgeToEdge: true } ? 0 : _bottomPadding;
        var area = TimbnUi.Column(_root, "Area", TimbnPageBuilder.PartGap, new RectOffset(0, 0, _topPadding, bottom));
        if (fixedHeight)
            TimbnUi.FillHeight(area);

        area.SetSiblingIndex(0);
        return area;
    }

    // A page that does not accept Back has nothing on B, so a Secondary button is hidden. A button that dims or undims
    // changes its hint, so the tips are printed again then.
    private void ShowActions(RectTransform area)
    {
        _area = area;
        _watchers = null;
        _actionsSeen = 0;
        _takesSelect = TimbnWidget.TakesSelect(area);
        _hasSteppers = TimbnWidget.HasStepper(area);
    }

    // A region built again, so what A does, which keys step and the hints are worked out again.
    internal void ContentChanged()
    {
        if (_page is not { } page || _area == null)
            return;

        _watchers = null;
        _takesSelect = TimbnWidget.TakesSelect(_area);
        _hasSteppers = TimbnWidget.HasStepper(_area);
        _triggerSteps = page.StepWithTriggers ?? TimbnPageNavigation.AnyStepperSharesLine(this);
        PrintTips();
    }

    // The page's controls and actions may sit in regions that build again, each with a watcher of its own. The list is kept
    // until a region builds again, since it is read every frame, and one hidden while it was read is skipped then.
    private IEnumerable<TimbnPageWatcher> Watchers =>
        _area == null ? [] : (_watchers ??= [.. _area.GetComponentsInChildren<TimbnPageWatcher>(true)]).Where(watcher => watcher != null && watcher.isActiveAndEnabled);

    private IEnumerable<TimbnPageAction> Actions => Watchers.SelectMany(watcher => watcher.Actions);

    // The action a key runs, the first one the player can see. A hidden or disabled one on the page around it, such as the
    // Mods window's own Save while a mod's tab is open, would otherwise take the key from the tab's own.
    private TimbnPageAction? ActionFor(TimbnFaceButton face) => Actions.FirstOrDefault(action => action.Face == face && action.Shown);

    private IEnumerable<TimbnWidget> Widgets => Watchers.SelectMany(watcher => watcher.Widgets);

    // The page's own tick, its size when that changed, and the hints when its actions did, such as a button that dims or a
    // region that built again.
    private void TickPage()
    {
        if (_page is not { } page)
            return;

        TimbnSafe.Run(page.Tick, _logger, $"{nameof(TimbnPageWindow)}|The page's tick");
        if (!Input.GetMouseButton(0) && !(page.Width.Equals(_appliedWidth) && page.Height.Equals(_appliedHeight)))
            ApplySize(page);

        // A number made from every action's key, label, state and placement, so the hints are only printed again on a change.
        var seen = 17;
        foreach (var action in Actions)
        {
            if (action.Face == TimbnFaceButton.Secondary && !page.AcceptsBackButton && action.Button is { } secondary)
                secondary.Visible = false;

            unchecked
            {
                seen = (seen * 31) + (int)action.Face;
                seen = (seen * 31) + action.Label.GetHashCode();
                seen = (seen * 31) + (action.Enabled ? 1 : 0);
                seen = (seen * 31) + (action.Shown ? 1 : 0);
                seen = (seen * 31) + (int)action.Placement;
            }
        }

        if (seen == _actionsSeen)
            return;

        _actionsSeen = seen;
        _takesSelect = _area != null && TimbnWidget.TakesSelect(_area);
        PrintTips();
    }

    // The game's button also polls its key while a gamepad is in use, so a press can arrive twice in a frame.
    private void RunAction(TimbnPageAction action)
    {
        if (_actionFrame.TryGetValue(action.Face, out var frame) && frame == Time.frameCount)
            return;

        _actionFrame[action.Face] = Time.frameCount;

        // A key press does not end the edit of a box with the caret the way a click does, so what was typed is taken first.
        // Back and Cancel sit on the other buttons and leave the edit alone, since Esc and B must not keep it.
        if (action.Face is TimbnFaceButton.Primary or TimbnFaceButton.PrimarySpecial)
        {
            foreach (var box in Widgets.OfType<TimbnTextBoxWidget>())
                box.CommitFocused();
        }

        if (action.Button != null)
            action.Button.Press();
        else if (action.Enabled)
            TimbnSafe.Run(action.Run, _logger, $"{nameof(TimbnPageWindow)}|A page's key action");
    }

    private bool PressSlot(TimbnFaceButton slot)
    {
        if (ActionFor(slot) is not { } action)
            return false;

        // With a hold action, which one it is is only known once the key is let go or held long enough.
        if (action.HoldRun != null)
        {
            _heldSince.TryAdd(slot, Time.unscaledTime);
            return true;
        }

        RunAction(action);
        return true;
    }

    protected override void Update()
    {
        base.Update();
        TickPage();
        if (_heldSince.Count == 0 || _area == null)
            return;

        foreach (var (face, since) in _heldSince.ToList())
        {
            var action = ActionFor(face);
            if (action?.HoldRun == null)
            {
                _heldSince.Remove(face);
                continue;
            }

            if (LazyInput.GetKey(action.Key))
            {
                if (Time.unscaledTime - since < _holdSeconds)
                    continue;

                _heldSince.Remove(face);
                if (action.Enabled)
                    TimbnSafe.Run(action.HoldRun, _logger, $"{nameof(TimbnPageWindow)}|A page's hold action");

                continue;
            }

            _heldSince.Remove(face);
            RunAction(action);
        }
    }

    private TimbnTabsWidget? Tabs() =>
        Widgets.OfType<TimbnTabsWidget>().FirstOrDefault(tabs => tabs.Root.gameObject.activeInHierarchy);

    private TimbnListWidget? List() =>
        Widgets.OfType<TimbnListWidget>().FirstOrDefault(list => list.Root.gameObject.activeInHierarchy);

    // LT and RT step a list when the page does not use them to step its controls.
    private bool StepList(int delta)
    {
        if (List() is not { } list)
            return false;

        LazyAudio.PlayAndForget("tab_click");
        list.Cycle(delta);
        return true;
    }

    private bool StepTab(int delta)
    {
        if (Tabs() is not { } tabs)
            return false;

        // LB and RB belong to the column the tabs head, so they do nothing while the focus is in another, such as a split's list.
        if (GamepadNavigationController.FocusedItem is { } focused && TimbnNavigationZone.Of(focused) != TimbnNavigationZone.Of(tabs.Root))
            return false;

        LazyAudio.PlayAndForget("tab_click");
        tabs.Cycle(delta);
        return true;
    }

    // The share is of the screen the whole window takes. The frame and title bar around the content are the window
    // layout's padding, and the topmost canvas above the window is the screen. A page that asked for no height is as tall
    // as what it holds, with the game's own padding around it, the way the pause menu is.
    private void ApplySize(TimbnPage page)
    {
        var screen = ((RectTransform)GetComponentsInParent<Canvas>(true).Last().transform).rect;
        var layout = (RectTransform)transform.Find("GenericWIndowLayout");
        var group = layout.GetComponent<VerticalLayoutGroup>();
        var gameSides = _gamePadding ??= new RectOffset(group.padding.left, group.padding.right, group.padding.top, group.padding.bottom);
        if (page.EdgeToEdge)
        {
            TimbnWindowFrame.Tighten(transform, _header!);
            group.padding.top -= _edgeTopTrim;
        }
        else
            group.padding = new RectOffset(gameSides.left, gameSides.right, gameSides.top, gameSides.bottom);

        TimbnWindowFrame.UseCharacterBack(transform, page.EdgeToEdge, page.Backdrop);
        var padding = group.padding;
        _appliedWidth = page.Width;
        _appliedHeight = page.Height;
        var size = TimbnPageSize.Resolve(page.Width, page.Height, screen.size, new Vector2(padding.horizontal, padding.vertical));
        var element = TimbnUi.Element(_root);
        TimbnUi.Fixed(_root, size.x);
        if (size.y > 0f)
        {
            TimbnUi.Fixed(_root, height: size.y);
        }
        else
        {
            element.minHeight = element.preferredHeight = -1f;
            element.flexibleHeight = 0f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
        _pageHeight = LayoutUtility.GetPreferredHeight(_root);

        // The game sets this window a little above the middle, which would push a tall one off the top of the screen.
        _raise ??= layout.anchoredPosition.y;

        // A fixed height is known now, and the layout may not have grown to it yet.
        var total = size.y > 0f ? size.y + padding.vertical : LayoutUtility.GetPreferredHeight(layout);
        var center = Mathf.Min(_raise.Value, Mathf.Max(0f, (screen.height - total) / 2f));

        // The window hangs from its top edge, so a page whose height changes while it is open (a card that shows and hides)
        // moves only the bottom and the title bar stays where it is.
        layout.pivot = new Vector2(layout.pivot.x, 1f);
        layout.anchoredPosition = new Vector2(layout.anchoredPosition.x, center + total / 2f);
    }

    private bool Side(int delta, GUIDirection direction) =>
        TimbnPageNavigation.Side(this, GamepadNavigationController, delta, direction, sidesStep: !_triggerSteps);

    private bool Move(GUIDirection direction) => TimbnPageNavigation.Move(this, GamepadNavigationController, direction);

    // Space is the keyboard's A. A page with a button, text box or key row leaves it to them, and any other page runs its
    // Primary button.
    private bool PressSelect()
    {
        // A button the page placed itself presses on A when nothing else has the focus, and the focused control otherwise.
        var primary = ActionFor(TimbnFaceButton.Primary);
        var focused = GamepadNavigationController.FocusedItem;
        var bound = primary?.Button is { Root: { } root } && (focused == null || focused.transform.IsChildOf(root));
        if (bound || (!_takesSelect && PressSlot(TimbnFaceButton.Primary)))
        {
            if (bound)
                PressSlot(TimbnFaceButton.Primary);

            return true;
        }

        GamepadNavigationController.SelectFocusedItem();
        return true;
    }

    private static void Clear(Transform parent)
    {
        // Destroy waits for the end of the frame, and the gamepad's list of controls is read again in the same frame, so the
        // old ones are hidden first to keep them out of it.
        foreach (Transform child in parent)
        {
            if (child.name != "Area")
                continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}
