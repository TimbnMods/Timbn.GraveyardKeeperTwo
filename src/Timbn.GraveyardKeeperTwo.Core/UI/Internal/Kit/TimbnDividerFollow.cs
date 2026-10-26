namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Keeps the divider of a split header running from the rails down to the bottom of the columns as the layout settles.</summary>
internal sealed class TimbnDividerFollow : MonoBehaviour
{
    public RectTransform? Columns { get; set; }

    public float Below { get; set; }

    public Canvas? Sorting { get; set; }

    private void LateUpdate()
    {
        var rect = (RectTransform)transform;
        if (Sorting != null && rect.parent.GetComponentInParent<Canvas>(true) is { } window)
        {
            Sorting.overrideSorting = true;
            Sorting.sortingOrder = window.sortingOrder + 1;
        }

        if (Columns == null)
            return;

        var scale = rect.lossyScale.y;
        if (scale <= 0f)
            return;

        var corners = new Vector3[4];
        Columns.GetWorldCorners(corners);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, (rect.position.y - corners[0].y) / scale + Below);
    }
}
