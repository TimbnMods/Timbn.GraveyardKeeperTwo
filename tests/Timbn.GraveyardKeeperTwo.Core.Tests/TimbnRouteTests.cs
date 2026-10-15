using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnRouteTests
{
    private static readonly TimbnRoute _corner = new([new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(4, 0, 3)]);

    [Fact]
    public void MeasuresAlongTheRouteSeenFromAbove()
    {
        var route = new TimbnRoute([new Vector3(0, 0, 0), new Vector3(3, 5, 4)]);

        Assert.Equal(5f, route.Length, 3);
        Assert.Equal(7f, _corner.Length, 3);
    }

    [Fact]
    public void FindsPointsAlongItAndHoldsToTheEnds()
    {
        AssertNear(new Vector3(2, 0, 0), _corner.PointAt(2f));
        AssertNear(new Vector3(4, 0, 1), _corner.PointAt(5f));
        AssertNear(new Vector3(0, 0, 0), _corner.PointAt(-1f));
        AssertNear(new Vector3(4, 0, 3), _corner.PointAt(10f));
    }

    [Fact]
    public void DropsPointsThatRepeat()
    {
        var route = new TimbnRoute([new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0)]);

        Assert.Equal(2, route.Points.Count);
        Assert.Equal(1f, route.Length, 3);
    }

    [Fact]
    public void HeadsTheWayEachSegmentGoes()
    {
        AssertNear(new Vector3(1, 0, 0), _corner.HeadingAt(1f));
        AssertNear(new Vector3(0, 0, 1), _corner.HeadingAt(6f));
    }

    [Fact]
    public void ProjectsOntoTheNearestPointAhead()
    {
        Assert.Equal(2f, _corner.Project(new Vector3(2, 0, 0.5f), 0f, 3f), 3);
        Assert.Equal(5f, _corner.Project(new Vector3(4.3f, 0, 1), 3f, 3f), 3);
    }

    [Fact]
    public void ProjectsOnlyWithinTheWindow()
    {
        var hairpin = new TimbnRoute([new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(4, 0, 0.5f), new Vector3(0, 0, 0.5f)]);

        Assert.Equal(1f, hairpin.Project(new Vector3(1, 0, 0.4f), 0.5f, 1f), 3);
        Assert.Equal(7.5f, hairpin.Project(new Vector3(1, 0, 0.4f), 7f, 1f), 3);
    }

    [Fact]
    public void NeverProjectsBehindWhereItStarts()
    {
        Assert.Equal(3f, _corner.Project(new Vector3(1, 0, 0), 3f, 1f), 3);
    }

    [Fact]
    public void SlicesWithBothEnds()
    {
        var slice = _corner.Slice(2f, 5f);

        Assert.Equal(3, slice.Count);
        AssertNear(new Vector3(2, 0, 0), slice[0]);
        AssertNear(new Vector3(4, 0, 0), slice[1]);
        AssertNear(new Vector3(4, 0, 1), slice[2]);
    }

    [Fact]
    public void SplicesADetourIn()
    {
        var spliced = _corner.Splice(2f, 5f, [new Vector3(2, 0, 0), new Vector3(3, 0, -1), new Vector3(4, 0, 1)]);

        Assert.Equal(5, spliced.Points.Count);
        AssertNear(new Vector3(0, 0, 0), spliced.Points[0]);
        AssertNear(new Vector3(3, 0, -1), spliced.Points[2]);
        AssertNear(new Vector3(4, 0, 3), spliced.Points[4]);
    }

    [Fact]
    public void SplicesADetourThatEndsTheRoute()
    {
        var spliced = _corner.Splice(0f, 7f, [new Vector3(0, 0, 0), new Vector3(4, 0, 2.7f)]);

        Assert.Equal(2, spliced.Points.Count);
        AssertNear(new Vector3(4, 0, 2.7f), spliced.Points[1]);
    }

    [Fact]
    public void SimplifiesToTheCornersItNeeds()
    {
        List<Vector3> dense = [];
        for (var x = 0; x <= 40; x++)
            dense.Add(new Vector3(x * 0.1f, 0, 0.005f * (x % 2)));

        for (var z = 1; z <= 30; z++)
            dense.Add(new Vector3(4, 0, z * 0.1f));

        var simplified = new TimbnRoute(dense).Simplified(0.02f);

        Assert.Equal(3, simplified.Points.Count);
        AssertNear(new Vector3(4, 0, 0), simplified.Points[1]);
    }

    [Fact]
    public void KeepsAPointPushedOffTheLine()
    {
        var route = new TimbnRoute([new Vector3(0, 0, 0), new Vector3(1, 0, 0.3f), new Vector3(2, 0, 0)]);

        Assert.Equal(3, route.Simplified(0.02f).Points.Count);
    }

    [Fact]
    public void KeepsAPointWhereTheHeightChanges()
    {
        var route = new TimbnRoute([new Vector3(0, 0, 0), new Vector3(1, 0.5f, 0), new Vector3(2, 0, 0)]);

        Assert.Equal(3, route.Simplified(0.02f).Points.Count);
    }

    [Fact]
    public void JoinsBlockedSpotsCloseTogether()
    {
        var stretches = TimbnRoute.Stretches([1f, 1.05f, 1.1f, 2f, 6f, 6.05f], 2.5f);

        List<(float From, float To)> expected = [(1f, 2f), (6f, 6.05f)];
        Assert.Equal(expected, stretches);
    }

    [Fact]
    public void FindsNoStretchesWhenNothingIsBlocked()
    {
        Assert.Empty(TimbnRoute.Stretches([], 2.5f));
    }

    private static void AssertNear(Vector3 expected, Vector3 actual)
    {
        Assert.True(Vector3.Distance(expected, actual) < 0.001f, $"Expected {expected} but got {actual}");
    }
}
