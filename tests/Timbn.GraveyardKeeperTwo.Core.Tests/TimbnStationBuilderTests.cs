using System.Runtime.CompilerServices;
using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnStationBuilderTests
{
    private static TimbnStationBuilder Station() => new("timbn_stick_box", "", "");

    private static TimbnExtensionBuilder Extension() => new("timbn_red_magic", "", "");

    [Fact]
    public void AStationStartsFreeOnTheDeskWithTheTemplatesExtensionsAndHalfRefund()
    {
        var station = Station();

        Assert.Empty(station.CostItems);
        Assert.Null(station.RefundItems);
        Assert.Null(station.ExtensionIds);
        Assert.Null(station.TechId);
    }

    [Fact]
    public void AStationLeavesTheBaseStationsBehaviorAloneUntilAskedToChangeIt()
    {
        var station = Station();

        Assert.Null(station.TalentId);
        Assert.Null(station.AllowsZombie);
        Assert.Null(station.IsAutomatic);
        Assert.Null(station.AutoTickDuration);
        Assert.Null(station.ActionSound);
    }

    [Fact]
    public void TheNamedSettersRecordWhatToChange()
    {
        var station = Station().Talent("talent_orange").TakesZombie().WorkSound("woodworking_workbench_tick");

        Assert.Equal("talent_orange", station.TalentId);
        Assert.True(station.AllowsZombie);
        Assert.Equal("woodworking_workbench_tick", station.ActionSound);
        Assert.False(Station().TakesZombie(false).AllowsZombie);
    }

    [Fact]
    public void ToolAndSlotSettersRecordWhatToChange()
    {
        var station = Station().Tool(ItemType.Hammer).CraftSlots(4).Storage(9);

        Assert.Equal(ItemType.Hammer, station.ToolType);
        Assert.Equal(4, station.CraftSlotCount);
        Assert.Equal(9, station.StorageSlotCount);
        Assert.Equal(ItemType.None, Station().Tool(ItemType.None).ToolType);
        Assert.Equal(0, Station().CraftSlots(0).CraftSlotCount);
    }

    [Fact]
    public void ASlotCountBelowZeroIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Station().CraftSlots(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Station().Storage(-1));
    }

    [Fact]
    public void ABuildTabIsKeptAsGivenIncludingTheFirstTabsEmptyName()
    {
        Assert.Null(Station().TabId);
        Assert.Equal("tab_sewing_extension", Station().BuildTab("tab_sewing_extension").TabId);
        Assert.Equal("", Station().BuildTab("").TabId);
    }

    [Fact]
    public void AutomaticSetsATickAndManualTakesItBack()
    {
        var automatic = Station().Automatic(10);

        Assert.True(automatic.IsAutomatic);
        Assert.Equal(10, automatic.AutoTickDuration);

        var manual = automatic.Manual();

        Assert.False(manual.IsAutomatic);
        Assert.Null(manual.AutoTickDuration);
    }

    [Fact]
    public void ADockDistanceBelowZeroIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Station().DockDistance(-0.1f));
        Assert.Equal(0.2f, Station().DockDistance(0.2f).DockGap);
    }

    [Fact]
    public void AStationIsBuiltWithASiteUnlessToldOtherwise()
    {
        Assert.False(Station().SkipsConstruction);
        Assert.True(Station().NoConstruction().SkipsConstruction);
    }

    [Fact]
    public void CostAddsUpOverSeveralCalls()
    {
        var station = Station().Cost(("flitch", 2)).Cost(("nails_bronze", 4), ("stick", 1));

        Assert.Equal([("flitch", 2), ("nails_bronze", 4), ("stick", 1)], station.CostItems);
    }

    [Fact]
    public void RefundAddsUpAndNoRefundGivesNothingBack()
    {
        Assert.Equal([("flitch", 1), ("stick", 2)], Station().Refund(("flitch", 1)).Refund(("stick", 2)).RefundItems);
        Assert.Equal([], Station().NoRefund().RefundItems);
    }

    [Fact]
    public void ExtensionsReplaceTheTemplatesListThenAddToIt()
    {
        var station = Station().Extensions("tool_rack").Extensions("timbn_red_magic", "timbn_blue_magic");

        Assert.Equal(["tool_rack", "timbn_red_magic", "timbn_blue_magic"], station.ExtensionIds);
    }

    [Fact]
    public void NoExtensionsTakesTheTemplatesAway()
    {
        Assert.Equal([], Station().NoExtensions().ExtensionIds);
    }

    [Fact]
    public void UnlockedByNamesTheTech()
    {
        Assert.Equal("work_with_wood_1", Station().UnlockedBy("work_with_wood_1").TechId);
    }

    [Fact]
    public void AnExtensionCostsAndRefundsLikeAStation()
    {
        var extension = Extension().Cost(("flitch", 2)).Cost(("stick", 1)).NoRefund().UnlockedBy("work_with_wood_1");

        Assert.Equal([("flitch", 2), ("stick", 1)], extension.CostItems);
        Assert.Equal([], extension.RefundItems);
        Assert.Equal("work_with_wood_1", extension.TechId);
    }

    [Fact]
    public void AnExtensionStartsFreeAndOnTheDesk()
    {
        var extension = Extension();

        Assert.Empty(extension.CostItems);
        Assert.Null(extension.RefundItems);
        Assert.Null(extension.TechId);
    }

    // A model wraps a Unity GameObject that can't exist outside the game, and the sized overloads never touch it.
    private static TimbnModel Model() => (TimbnModel)RuntimeHelpers.GetUninitializedObject(typeof(TimbnModel));

    [Fact]
    public void AStationWithOnlyAModelHasOneSideFacingDown()
    {
        var model = Model();
        var station = Station().Model(model, new Vector3(1.5f, 0.8f, 1f));

        var side = Assert.Single(station.Sides);
        Assert.Same(model, side.Model);
        Assert.Equal(new Vector3(1.5f, 0.8f, 1f), side.Size);
        Assert.Equal(TimbnFacing.Down, side.Facing);
        Assert.Same(model, station.StationModel);
        Assert.Equal(new Vector3(1.5f, 0.8f, 1f), station.Size);
    }

    [Fact]
    public void EachFacingTakesItsOwnModelSizeAndSite()
    {
        var (down, left, downSite, leftSite) = (Model(), Model(), Model(), Model());
        var station = Station()
            .Model(down, new Vector3(1.2f, 1.4f, 0.9f)).Site(downSite)
            .Left(left, new Vector3(0.9f, 1.4f, 1.5f), leftSite);

        Assert.Equal([TimbnFacing.Down, TimbnFacing.Left], station.Sides.Select(side => side.Facing));
        Assert.Same(down, station.Sides[0].Model);
        Assert.Same(downSite, station.Sides[0].Site);
        Assert.Same(left, station.Sides[1].Model);
        Assert.Same(leftSite, station.Sides[1].Site);
        Assert.Equal(new Vector3(0.9f, 1.4f, 1.5f), station.Sides[1].Size);
        Assert.Same(downSite, station.SiteModel);
    }

    [Fact]
    public void AFacingWithNoSiteLeavesItToTheGeneratedOutline()
    {
        var station = Station().Model(Model(), Vector3.one).Right(Model(), Vector3.one);

        Assert.All(station.Sides, side => Assert.Null(side.Site));
    }

    [Fact]
    public void SiteCalledBeforeModelIsKept()
    {
        var site = Model();
        var station = Station().Site(site).Model(Model(), Vector3.one);

        Assert.Same(site, station.SiteModel);
    }

    [Fact]
    public void FacingsComeOutInTheOrderRTurnsWhateverOrderTheyWereCalled()
    {
        var station = Station().Right(Model(), Vector3.one).Up(Model(), Vector3.one).Left(Model(), Vector3.one).Model(Model(), Vector3.one);

        Assert.Equal([TimbnFacing.Down, TimbnFacing.Left, TimbnFacing.Up, TimbnFacing.Right], station.Sides.Select(side => side.Facing));
    }

    [Fact]
    public void ASideNeedsASizeOnEveryAxis()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Station().Model(Model(), new Vector3(1f, 0f, 1f)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Station().Left(Model(), new Vector3(1f, 1f, -1f)));
    }

    [Fact]
    public void MirrorSidesFillsTheMissingRightFromTheLeftWhereverItIsCalled()
    {
        var (down, left) = (Model(), Model());
        var station = Station().MirrorSides().Model(down, Vector3.one).Left(left, Vector3.one);

        Assert.Equal(3, station.Sides.Count);
        Assert.Same(left, station.Sides[2].Model);
        Assert.Equal(TimbnFacing.Right, station.Sides[2].Facing);
        Assert.Equal(TimbnFacing.Left, station.Sides[2].MirrorOf);
    }

    [Fact]
    public void MirrorSidesNeverFlipsADownFacingStation()
    {
        var station = Station().Model(Model(), Vector3.one).MirrorSides();

        Assert.Single(station.Sides);
    }

    [Fact]
    public void AStationWithNoModelCantResolveItsSides()
    {
        Assert.Throws<InvalidOperationException>(() => Station().Left(Model(), Vector3.one).Sides);
        Assert.Null(Station().StationModel);
    }
}
