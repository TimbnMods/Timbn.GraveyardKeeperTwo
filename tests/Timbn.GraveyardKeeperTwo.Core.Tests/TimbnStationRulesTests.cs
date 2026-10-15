namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnStationRulesTests
{
    private static readonly string[] _gameBuildings =
    [
        "sawhorse_place_p", "sawhorse_r", "sawhorse_place_r", "woodworking_workbench_1_place_p", "tool_rack_p", "tool_rack_r",
        "resurrection_table_1_place_p",
    ];

    private static bool GameHas(string buildingId) => Array.IndexOf(_gameBuildings, buildingId) >= 0;

    [Fact]
    public void IdsFollowTheGamesNaming()
    {
        Assert.Equal("timbn_smoking_rack_place", TimbnStationRules.SiteId("timbn_smoking_rack"));
        Assert.Equal("timbn_smoking_rack_place_p", TimbnStationRules.BuildingId(TimbnStationRules.SiteId("timbn_smoking_rack")));
        Assert.Equal("timbn_smoking_rack_r", TimbnStationRules.RemovalId("timbn_smoking_rack"));
        Assert.Equal("timbn_smoking_rack_place_r", TimbnStationRules.RemovalId(TimbnStationRules.SiteId("timbn_smoking_rack")));
        Assert.Equal("timbn_red_magic_p", TimbnStationRules.BuildingId("timbn_red_magic"));
        Assert.Equal("timbn_smoking_rack_place_p_d", TimbnStationRules.DescriptionKey("timbn_smoking_rack_place_p"));
    }

    [Fact]
    public void AStationBuiltFromADeskLendsItsBuildFamily()
    {
        Assert.Equal("sawhorse", TimbnStationRules.BuildFamily("sawhorse", GameHas));
    }

    [Fact]
    public void AStationWithNoBuildingFallsBackToTheWoodworkingBench()
    {
        Assert.Equal("woodworking_workbench_1", TimbnStationRules.BuildFamily("kitchen_oven", GameHas));
    }

    [Fact]
    public void AStationTheGameNeverRemovesFallsBackToTheWoodworkingBench()
    {
        Assert.Equal("woodworking_workbench_1", TimbnStationRules.BuildFamily("resurrection_table_1", GameHas));
    }

    [Fact]
    public void AZombieComesBackOnlyWithFreeHandsAndNoCraftRunning()
    {
        Assert.Contains("NoOvhdItem()", TimbnStationRules.HoldTakeCondition);
        Assert.Contains("IsNotCraftingNow()", TimbnStationRules.HoldTakeCondition);
        Assert.Contains("SetWGOPar(\"lock_building_removal\", 1)", TimbnStationRules.HoldPut);
        Assert.Contains("SetWGOPar(\"lock_building_removal\", 0)", TimbnStationRules.HoldTake);
    }

    [Fact]
    public void AnExtensionFallsBackToTheToolRack()
    {
        Assert.Equal("tool_rack", TimbnStationRules.ExtensionFamily("tool_rack", GameHas));
        Assert.Equal("tool_rack", TimbnStationRules.ExtensionFamily("tool_rack_fine", GameHas));
    }

    [Fact]
    public void HalfRefundRoundsDownAndDropsWhatRoundsToNothing()
    {
        var refund = TimbnStationRules.HalfRefund([("flitch", 4), ("nails_bronze", 3), ("stick", 1)]);

        Assert.Equal([("flitch", 2), ("nails_bronze", 1)], refund);
    }

    [Fact]
    public void AGivenRefundKeepsOnlyWhatGivesSomethingBack()
    {
        Assert.Equal([("flitch", 1)], TimbnStationRules.Refund([("flitch", 1), ("stick", 0)]));
        Assert.Empty(TimbnStationRules.Refund([]));
    }

    [Fact]
    public void TheTemplatesAreaStaysOnTheTemplatesDesk()
    {
        Assert.Equal("yard_place", TimbnStationRules.BuildArea("builder_yard", null, ["builder_yard"], "yard_place"));
    }

    [Fact]
    public void AnotherDeskGetsNoAreaUnlessOneIsNamed()
    {
        Assert.Equal("", TimbnStationRules.BuildArea("builder_graveyard", null, ["builder_yard"], "yard_place"));
        Assert.Equal("church_place", TimbnStationRules.BuildArea("builder_church", "church_place", ["builder_yard"], "yard_place"));
    }

    [Fact]
    public void AStationIsNamedOnItselfItsSiteAndItsBuilding()
    {
        var texts = TimbnStationRules.StationTexts("timbn_stick_box", "Stick Box", "A red box.");

        Assert.Equal("Stick Box", texts["timbn_stick_box"]);
        Assert.Equal("Stick Box", texts["timbn_stick_box_place"]);
        Assert.Equal("Stick Box", texts["timbn_stick_box_place_p"]);
        Assert.Equal("A red box.", texts["timbn_stick_box_place_p_d"]);
        Assert.Equal(4, texts.Count);
    }

    [Fact]
    public void AnExtensionIsNamedOnItselfAndItsBuilding()
    {
        var texts = TimbnStationRules.ExtensionTexts("timbn_red_magic", "Red Magic Pillar", "A humming red pillar.");

        Assert.Equal("Red Magic Pillar", texts["timbn_red_magic"]);
        Assert.Equal("Red Magic Pillar", texts["timbn_red_magic_p"]);
        Assert.Equal("A humming red pillar.", texts["timbn_red_magic_p_d"]);
        Assert.Equal(3, texts.Count);
    }

    [Fact]
    public void EmptyTextIsLeftToTheLangFiles()
    {
        Assert.Empty(TimbnStationRules.StationTexts("timbn_stick_box", "", ""));
        Assert.Equal(["timbn_stick_box", "timbn_stick_box_place", "timbn_stick_box_place_p"], TimbnStationRules.StationTexts("timbn_stick_box", "Stick Box", "").Keys);
    }
}
