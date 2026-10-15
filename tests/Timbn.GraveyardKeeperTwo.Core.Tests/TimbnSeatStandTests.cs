using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSeatStandTests
{
    private static TimbnStationBuilder Station() => new("timbn_seat_stand", "", "");

    [Theory]
    [InlineData(TimbnFacing.Down, Direction.Up)]
    [InlineData(TimbnFacing.Left, Direction.Right)]
    [InlineData(TimbnFacing.Up, Direction.Down)]
    [InlineData(TimbnFacing.Right, Direction.Left)]
    public void AZombieAtTheDockLooksBackAtTheStation(TimbnFacing facing, Direction expected)
    {
        Assert.Equal(expected, TimbnZombieSpotRules.DirectionFor(facing));
    }

    [Fact]
    public void ThePointStandsAtTheFacingsOwnDockWhenTheGameDoesNotFlipIt()
    {
        var own = new Vector3(0.6f, 0f, 0f);
        var source = new Vector3(-0.6f, 0f, 0f);

        Assert.Equal(own, TimbnZombieSpotRules.LocalPosition(own, source, false));
        Assert.Equal(own, TimbnZombieSpotRules.LocalPosition(own, null, false));
    }

    [Fact]
    public void AMirroredFacingBakesTheFlippedFacingsSpotWhenTheGameFlipsItItself()
    {
        var own = new Vector3(0.6f, 0f, 0f);
        var source = new Vector3(-0.6f, 0f, 0f);

        Assert.Equal(source, TimbnZombieSpotRules.LocalPosition(own, source, true));
        Assert.Equal(own, TimbnZombieSpotRules.LocalPosition(own, null, true));
    }

    [Fact]
    public void ASeatOnlyStationKeepsItsZombieSeatedAndAnyOtherStationDoesNot()
    {
        HashSet<string> seats = ["timbn_seat_stand"];

        Assert.True(TimbnZombieSpotRules.IsSeatOnly("timbn_seat_stand", seats));
        Assert.False(TimbnZombieSpotRules.IsSeatOnly("zombie_supplier_station", seats));
        Assert.False(TimbnZombieSpotRules.IsSeatOnly(null, seats));
        Assert.False(TimbnZombieSpotRules.IsSeatOnly("timbn_seat_stand", []));
    }

    [Theory]
    [InlineData(WGODef.InteractionType.Station, true)]
    [InlineData(WGODef.InteractionType.PorterStation, true)]
    [InlineData(WGODef.InteractionType.ConveyorTransporterStation, true)]
    [InlineData(WGODef.InteractionType.Craft, false)]
    public void OnlyTheStationHandlersSeatAZombieAtThePoint(WGODef.InteractionType type, bool seats)
    {
        Assert.Equal(seats, TimbnZombieSpotRules.HandlerSeatsAtPoint(type));
    }

    [Fact]
    public void TheZombieSpotIsAPlainSeatUnlessTheStationTakesOrders()
    {
        var plain = Station().ZombieSpot();
        var supplier = Station().ZombieSpot(takesOrders: true);

        Assert.True(plain.HasZombieSpot);
        Assert.False(plain.ZombieTakesOrders);
        Assert.True(supplier.HasZombieSpot);
        Assert.True(supplier.ZombieTakesOrders);
        Assert.False(Station().HasZombieSpot);
    }

    [Fact]
    public void TheLimitCountsTheStationAndItsSites()
    {
        Assert.Equal("TimbnCount(\"timbn_seat_stand\", \"timbn_seat_stand_place\")",
            TimbnStationLimitRules.CurrentExpression("timbn_seat_stand", TimbnStationRules.SiteId("timbn_seat_stand")));
    }

    [Fact]
    public void ALimitIsRecordedOnTheBuilder()
    {
        Assert.Null(Station().BuildLimit);
        Assert.Equal(3, Station().Limit(3).BuildLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ALimitBelowOneThrows(int max)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Station().Limit(max));
    }
}
