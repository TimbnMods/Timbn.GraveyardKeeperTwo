using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's "press a key to do something" hint, drawn over a spot in the world the way it is drawn over a chest
/// or a door, for an interaction the game itself does not know about. Get one from the plugin's UI.CreateHint, move
/// it with Show as often as you like, and it is hidden when the plugin unloads or the player returns to the main
/// menu.
/// </summary>
/// <example>
/// <code>
/// _hint = UI.CreateHint();
/// // every frame the player stands by the fire
/// _hint.Show(fire + Vector3.up * 1.2f, LLBase.L("timbn_campfire_meditate"));
/// // once they walk away
/// _hint.Hide();
/// </code>
/// </example>
public sealed class TimbnInteractionHint : IBubbleDrawable
{
    private readonly SGuid _id = new(Guid.NewGuid());
    private Vector3 _position;
    private string _text = "";
    private bool _shown;

    internal TimbnInteractionHint()
    {
    }

    /// <summary>Whether the hint is on screen.</summary>
    public bool IsShown => _shown;

    SGuid IBubbleDrawable.BubbleDrawableUniqueId => _id;

    List<LazyWidgetDataBase> IBubbleDrawable.BubbleDrawableWidgets =>
        _shown ? [new UIInteractionHintWidgetData(new UIInteractionHintRowWidgetData(new InteractionInfo(_text)))] : [];

    Vector3 IBubbleDrawable.BubbleDrawablePosition => _position;

    /// <summary>
    /// Shows the hint, or moves and rewords it if it is already showing. Calling it every frame with the same
    /// values costs nothing.
    /// </summary>
    /// <param name="position">The spot in the world to draw it over.</param>
    /// <param name="text">What the key does, already in the player's language, such as "Meditate".</param>
    public void Show(Vector3 position, string text) => Show(position, text, GameKey.Interaction);

    /// <inheritdoc cref="Show(Vector3, string)"/>
    /// <param name="position">The spot in the world to draw it over.</param>
    /// <param name="text">What the key does, already in the player's language, such as "Meditate".</param>
    /// <param name="key">The key whose icon goes in front of the text, such as GameKey.Interaction.</param>
    public void Show(Vector3 position, string text, GameKey key)
    {
        var manager = UIObjectBubbleManager.Instance;
        if (manager == null)
            return;

        var label = ControllerIconLibrary.GetIconId(key) + text;
        if (_shown && label == _text && position == _position && manager.TryGetDisplayedBubble(_id, out _))
            return;

        _position = position;
        _text = label;
        _shown = true;
        manager.Display(this);
    }

    /// <summary>Takes the hint off the screen. It does nothing when the hint is not showing.</summary>
    public void Hide()
    {
        if (!_shown)
            return;

        _shown = false;
        var manager = UIObjectBubbleManager.Instance;
        if (manager != null)
            manager.Hide(this);
    }
}
