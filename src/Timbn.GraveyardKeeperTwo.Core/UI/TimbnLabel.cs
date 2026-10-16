using LazyBearTechnology;
using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A short line of text floating over a spot in the world, drawn the way the game draws a grave's quality label, in
/// the game's font with no frame. Use it to name something, such as a chest or a stash. The text can start with a font
/// icon (one of the game's sprite names, such as "gear"), or have none. Get one from the plugin's UI.CreateLabel, show
/// it with a position or a function that returns the position each frame, and it is hidden when the plugin unloads or
/// the player returns to the main menu. Dispose a label the plugin is done with, such as one made per chest.
/// </summary>
/// <example>
/// <code>
/// _label = UI.CreateLabel();
/// // follows the chest, lifted a little while the player stands at it
/// _label.Show(() => chest.Position + Vector3.up * lift, "Relics");
/// // when the label is removed
/// _label.Hide();
/// </code>
/// </example>
public sealed class TimbnLabel : IBubbleDrawable, IDisposable
{
    private static readonly AccessTools.FieldRef<UIQualityTooltipWidget, TextMeshProUGUI>? _iconLabel =
        TimbnReflection.TryField<UIQualityTooltipWidget, TextMeshProUGUI>("iconLabel");

    private readonly SGuid _id = new(Guid.NewGuid());
    private Func<Vector3>? _follow;
    private Vector3 _position;
    private string _text = "";
    private string _icon = "";
    private bool _shown;

    internal TimbnLabel()
    {
    }

    internal IDisposable? Owned { get; set; }

    /// <summary>Whether the label is on screen.</summary>
    public bool IsVisible => _shown;

    SGuid IBubbleDrawable.BubbleDrawableUniqueId => _id;

    List<LazyWidgetDataBase> IBubbleDrawable.BubbleDrawableWidgets => _shown ? [new UIQualityTooltipWidgetData(_icon, _text)] : [];

    Vector3 IBubbleDrawable.BubbleDrawablePosition
    {
        get
        {
            if (_follow != null)
            {
                try
                {
                    _position = _follow();
                }
                catch (Exception)
                {
                    // The thing it followed was destroyed, so the label stays where it was until it is hidden.
                }
            }

            return _position;
        }
    }

    /// <summary>
    /// Shows the label over a fixed spot, or changes its text if it is already showing. Calling it every frame with
    /// the same values costs nothing.
    /// </summary>
    /// <param name="position">The spot in the world to draw it over.</param>
    /// <param name="text">The text, which may use rich text such as color tags.</param>
    /// <param name="icon">The game's font icon to draw before the text, such as "gear". Leave it out for text alone.</param>
    public void Show(Vector3 position, string text, string icon = "")
    {
        _follow = null;
        _position = position;
        Display(text, icon);
    }

    /// <summary>
    /// Shows the label over a spot that can move, or changes its text if it is already showing. The function is read
    /// every frame the label is on screen, so it can lift the label while something happens or follow a character.
    /// </summary>
    /// <param name="position">Returns the spot in the world to draw it over. If it throws, the label stays where it last was.</param>
    /// <param name="text">The text, which may use rich text such as color tags.</param>
    /// <param name="icon">The game's font icon to draw before the text, such as "gear". Leave it out for text alone.</param>
    public void Show(Func<Vector3> position, string text, string icon = "")
    {
        _follow = position;
        Display(text, icon);
    }

    /// <summary>Takes the label off the screen. It does nothing when the label is not showing.</summary>
    public void Hide()
    {
        if (!_shown)
            return;

        _shown = false;
        if (UIObjectBubbleManager.Instance is { } manager)
            manager.Hide(this);
    }

    /// <summary>Hides the label and lets the plugin forget it. It can still be shown again afterwards, but nothing hides it for the plugin any more.</summary>
    public void Dispose()
    {
        Hide();
        var owned = Owned;
        Owned = null;
        owned?.Dispose();
    }

    // Runs inside the widget's Draw, because the bubble freezes the widget's layout right after drawing it.
    internal static void ShowIcon(UIQualityTooltipWidget widget, UIQualityTooltipWidgetData data)
    {
        if (_iconLabel?.Invoke(widget) is { } icon)
            icon.gameObject.SetActive(!string.IsNullOrEmpty(data.IconName));
    }

    private void Display(string text, string icon)
    {
        var manager = UIObjectBubbleManager.Instance;
        if (manager == null)
            return;

        if (_shown && text == _text && icon == _icon && manager.TryGetDisplayedBubble(_id, out _))
            return;

        _text = text;
        _icon = icon;
        _shown = true;
        manager.Display(this);
    }
}
