using Cell = Timbn.GraveyardKeeperTwo.Core.Framework.TimbnQuestLayout.Cell;
using Quest = Timbn.GraveyardKeeperTwo.Core.Framework.TimbnQuestLayout.Quest;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnQuestLayoutTests
{
    [Fact]
    public void UnstartedModQuestsWaitOffTheTree()
    {
        var plan = TimbnQuestLayout.Arrange([Ours("first"), Ours("second")], [Game("story", 0, 0)]);

        Assert.Equal(new Cell(0, -1), plan.Cells["first"]);
        Assert.Equal(new Cell(0, -2), plan.Cells["second"]);
        Assert.Equal(new Cell(0, 0), plan.Cells["story"]);
    }

    [Fact]
    public void StartedFamiliesTakeTheTopBandAndPushTheStoryDown()
    {
        var plan = TimbnQuestLayout.Arrange([Ours("mine", started: true)], [Game("story", 2, 0), Game("later", 1, 3)]);

        Assert.Equal(new Cell(0, 0), plan.Cells["mine"]);
        Assert.Equal(new Cell(2, 2), plan.Cells["story"]);
        Assert.Equal(new Cell(1, 5), plan.Cells["later"]);
    }

    [Fact]
    public void AChainSharesOneCellAndShowsItsNewestStepWhateverTheRegistrationOrder()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Continuing("second", "first"), Ours("first", started: true)],
            [Game("story", 0, 0)]);

        Assert.Equal(plan.Cells["first"], plan.Cells["second"]);
        Assert.True(plan.Hidden["first"]);
        Assert.False(plan.Hidden["second"]);
    }

    [Fact]
    public void AQuestWithAModParentGetsItsOwnCellBelowIt()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("first", started: true), Ours("after", started: true, "first")],
            [Game("story", 3, 0)]);

        Assert.Equal(plan.Cells["first"] + new Cell(0, 1), plan.Cells["after"]);
        Assert.False(plan.Hidden["first"]);
        Assert.False(plan.Hidden["after"]);
    }

    [Fact]
    public void AQuestBranchingOffAChainHangsBelowItAndListsEveryStepAsAParent()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("first", started: true), Continuing("second", "first"), Ours("branch", started: true, "first")],
            [Game("story", 3, 0)]);

        Assert.Equal(plan.Cells["first"] + new Cell(0, 1), plan.Cells["branch"]);
        Assert.Equal(["second"], plan.ExtraParents["branch"]);
    }

    [Fact]
    public void AFamilyUnderAGameQuestGoesRightBelowItAndListsTheQuestsSharingThatCell()
    {
        var mine = Ours("mine", started: true, "anchor");

        var plan = TimbnQuestLayout.Arrange([mine], [Game("anchor", 3, 5), Game("mate", 3, 5)]);

        Assert.Equal(new Cell(3, 6), plan.Cells["mine"]);
        Assert.Equal(["mate"], plan.ExtraParents["mine"]);
        Assert.Equal(["anchor"], mine.Parents);
    }

    [Fact]
    public void AFamilyUnderAGameQuestOpensRowsWhenThereIsNoRoomBesideIt()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("mine", started: true, "anchor")],
            [
                Game("anchor", 1, 0),
                Game("below", 1, 1, "anchor"),
                Game("left", 0, 1),
                Game("right", 2, 1),
                Game("under", 0, 2),
                Game("under_right", 2, 2),
            ]);

        Assert.Equal(new Cell(2, 1), plan.Cells["mine"]);
        Assert.Equal(new Cell(1, 2), plan.Cells["below"]);
        Assert.Equal(new Cell(2, 2), plan.Cells["right"]);
    }

    [Fact]
    public void AFamilyUnderAGameQuestTakesTheNearestFreeCellBeforeOpeningRows()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("mine", started: true, "anchor")],
            [
                Game("anchor", 2, 0),
                Game("a", 0, 1),
                Game("b", 1, 1),
                Game("c", 2, 1),
                Game("d", 3, 1),
                Game("far", 4, 3),
            ]);

        Assert.Equal(new Cell(4, 1), plan.Cells["mine"]);
        Assert.Equal(new Cell(2, 1), plan.Cells["c"]);
        Assert.Equal(new Cell(4, 3), plan.Cells["far"]);
    }

    [Fact]
    public void AGameLineBlocksTheChildsColumnNotTheParents()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("mine", started: true, "anchor")],
            [Game("anchor", 0, 0), Game("child", 1, 2, "anchor")]);

        Assert.Equal(new Cell(0, 1), plan.Cells["mine"]);
    }

    [Fact]
    public void TheCellOfTheGamesHiddenQuestsStaysTaken()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("mine", started: true, "anchor")],
            [Game("anchor", 1, 0), Game("hidden", 1, 1, hasCell: false)]);

        Assert.Equal(new Cell(0, 1), plan.Cells["mine"]);
    }

    [Fact]
    public void AQuestContinuingAModQuestIsItsNextStepEvenWhenRegisteredAfterAnotherChild()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("first", started: true), Ours("side", started: true, "first"), Continuing("next", "first")],
            [Game("story", 3, 0)]);

        Assert.Equal(plan.Cells["first"], plan.Cells["next"]);
        Assert.Equal(plan.Cells["first"] + new Cell(0, 1), plan.Cells["side"]);
        Assert.False(plan.Hidden["next"]);
    }

    [Fact]
    public void AStepLeftAloneInItsChainIsShownAgain()
    {
        var plan = TimbnQuestLayout.Arrange([Ours("first", started: true), Continuing("second", "first", started: false)], [Game("story", 0, 0)]);

        Assert.False(plan.Hidden["first"]);
    }

    [Fact]
    public void QuestsThatHangUnderEachOtherStillGetTheirOwnCells()
    {
        var plan = TimbnQuestLayout.Arrange(
            [Ours("a", started: true, "b"), Ours("b", started: true, "a")],
            [Game("story", 3, 0)]);

        Assert.NotEqual(plan.Cells["a"], plan.Cells["b"]);
        Assert.True(plan.Cells["a"].Y >= 0);
        Assert.True(plan.Cells["b"].Y >= 0);
    }

    private static Quest Ours(string id, bool started = false, params string[] parents) =>
        new(id, parents, default, started: started);

    private static Quest Continuing(string id, string parent, bool started = true) =>
        new(id, [parent], default, started: started, continues: true);

    private static Quest Game(string id, int x, int y, params string[] parents) =>
        new(id, parents, new Cell(x, y));

    private static Quest Game(string id, int x, int y, bool hasCell) =>
        new(id, [], new Cell(x, y), hasCell);
}
