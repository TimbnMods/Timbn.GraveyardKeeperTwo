namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnTripClaimTests
{
    private readonly TimbnTripClaim _claim = new();

    [Fact]
    public void AClaimMadeBeforeTheStepStaysClaimed()
    {
        _claim.Begin("bench");

        Assert.False(_claim.ShouldRelease("bench"));
    }

    [Fact]
    public void AClaimMadeSinceTheStepIsReleased()
    {
        _claim.Begin(null);

        Assert.True(_claim.ShouldRelease("pond"));
    }

    [Fact]
    public void ADifferentClaimThanTheOneBeforeIsReleased()
    {
        _claim.Begin("bench");

        Assert.True(_claim.ShouldRelease("pond"));
    }

    [Fact]
    public void NothingClaimedMeansNothingToRelease()
    {
        _claim.Begin(null);

        Assert.False(_claim.ShouldRelease(null));
    }

    [Fact]
    public void ALaterStepTakesANewSnapshot()
    {
        _claim.Begin(null);
        _claim.Begin("bench");

        Assert.False(_claim.ShouldRelease("bench"));
    }
}
