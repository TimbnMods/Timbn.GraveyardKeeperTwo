namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gives a row the width its content needs as the content grows, since a label's width is only known once it is drawn. The
/// row never gets narrower again.
/// </summary>
internal sealed class TimbnKeepsRoom : MonoBehaviour
{
    private void LateUpdate()
    {
        var rect = (RectTransform)transform;
        var element = TimbnUi.Element(rect);
        // The row's own layout element says zero, so what its children need is read from its layout group.
        var wanted = rect.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>()?.minWidth ?? 0f;
        if (wanted <= element.minWidth + 0.5f)
            return;

        element.minWidth = wanted;
        UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild((RectTransform)rect.parent);
    }
}
