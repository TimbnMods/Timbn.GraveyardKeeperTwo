namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Tells when one of the game's own talks is over, once the game gives the player's control back or never takes it
/// within a few frames. It holds no game state, so it can be tested on its own.
/// </summary>
internal sealed class TimbnTalkWatch
{
    internal const int GraceFrames = 10;

    private bool _hadPlayer;
    private int _frames;

    internal void Restart()
    {
        _hadPlayer = false;
        _frames = 0;
    }

    internal bool Ended(bool gameHasPlayer)
    {
        if (gameHasPlayer)
        {
            _hadPlayer = true;
            return false;
        }

        return _hadPlayer || ++_frames >= GraceFrames;
    }
}
