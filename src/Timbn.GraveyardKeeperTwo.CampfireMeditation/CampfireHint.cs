using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

internal sealed class CampfireHint : IBubbleDrawable
{
    private const float _heightAboveFire = 1.2f;

    private readonly SGuid _id = new(Guid.NewGuid());
    private Vector3 _position;
    private string _text = "";
    private bool _shown;

    public SGuid BubbleDrawableUniqueId => _id;

    public List<LazyWidgetDataBase> BubbleDrawableWidgets =>
        _shown ? [new UIInteractionHintWidgetData(new UIInteractionHintRowWidgetData(new InteractionInfo(_text)))] : [];

    public Vector3 BubbleDrawablePosition => _position;

    public void Show(Vector3 fire, string text)
    {
        var manager = UIObjectBubbleManager.Instance;
        if (manager == null)
            return;

        var position = fire + Vector3.up * _heightAboveFire;
        var label = ControllerIconLibrary.GetIconId(GameKey.Interaction) + text;
        if (_shown && label == _text && position == _position && manager.TryGetDisplayedBubble(_id, out _))
            return;

        _position = position;
        _text = label;
        _shown = true;
        manager.Display(this);
    }

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
