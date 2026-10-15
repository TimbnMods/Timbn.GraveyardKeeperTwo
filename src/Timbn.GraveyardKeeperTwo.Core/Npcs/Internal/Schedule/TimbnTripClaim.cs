namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Tells which claim a trip made, so a schedule or run lets go only of a point claimed since the step that started the
/// trip. A point claimed before it, such as a seat the person walked to, stays claimed.
/// </summary>
internal sealed class TimbnTripClaim
{
    private string? _claimedAtStart;

    internal void Begin(string? claimed) => _claimedAtStart = claimed;

    internal bool ShouldRelease(string? claimed) => claimed != null && claimed != _claimedAtStart;
}
