namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnGameSpeed
{
    private static readonly TimbnHolds<float> _holds = new(Apply, Restore);
    private static UpdateManager? _manager;
    private static float _gameSpeed = 1f;
    private static float _appliedSpeed = 1f;

    internal static IDisposable Set(float multiplier)
    {
        var manager = MainGame.Instance != null ? MainGame.UpdateManager : null;
        if (manager == null)
            return new TimbnUndo(() => { });

        if (_manager != manager)
        {
            _holds.ReleaseAll();
            _gameSpeed = manager.TimeMultiplier;
        }

        _manager = manager;
        return _holds.Take(multiplier);
    }

    internal static void ReleaseAll() => _holds.ReleaseAll();

    private static void Apply(float multiplier)
    {
        if (_manager == null)
            return;

        _manager.SetTimeSpeedMultiplier(multiplier);
        _appliedSpeed = multiplier;
    }

    private static void Restore()
    {
        var manager = _manager;
        _manager = null;
        if (manager != null && Mathf.Approximately(manager.TimeMultiplier, _appliedSpeed))
            manager.SetTimeSpeedMultiplier(_gameSpeed);
    }
}
