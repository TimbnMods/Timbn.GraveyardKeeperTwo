namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnControl
{
    internal const TakenControlType Flag = (TakenControlType)7400;

    private static readonly TimbnHolds<bool> _holds = new(_ => SetTaken(true), () => SetTaken(false));
    private static PlayerController? _player;

    internal static IDisposable Take()
    {
        var player = MainGame.Instance != null ? MainGame.PlayerController : null;
        if (player == null)
            return new TimbnUndo(() => { });

        if (_player != player)
            _holds.ReleaseAll();

        _player = player;
        return _holds.Take(true);
    }

    internal static void ReleaseAll() => _holds.ReleaseAll();

    private static void SetTaken(bool taken)
    {
        var player = _player;
        if (!taken)
            _player = null;

        if (player != null)
            player.SetControlTakenType(Flag, isEnabled: !taken);
    }
}
