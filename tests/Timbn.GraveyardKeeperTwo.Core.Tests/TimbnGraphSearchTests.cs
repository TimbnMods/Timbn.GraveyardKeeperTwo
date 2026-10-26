namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnGraphSearchTests
{
    private readonly Dictionary<string, Point> _points = [];

    private Point Add(string id, float x)
    {
        var point = new Point(id, x);
        _points[id] = point;
        return point;
    }

    private void Link(string from, string to) => _points[from].Next.Add(_points[to]);

    private List<string>? Route(string from, string to, out float length) =>
        TimbnGraphSearch.Route(_points[from], _points[to], point => point.Next, (a, b) => Math.Abs(a.X - b.X), out length)?.Select(point => point.Id).ToList();

    [Fact]
    public void FindsTheShortestRoute()
    {
        Add("a", 0);
        Add("b", 1);
        Add("c", 2);
        Add("far", 10);
        Link("a", "far");
        Link("far", "c");
        Link("a", "b");
        Link("b", "c");

        Assert.Equal(["a", "b", "c"], Route("a", "c", out var length));
        Assert.Equal(2f, length);
    }

    [Fact]
    public void FollowsLinksOnlyTheWayTheyPoint()
    {
        Add("a", 0);
        Add("b", 1);
        Link("a", "b");

        Assert.NotNull(Route("a", "b", out _));
        Assert.Null(Route("b", "a", out var length));
        Assert.Equal(0f, length);
    }

    [Fact]
    public void GoesTheLongWayRoundALoop()
    {
        Add("a", 0);
        Add("b", 1);
        Add("c", 2);
        Link("a", "b");
        Link("b", "c");
        Link("c", "a");

        Assert.Equal(["b", "c", "a"], Route("b", "a", out _));
    }

    public sealed class Point
    {
        public Point(string id, float x)
        {
            Id = id;
            X = x;
        }

        public string Id { get; }

        public float X { get; }

        public List<Point> Next { get; } = [];
    }
}
