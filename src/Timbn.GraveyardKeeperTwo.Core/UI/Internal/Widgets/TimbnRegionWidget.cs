namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Lets a page's refresh reach the controls inside a <see cref="TimbnRegion"/>.</summary>
internal sealed class TimbnRegionWidget(RectTransform box, TimbnRegion region) : TimbnWidget(box, null)
{
    public override void Refresh() => region.Refresh();
}
