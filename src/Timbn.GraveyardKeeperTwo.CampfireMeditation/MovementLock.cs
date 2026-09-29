namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

/// <summary>Holds the player still while meditating and keeps whatever movement lock the game asks for meanwhile, to hand it back on release.</summary>
internal static class MovementLock
{
    private static readonly AccessTools.FieldRef<PlayerPhysicalBody, bool> _isMovementLocked =
        AccessTools.FieldRefAccess<PlayerPhysicalBody, bool>("isMovementLocked");

    private static PlayerPhysicalBody? _body;
    private static bool _gameLocked;

    public static void Hold(PlayerPhysicalBody body)
    {
        if (_body == body)
            return;

        Release();
        var gameLocked = _isMovementLocked(body);
        body.LockMovement(true);
        _body = body;
        _gameLocked = gameLocked;
    }

    public static void Release()
    {
        var body = _body;
        _body = null;
        if (body != null)
            body.LockMovement(_gameLocked);
    }

    public static bool Allow(PlayerPhysicalBody body, bool isLock)
    {
        if (_body == null || body != _body)
            return true;

        _gameLocked = isLock;
        return false;
    }
}
