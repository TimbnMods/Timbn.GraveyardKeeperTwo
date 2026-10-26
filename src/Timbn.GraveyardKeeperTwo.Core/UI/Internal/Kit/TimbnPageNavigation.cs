using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gamepad movement for a window that hosts kit controls, shared by every window that does (the Mods window and the page
/// window). The game moves between the middles of controls, which on a page of columns and cards jumps to whatever wide
/// row or centered tab bar lines up best, so each move from the control in focus is set from the boxes the controls take.
/// </summary>
internal static class TimbnPageNavigation
{
    private static readonly GUIDirection[] _directions = [GUIDirection.Up, GUIDirection.Down, GUIDirection.Left, GUIDirection.Right];
    private static readonly System.Reflection.FieldInfo _selectableItems =
        AccessTools.Field(typeof(GamepadNavigationController), "selectableItems");

    // Whether a switch, choice or slider has another control to its left or right, the way the gamepad would find one,
    // such as in one of two columns. Left and right have to move between those, so a page like that steps its values
    // with the triggers.
    internal static bool AnyStepperSharesLine(Component window)
    {
        Canvas.ForceUpdateCanvases();
        var items = window.GetComponentsInChildren<GamepadNavigationItem>().Where(item => item.group == 0 && item.Active).ToList();
        foreach (var stepper in items.Where(TimbnWidget.CanStep))
        {
            var others = items
                .Where(other => other != stepper && !other.transform.IsChildOf(stepper.transform) && !stepper.transform.IsChildOf(other.transform) && TimbnNavigationZone.Of(other) == TimbnNavigationZone.Of(stepper))
                .Select(other => WorldBox((RectTransform)other.transform))
                .ToList();
            var box = WorldBox((RectTransform)stepper.transform);
            if (TimbnNavigationPicker.Pick(box, others, GUIDirection.Left) >= 0 || TimbnNavigationPicker.Pick(box, others, GUIDirection.Right) >= 0)
                return true;
        }

        return false;
    }

    // Left and right change the control in focus when it is a switch, choice or slider, unless the page steps with the
    // triggers, and move focus anywhere else.
    internal static bool Side(Component window, GamepadNavigationController controller, int delta, GUIDirection direction, bool sidesStep)
    {
        if (!LazyInput.IsGamepadActive)
            return false;

        var focused = controller.FocusedItem;
        if (focused == null || !sidesStep || !TimbnWidget.TryStep(focused, delta))
            Move(window, controller, direction);

        return true;
    }

    internal static bool Trigger(GamepadNavigationController controller, int delta) =>
        LazyInput.IsGamepadActive && controller.FocusedItem is { } focused && TimbnWidget.TryStep(focused, delta);

    // The game's settings windows show the d-pad as minus and plus the whole time, and a window that steps with the
    // triggers, like the character customization, shows them as Previous and Next.
    internal static void AddStepTips(List<LazyGameKeyTip> tips, bool triggers)
    {
        if (triggers)
        {
            tips.Add(new LazyGameKeyTip(GameKey.PrevSubTab, TimbnGameIds.Texts.TipPrev));
            tips.Add(new LazyGameKeyTip(GameKey.NextSubTab, TimbnGameIds.Texts.TipNext));
            return;
        }

        tips.Add(new LazyGameKeyTip(GameKey.DpadLeft, "-", translate: false));
        tips.Add(new LazyGameKeyTip(GameKey.DpadRight, "+", translate: false));
    }

    // Each move from the control in focus is set from the boxes the controls take, and a direction with nothing in it
    // stays put rather than falling back to the game's pick. A move never leaves the focused control's zone.
    internal static void Aim(Component window, GamepadNavigationItem focused)
    {
        Canvas.ForceUpdateCanvases();
        var zone = TimbnNavigationZone.Of(focused);
        var items = window.GetComponentsInChildren<GamepadNavigationItem>()
            .Where(item => item != focused && item.group == 0 && item.Active && TimbnNavigationZone.Of(item) == zone)
            .ToList();
        var boxes = items.Select(item => WorldBox((RectTransform)item.transform)).ToList();
        var from = WorldBox((RectTransform)focused.transform);
        foreach (var direction in _directions)
        {
            var index = TimbnNavigationPicker.Pick(from, boxes, direction);
            focused.SetCustomDirectionItem(direction, index < 0 ? focused : items[index]);
        }
    }

    // A page can show controls after it was built, such as a panel behind its own tabs, and the game only moves to
    // controls it listed when the page was drawn. So the list is brought up to date and the moves aimed again first.
    internal static bool Move(Component window, GamepadNavigationController controller, GUIDirection direction)
    {
        if (!LazyInput.IsGamepadActive)
            return false;

        var focused = controller.FocusedItem;
        var shown = window.GetComponentsInChildren<GamepadNavigationItem>();
        var listed = (List<GamepadNavigationItem>)_selectableItems.GetValue(controller);
        if (shown.Length != listed.Count || shown.Any(item => !listed.Contains(item)))
            controller.ReinitItems(false, skipUnfocusItem: focused);

        if (focused != null && focused.group == 0)
            Aim(window, focused);

        controller.Navigate(direction);
        return true;
    }

    // A control in a scroll area is placed as if the area were scrolled to its top, so one scrolled out above the view
    // does not count as above the window's first control.
    internal static Rect WorldBox(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var box = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        if (rect.GetComponentInParent<ScrollRect>() is not { content: { } content, viewport: { } viewport })
            return box;

        content.GetWorldCorners(corners);
        var contentTop = corners[1].y;
        viewport.GetWorldCorners(corners);
        box.y += corners[1].y - contentTop;
        return box;
    }

    // Scrolls the list or page a control is in just far enough to show all of it.
    internal static void KeepInView(RectTransform target)
    {
        if (target.GetComponentInParent<ScrollRect>() is not { } scroll || scroll.viewport is not { } viewport)
            return;

        Canvas.ForceUpdateCanvases();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
        var view = viewport.rect;
        var shift = bounds.max.y > view.yMax ? bounds.max.y - view.yMax : bounds.min.y < view.yMin ? bounds.min.y - view.yMin : 0f;
        if (shift == 0f)
            return;

        scroll.StopMovement();

        // The first or last control scrolls the whole way, so the section plate above it or the text under it shows too.
        if (!HasControlBeyond(scroll, target, above: shift > 0f))
        {
            scroll.verticalNormalizedPosition = shift > 0f ? 1f : 0f;
            return;
        }

        scroll.content.anchoredPosition -= new Vector2(0f, shift);
    }

    private static bool HasControlBeyond(ScrollRect scroll, RectTransform target, bool above)
    {
        var content = scroll.content;
        var edge = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);
        foreach (var item in content.GetComponentsInChildren<GamepadNavigationItem>())
        {
            if (!item.Active || item.transform == target || item.transform.IsChildOf(target) || target.IsChildOf(item.transform))
                continue;

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, item.transform);
            if (above ? bounds.max.y > edge.max.y : bounds.min.y < edge.min.y)
                return true;
        }

        return false;
    }
}
