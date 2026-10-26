using LazyBearTechnology;
using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnNavigationPickerTests
{
    // A page 300 wide with y going up, like the window's world space.
    private static readonly Rect _header = Rect.MinMaxRect(0, 200, 300, 220);
    private static readonly Rect _leftColumn = Rect.MinMaxRect(0, 150, 100, 170);
    private static readonly Rect _rightTop = Rect.MinMaxRect(110, 170, 300, 190);
    private static readonly Rect _rightBottom = Rect.MinMaxRect(110, 140, 300, 160);
    private static readonly Rect _cardOne = Rect.MinMaxRect(60, 100, 95, 120);
    private static readonly Rect _cardTwo = Rect.MinMaxRect(160, 100, 195, 120);
    private static readonly Rect _cardThree = Rect.MinMaxRect(260, 100, 295, 120);
    private static readonly Rect _tabs = Rect.MinMaxRect(0, 60, 300, 80);
    private static readonly Rect _back = Rect.MinMaxRect(8, 20, 60, 40);
    private static readonly Rect _save = Rect.MinMaxRect(240, 20, 292, 40);

    private static readonly Rect[] _page = [_header, _leftColumn, _rightTop, _rightBottom, _cardOne, _cardTwo, _cardThree, _tabs, _back, _save];

    private static Rect Pick(Rect from, GUIDirection direction)
    {
        var others = _page.Where(box => box != from).ToList();
        var index = TimbnNavigationPicker.Pick(from, others, direction);
        return index < 0 ? default : others[index];
    }

    [Fact]
    public void DownFromAWideRowGoesToTheLeftOfTheNextLine() =>
        Assert.Equal(_leftColumn, Pick(_header, GUIDirection.Down));

    [Fact]
    public void DownIntoARowOfCardsPicksTheNearestAcross() =>
        Assert.Equal(_cardTwo, Pick(_rightBottom, GUIDirection.Down));

    [Fact]
    public void DownPastTheCardsReachesTheTabsNotFurther() =>
        Assert.Equal(_tabs, Pick(_cardThree, GUIDirection.Down));

    [Fact]
    public void DownFromABarGoesToATallControlBeforeAButtonTouchingItsBottom()
    {
        var bar = Rect.MinMaxRect(0, 300, 300, 320);
        var tall = Rect.MinMaxRect(240, 200, 300, 280);
        var button = Rect.MinMaxRect(100, 180, 300, 200);

        Assert.Equal(1, TimbnNavigationPicker.Pick(bar, [button, tall], GUIDirection.Down));
    }
}
