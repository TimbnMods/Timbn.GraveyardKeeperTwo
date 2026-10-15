namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The way one side of a station faces as the camera sees it. The facing is also the edge of the footprint its dock spot
/// is on, where the player or a zombie stands to work it and finished items drop. The values are the rotation indexes
/// the game saves, and R in build mode turns through the facings a station has in this order. Only Left and Right can be
/// mirrored, since a station facing down or up looks the same flipped.
/// </summary>
public enum TimbnFacing
{
    /// <summary>Facing the camera, with the dock spot on the front edge. The facing every station has.</summary>
    Down = 0,

    /// <summary>Facing left, with the dock spot on the left edge.</summary>
    Left = 1,

    /// <summary>Facing away from the camera, with the dock spot on the back edge.</summary>
    Up = 2,

    /// <summary>Facing right, with the dock spot on the right edge.</summary>
    Right = 3,
}
