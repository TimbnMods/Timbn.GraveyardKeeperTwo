namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnClockPause
{
    private static readonly TimbnHolds<bool> _holds = new(_ => Apply(), Restore);
    private static EnvironmentEngine? _engine;
    private static bool _wasPaused;

    internal static IDisposable Take()
    {
        var engine = TimbnGame.IsInGame ? EnvironmentEngine.Instance : null;
        if (engine == null)
            return TimbnUndo.None;

        if (_engine != engine)
        {
            _holds.ReleaseAll();
            _wasPaused = engine.IsPaused;
        }

        _engine = engine;
        return _holds.Take(true);
    }

    internal static void ReleaseAll() => _holds.ReleaseAll();

    private static void Apply()
    {
        if (_engine != null)
            _engine.IsPaused = true;
    }

    private static void Restore()
    {
        var engine = _engine;
        _engine = null;
        if (engine != null && engine.IsPaused)
            engine.IsPaused = _wasPaused;
    }
}
