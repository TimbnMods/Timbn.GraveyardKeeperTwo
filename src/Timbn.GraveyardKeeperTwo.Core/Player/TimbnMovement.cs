namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnMovement
{
    private static readonly AccessTools.FieldRef<PlayerPhysicalBody, bool> _isMovementLocked =
        AccessTools.FieldRefAccess<PlayerPhysicalBody, bool>("isMovementLocked");

    private static readonly TimbnHolds<bool> _stillHolds = new(_ => Lock(), Unlock);
    private static readonly TimbnHolds<float> _speedHolds = new(ApplySpeed, RestoreSpeed);
    private static PlayerPhysicalBody? _lockedBody;
    private static bool _gameLocked;
    private static PlayerPhysicalBody? _spedBody;
    private static float _gameSpeed = 1f;
    private static float _appliedSpeed = 1f;
    private static bool _locking;

    internal static IDisposable HoldStill()
    {
        if (Body() is not { } body)
            return new TimbnUndo(() => { });

        if (_lockedBody != body)
            _stillHolds.ReleaseAll();

        _lockedBody = body;
        return _stillHolds.Take(true);
    }

    internal static IDisposable SetSpeed(float multiplier)
    {
        if (Body() is not { } body)
            return new TimbnUndo(() => { });

        if (_spedBody != body)
        {
            _speedHolds.ReleaseAll();
            _gameSpeed = body.SpeedMultiplier;
        }

        _spedBody = body;
        return _speedHolds.Take(multiplier);
    }

    internal static bool Allow(PlayerPhysicalBody body, bool isLock)
    {
        if (_locking || !_stillHolds.IsHeld || body != _lockedBody)
            return true;

        _gameLocked = isLock;
        return false;
    }

    internal static void ReleaseAll()
    {
        _stillHolds.ReleaseAll();
        _speedHolds.ReleaseAll();
    }

    private static PlayerPhysicalBody? Body() =>
        MainGame.Instance != null ? MainGame.PlayerController?.PhysicalBody : null;

    private static void Lock()
    {
        if (_lockedBody == null)
            return;

        _gameLocked = _isMovementLocked(_lockedBody);
        _locking = true;
        try
        {
            _lockedBody.LockMovement(true);
        }
        finally
        {
            _locking = false;
        }
    }

    private static void Unlock()
    {
        var body = _lockedBody;
        _lockedBody = null;
        if (body != null)
            body.LockMovement(_gameLocked);
    }

    private static void ApplySpeed(float multiplier)
    {
        if (_spedBody == null)
            return;

        _spedBody.SpeedMultiplier = multiplier;
        _appliedSpeed = multiplier;
    }

    private static void RestoreSpeed()
    {
        var body = _spedBody;
        _spedBody = null;
        if (body != null && Mathf.Approximately(body.SpeedMultiplier, _appliedSpeed))
            body.SpeedMultiplier = _gameSpeed;
    }
}
