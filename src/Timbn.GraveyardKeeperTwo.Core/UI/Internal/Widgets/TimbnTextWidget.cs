using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnTextWidget : TimbnWidget
{
    public TimbnTextWidget(TextMeshProUGUI text)
        : base(text.rectTransform, text)
    {
    }

    public TimbnTextWidget(RectTransform root, TextMeshProUGUI text)
        : base(root, text)
    {
    }
}
