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
    public void AStationBuiltFromADeskLendsItsBuildFamily()
    {
        Assert.Equal("sawhorse", TimbnStationRules.BuildFamily("sawhorse", GameHas));
        Assert.Equal("woodworking_workbench_1", TimbnStationRules.BuildFamily("kitchen_oven", GameHas));
        Assert.Equal("woodworking_workbench_1", TimbnStationRules.BuildFamily("resurrection_table_1", GameHas));
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
    public void TheTemplatesAreaStaysOnTheTemplatesDesk()
    {
        Assert.Equal("yard_place", TimbnStationRules.BuildArea("builder_yard", null, ["builder_yard"], "yard_place"));
        Assert.Equal("", TimbnStationRules.BuildArea("builder_graveyard", null, ["builder_yard"], "yard_place"));
        Assert.Equal("church_place", TimbnStationRules.BuildArea("builder_church", "church_place", ["builder_yard"], "yard_place"));
    }
}
