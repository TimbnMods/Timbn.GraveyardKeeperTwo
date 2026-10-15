namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The rules behind a station's zombie spot, kept free of game statics so they can be tested. The game's station
/// handlers seat a zombie at the world object's GD point named <see cref="PointId"/>, so a station that seats one needs that point
/// baked into each of its facings, standing at the worker dock and looking back at the station.
/// </summary>
internal static class TimbnZombieSpotRules
{
    /// <summary>The GD point id the game's station, porter station and conveyor station handlers look up.</summary>
    internal const string PointId = "zombie_porter_station_gd_point";

    // Not known whether the game flips a baked GD point's position for a mirrored facing the way it flips a dock point's,
    // so a mirrored facing bakes the position it ends up at until a game check says otherwise.
    internal const bool GameFlipsMirroredPoints = false;

    /// <summary>The way a zombie standing at a facing's dock spot looks, back at the station, so the opposite of the facing.</summary>
    internal static Direction DirectionFor(TimbnFacing facing) => facing switch
    {
        TimbnFacing.Down => Direction.Up,
        TimbnFacing.Up => Direction.Down,
        TimbnFacing.Left => Direction.Right,
        _ => Direction.Left,
    };

    /// <summary>
    /// Where a facing's point is baked, relative to the station. The point stands at the facing's own dock spot. When the
    /// game would flip a mirrored facing's baked point by itself, that facing bakes the spot of the one it flips instead,
    /// the way a mirrored dock point is baked.
    /// </summary>
    /// <param name="ownDock">The dock spot of the facing being baked.</param>
    /// <param name="sourceDock">The dock spot of the facing it flips, or null when it isn't mirrored.</param>
    /// <param name="gameFlips">Whether the game flips a mirrored facing's baked point.</param>
    internal static Vector3 LocalPosition(Vector3 ownDock, Vector3? sourceDock, bool gameFlips) =>
        gameFlips && sourceDock is { } source ? source : ownDock;

    /// <summary>
    /// Whether a zombie seated at this station, which the game treats as a caretaker waiting on it, should be kept from
    /// taking orders. A station made with a zombie spot that doesn't take orders is a plain seat.
    /// </summary>
    internal static bool IsSeatOnly(string? attachedStationId, ICollection<string> seatOnlyStationIds) =>
        attachedStationId != null && seatOnlyStationIds.Contains(attachedStationId);

    /// <summary>
    /// Whether the game handler of an interaction type seats a zombie at the point, which a station only does when it
    /// is based on one of these.
    /// </summary>
    internal static bool HandlerSeatsAtPoint(WGODef.InteractionType type) =>
        type is WGODef.InteractionType.Station or WGODef.InteractionType.PorterStation or WGODef.InteractionType.ConveyorTransporterStation;
}
