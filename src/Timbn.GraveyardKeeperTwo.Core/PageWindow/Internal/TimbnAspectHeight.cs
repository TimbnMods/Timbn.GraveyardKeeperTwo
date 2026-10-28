using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gives a picture the height its shape needs at the width its box has. Unity sets every width before it asks for heights,
/// so the width is known when the layout asks, the same way a line of wrapped text gets its height. It outranks the
/// image's own size, which is the sprite's native height, and says nothing about width.
/// </summary>
internal sealed class TimbnAspectHeight : MonoBehaviour, ILayoutElement
{
    private float _height;

    internal Image? Source { get; set; }

    public float minWidth => -1f;

    public float preferredWidth => -1f;

    public float flexibleWidth => -1f;

    public float minHeight => _height;

    public float preferredHeight => _height;

    // Nothing here, so a fill weight on the box still counts.
    public float flexibleHeight => -1f;

    public int layoutPriority => 2;

    public void CalculateLayoutInputHorizontal()
    {
    }

    public void CalculateLayoutInputVertical()
    {
        var sprite = Source != null ? Source.sprite : null;
        _height = sprite == null ? 0f : ((RectTransform)transform).rect.width * sprite.rect.height / sprite.rect.width;
    }
}
