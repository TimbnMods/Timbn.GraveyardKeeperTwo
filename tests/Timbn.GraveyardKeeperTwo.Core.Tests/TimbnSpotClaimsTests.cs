namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSpotClaimsTests
{
    private readonly List<string> _log = [];
    private readonly Npc _herm = new("herm");
    private readonly Npc _larry = new("larry");

    private TimbnPointClaims<Npc> NewClaims() => new((spot, owner) => _log.Add($"occupy {spot} {owner.Name}"), spot => _log.Add($"release {spot}"));

    [Fact]
    public void ClaimOccupiesTheSpotInTheGame()
    {
        var claims = NewClaims();

        Assert.True(claims.Claim("bench", _herm));

        Assert.Equal(["occupy bench herm"], _log);
        Assert.Equal("bench", claims.PointOf(_herm));
        Assert.Same(_herm, claims.HolderOf("bench"));
    }

    [Fact]
    public void AnotherOwnerCannotClaimAHeldSpot()
    {
        var claims = NewClaims();
        claims.Claim("bench", _herm);

        Assert.False(claims.Claim("bench", _larry));
        Assert.Null(claims.PointOf(_larry));
    }

    [Fact]
    public void ASpotTheGameGaveAVillagerCannotBeClaimed()
    {
        var claims = NewClaims();
        claims.Claim("pond", _herm);

        Assert.False(claims.Claim("bench", _herm, takenInGame: true));
        Assert.Null(claims.HolderOf("bench"));
        Assert.Equal("pond", claims.PointOf(_herm));
        Assert.Equal(["occupy pond herm"], _log);
    }

    [Fact]
    public void OwnClaimIsKeptWhateverTheGameMarks()
    {
        var claims = NewClaims();
        claims.Claim("bench", _herm);

        Assert.True(claims.Claim("bench", _herm, takenInGame: true));
        Assert.False(claims.Claim("bench", _larry, takenInGame: true));
    }

    [Fact]
    public void ClaimingAgainKeepsTheSpotWithoutTouchingTheGame()
    {
        var claims = NewClaims();
        claims.Claim("bench", _herm);

        Assert.True(claims.Claim("bench", _herm));
        Assert.Equal(["occupy bench herm"], _log);
    }

    [Fact]
    public void ANewClaimLetsGoOfTheOldOne()
    {
        var claims = NewClaims();
        claims.Claim("bench", _herm);

        claims.Claim("pond", _herm);

        Assert.Equal(["occupy bench herm", "release bench", "occupy pond herm"], _log);
        Assert.Null(claims.HolderOf("bench"));
    }

    [Fact]
    public void ReleaseAllFreesEverySpot()
    {
        var claims = NewClaims();
        claims.Claim("bench", _herm);
        claims.Claim("pond", _larry);

        claims.ReleaseAll();
        claims.Release(_herm);

        Assert.Equal(0, claims.Count);
        Assert.Equal(2, _log.Count(line => line.StartsWith("release", StringComparison.Ordinal)));
    }

    [Fact]
    public void OwnClaimNeverBlocks()
    {
        var claims = NewClaims();
        claims.Claim("bench", _herm);

        Assert.Null(claims.WhyBlocked("bench", _herm, exists: true, enabled: true, takenInGame: true, standingThere: null));
    }

    [Fact]
    public void SomeoneElsesClaimBlocks()
    {
        var claims = NewClaims();
        claims.Claim("bench", _larry);

        Assert.Equal("claimed by another schedule", claims.WhyBlocked("bench", _herm, true, true, true, null));
    }

    [Theory]
    [InlineData(false, true, false, null, "no such point")]
    [InlineData(true, false, false, null, "disabled")]
    [InlineData(true, true, true, null, "claimed in the simulator")]
    [InlineData(true, true, false, "the player", "occupied by the player")]
    [InlineData(true, true, false, null, null)]
    public void WhyBlockedChecksInOrder(bool exists, bool enabled, bool takenInGame, string? standing, string? expected) =>
        Assert.Equal(expected, NewClaims().WhyBlocked("bench", _herm, exists, enabled, takenInGame, standing));

    [Fact]
    public void SomeoneStandingOnHisOwnClaimStillBlocks()
    {
        var claims = NewClaims();
        claims.Claim("bench", _herm);

        Assert.Equal("occupied by the player", claims.WhyBlocked("bench", _herm, true, true, true, "the player"));
    }

    public sealed class Npc
    {
        public Npc(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
