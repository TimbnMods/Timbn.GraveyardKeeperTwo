using LazyBearTechnology;
using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One control of the Mods window, such as a setting's row or a button. It can be grayed out, rereads its value on
/// Refresh, and layouts place it by its Root.
/// </summary>
internal abstract class TimbnWidget
{
    private bool _interactable = true;
    private CanvasGroup? _group;

    protected TimbnWidget(RectTransform root, TextMeshProUGUI? label)
    {
        Root = root;
        Label = label;
    }

    public RectTransform Root { get; }

    public TextMeshProUGUI? Label { get; }

    /// <summary>The name a typing prompt opened from this control carries, when the control has no name of its own.</summary>
    internal string PromptTitle { get; set; } = string.Empty;

    // A component dims and blocks the control itself, so this only gates what the control does.
    internal bool ManagedByComponent { get; set; }

    // The game's dialog logs an error for an empty header, which a text box with no label would otherwise give.
    protected string TitleText => PromptTitle.Length > 0 ? PromptTitle : string.IsNullOrEmpty(Label?.text) ? TimbnUiText.PromptFallbackTitle : Label.text;

    public bool Interactable
    {
        get => _interactable && !BlockedByAParent;
        set
        {
            _interactable = value;
            if (ManagedByComponent)
                return;

            _group ??= Root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = value ? 1f : 0.5f;
            _group.interactable = value;
        }
    }

    // A disabled row or card turns its controls off through a group on itself, which the control's own flag does not know
    // about, so a click is stopped but a gamepad press would not be.
    private bool BlockedByAParent
    {
        get
        {
            foreach (var group in Root.GetComponentsInParent<CanvasGroup>(false))
            {
                if (!group.interactable)
                    return true;

                if (group.ignoreParentGroups)
                    break;
            }

            return false;
        }
    }

    public virtual void Refresh()
    {
    }

    /// <summary>
    /// Makes the widget's root a stop for the gamepad. The window steps it by the given amount when left or right is
    /// pressed on it, and the select key runs the given action.
    /// </summary>
    protected GamepadNavigationItem GamepadStop(Action<int>? step = null, Action? select = null)
    {
        var item = Root.gameObject.AddComponent<GamepadNavigationItem>();
        if (select != null)
        {
            Root.gameObject.AddComponent<SelectTaker>();
            item.OnSelect.AddListener(() =>
            {
                if (Interactable)
                    select();
            });
        }

        if (step != null)
        {
            var stepper = Root.gameObject.AddComponent<Stepper>();
            stepper.Step = delta =>
            {
                if (Interactable)
                    step(delta);
            };
        }

        return item;
    }

    /// <summary>Whether anything under the box uses A (a button, text box or key row), which decides the key a page confirms with.</summary>
    internal static bool TakesSelect(Component box) => box.GetComponentInChildren<SelectTaker>(true) != null;

    /// <summary>Whether a gamepad stop belongs to a control that left and right step, such as a switch or a slider.</summary>
    internal static bool CanStep(GamepadNavigationItem item) => item.GetComponent<Stepper>() is { Step: not null };

    /// <summary>Whether anything under the box is a control that left and right step.</summary>
    internal static bool HasStepper(Component box) => box.GetComponentInChildren<Stepper>(true) != null;

    /// <summary>Takes the mark off a button whose key the page tied to an action, since A runs that action and the button is hidden with a gamepad.</summary>
    internal static void UnmarkSelect(Component root)
    {
        foreach (var taker in root.GetComponentsInChildren<SelectTaker>(true))
            UnityEngine.Object.DestroyImmediate(taker);
    }

    /// <summary>Marks a button's own stop as one that uses A.</summary>
    internal static void MarkSelect(Component root) => root.gameObject.AddComponent<SelectTaker>();

    /// <summary>Steps the control a gamepad stop belongs to, and says whether it is one that steps.</summary>
    internal static bool TryStep(GamepadNavigationItem item, int delta)
    {
        if (item.GetComponent<Stepper>() is not { Step: { } step })
            return false;

        step(delta);
        return true;
    }

    private sealed class SelectTaker : MonoBehaviour
    {
    }

    private sealed class Stepper : MonoBehaviour
    {
        public Action<int>? Step { get; set; }
    }
}
