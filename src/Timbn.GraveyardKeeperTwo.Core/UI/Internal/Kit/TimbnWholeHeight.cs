using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gives wrapped text a height in whole units. Its own height is the font's line height times the lines, which is a hair
/// over a whole number (12.01), and a list of rows adds the hair up until one row's text lands a pixel higher or lower
/// than its neighbors'.
/// </summary>
internal sealed class TimbnWholeHeight : UIBehaviour, ILayoutElement
{
    private TMP_Text? _text;

    public float minWidth => -1f;

    public float preferredWidth => -1f;

    public float flexibleWidth => -1f;

    public float minHeight => Whole;

    public float preferredHeight => Whole;

    public float flexibleHeight => -1f;

    public int layoutPriority => 1;

    private float Whole
    {
        get
        {
            _text ??= GetComponent<TMP_Text>();
            return Mathf.Round(_text.preferredHeight);
        }
    }

    public void CalculateLayoutInputHorizontal()
    {
    }

    public void CalculateLayoutInputVertical()
    {
    }
}
