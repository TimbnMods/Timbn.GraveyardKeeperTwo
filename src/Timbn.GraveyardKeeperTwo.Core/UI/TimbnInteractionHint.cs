using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's "press a key to do something" hint, drawn over a spot in the world the way it is drawn over a chest
/// or a door, for an interaction the game itself does not know about. Get one from the plugin's UI.CreateHint, move
/// it with Show as often as you like, and it is hidden when the plugin unloads or the player returns to the main
/// menu. It shows one key, or two on two rows the way a cargo lift shows Open and Put. While it shows, its keys
/// belong to the hint the way they do for the game's own hints, so the game ignores those presses and does not, for
/// example, drop what the player is carrying. Dispose a hint the plugin is done with, such as one made per visitor.
/// </summary>
/// <example>
/// <code>
/// _hint = UI.CreateHint();
/// // every frame the player stands by the fire
/// _hint.Show(fire + Vector3.up * 1.2f, LLBase.L("timbn_campfire_meditate"));
/// // by a keg, with two keys
/// _hint.Show(keg, LLBase.L("timbn_bar_open"), GameKey.Interaction, LLBase.L("timbn_bar_pour"), GameKey.Action);
/// // once they walk away
/// _hint.Hide();
/// </code>
/// </example>
public sealed class TimbnInteractionHint : IBubbleDrawable, IDisposable
{
    private static readonly List<TimbnInteractionHint> _shownHints = [];

    private readonly SGuid _id = new(Guid.NewGuid());
    private readonly List<(GameKey Key, string Label)> _rows = [];
    private Vector3 _position;
    private bool _shown;

    internal TimbnInteractionHint()
    {
    }

    internal IDisposable? Owned { get; set; }

    /// <summary>Whether the hint is on screen.</summary>
    public bool IsVisible => _shown;

    SGuid IBubbleDrawable.BubbleDrawableUniqueId => _id;

    List<LazyWidgetDataBase> IBubbleDrawable.BubbleDrawableWidgets =>
        _shown
            ? [new UIInteractionHintWidgetData(_rows.Select(row => new UIInteractionHintRowWidgetData(new InteractionInfo(row.Label))).ToList())]
            : [];

    Vector3 IBubbleDrawable.BubbleDrawablePosition => _position;

    /// <summary>
    /// Shows the hint, or moves and rewords it if it is already showing. Calling it every frame with the same
    /// values costs nothing.
    /// </summary>
    /// <param name="position">The spot in the world to draw it over.</param>
    /// <param name="text">What the key does, already in the player's language, such as "Meditate".</param>
    public void Show(Vector3 position, string text) => Show(position, text, GameKey.Interaction);

    /// <summary>Shows the hint like <see cref="Show(Vector3, string)"/>, with the icon of a key other than Interaction in front of the text.</summary>
    /// <param name="position">The spot in the world to draw it over.</param>
    /// <param name="text">What the key does, already in the player's language, such as "Meditate".</param>
    /// <param name="key">The key whose icon goes in front of the text, such as GameKey.Interaction.</param>
    public void Show(Vector3 position, string text, GameKey key) => Show(position, [(key, text)]);

    /// <summary>
    /// Shows the hint with two keys, one on each row, or moves and rewords it if it is already showing. Calling it
    /// every frame with the same values costs nothing.
    /// </summary>
    /// <param name="position">The spot in the world to draw it over.</param>
    /// <param name="text">What the first key does, already in the player's language, such as "Open".</param>
    /// <param name="key">The first key, such as GameKey.Interaction.</param>
    /// <param name="secondText">What the second key does, such as "Pour Beer".</param>
    /// <param name="secondKey">The second key, such as GameKey.Action.</param>
    public void Show(Vector3 position, string text, GameKey key, string secondText, GameKey secondKey) =>
        Show(position, [(key, text), (secondKey, secondText)]);

    private void Show(Vector3 position, (GameKey Key, string Text)[] rows)
    {
        var manager = UIObjectBubbleManager.Instance;
        if (manager == null)
            return;

        var labels = rows.Select(row => (row.Key, ControllerIconLibrary.GetIconId(row.Key) + row.Text)).ToList();
        if (_shown && position == _position && labels.SequenceEqual(_rows) && manager.TryGetDisplayedBubble(_id, out _))
            return;

        _position = position;
        _rows.Clear();
        _rows.AddRange(labels);
        if (!_shown)
            _shownHints.Add(this);

        _shown = true;
        manager.Display(this);
    }

    internal static bool IsKeyClaimed() => _shownHints.Any(hint => hint._rows.Any(row => LazyInput.GetKeyDown(row.Key)));

    /// <summary>Takes the hint off the screen. It does nothing when the hint is not showing.</summary>
    public void Hide()
    {
        if (!_shown)
            return;

        _shown = false;
        _shownHints.Remove(this);
        var manager = UIObjectBubbleManager.Instance;
        if (manager != null)
            manager.Hide(this);
    }

    /// <summary>Hides the hint and lets the plugin forget it. It can still be shown again afterwards, but nothing hides it for the plugin any more.</summary>
    public void Dispose()
    {
        Hide();
        var owned = Owned;
        Owned = null;
        owned?.Dispose();
    }
}
