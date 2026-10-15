namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>How a person walks to a point.</summary>
public enum TimbnWalk
{
    /// <summary>
    /// Along the game's paths, the way villagers walk, taking the shortest way along the roads and going through
    /// doors. When the paths do not join where they stand to the point, the walk is skipped instead of cutting across
    /// the map.
    /// </summary>
    Paths,

    /// <summary>
    /// Straight over the walkable ground to the point, around obstacles but not along the paths, and never through
    /// doors. Use it only to step onto the paths from a spot they do not reach, such as a vendor's stall.
    /// </summary>
    Navmesh,

    /// <summary>
    /// In a straight line to the point, around nothing. Use it only for a short step neither the paths nor the navmesh
    /// can make, such as from behind a counter to its opening.
    /// </summary>
    Straight,
}
