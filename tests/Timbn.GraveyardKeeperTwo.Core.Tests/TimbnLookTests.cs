using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLookTests
{
    private static readonly IReadOnlyCollection<TimbnFacing> _downAndLeft = [TimbnFacing.Down, TimbnFacing.Left];

    private static readonly IReadOnlyDictionary<TimbnFacing, TimbnFacing> _none = new Dictionary<TimbnFacing, TimbnFacing>();

    [Fact]
    public void ModelsBuiltFromFacesLineUpByTheDifferenceOfTheirCenters()
    {
        var offset = TimbnLookRules.AlignmentOffset(new Vector2(20f, 12f), new Vector2(30f, 20f));

        Assert.Equal(-10f / 50f, offset.x, 5);
        Assert.Equal(0f, offset.y);
        Assert.Equal(-8f / 40f, offset.z, 5);
        Assert.Equal(Vector3.zero, TimbnLookRules.AlignmentOffset(null, new Vector2(1f, 1f)));
        Assert.Equal(Vector3.zero, TimbnLookRules.AlignmentOffset(new Vector2(1f, 1f), null));
    }
}
