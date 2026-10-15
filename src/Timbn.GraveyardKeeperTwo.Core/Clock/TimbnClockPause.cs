namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnClockPause
{
    private static readonly TimbnHolds<bool> _holds = new(_ => Apply(), Restore);
    private static EnvironmentEngine? _engine;
    private static bool _wasPaused;
    private static bool _applying;
    private static bool _gamePausedMeanwhile;

    internal static IDisposable Take()
    {
        var engine = TimbnGame.IsInGame ? EnvironmentEngine.Instance : null;
        if (engine == null)
            return TimbnUndo.None;

        if (_engine != engine)
        {
            _holds.ReleaseAll();
            _wasPaused = engine.IsPaused;
            _gamePausedMeanwhile = false;
        }

        _engine = engine;
        return _holds.Take(true);
    }

    internal static void ReleaseAll() => _holds.ReleaseAll();

    // The game pausing or unpausing the clock on its own while a hold is on, for a fight or a story scene, which the
    // last release then leaves alone.
    internal static void OnGameSet(bool paused)
    {
        if (_engine != null && !_applying)
            _gamePausedMeanwhile = paused;
    }

    private static void Apply()
    {
        if (_engine == null)
            return;

        _applying = true;
        _engine.IsPaused = true;
        _applying = false;
    }

    private static void Restore()
    {
        var engine = _engine;
        var gamePaused = _gamePausedMeanwhile;
        _engine = null;
        _gamePausedMeanwhile = false;
        if (engine != null && engine.IsPaused && !gamePaused)
            engine.IsPaused = _wasPaused;
    }
}
