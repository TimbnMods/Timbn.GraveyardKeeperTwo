namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnControl
{
    private const int _firstReason = 7400;

    private static readonly List<Hold> _holds = [];

    internal static IDisposable Take()
    {
        var player = MainGame.Instance != null ? MainGame.PlayerController : null;
        if (player == null)
            return new TimbnUndo(() => { });

        var reason = _firstReason;
        while (_holds.Any(hold => hold.Reason == (TakenControlType)reason))
            reason++;

        var taken = new Hold(player, (TakenControlType)reason);
        _holds.Add(taken);
        Set(player, taken.Reason, taken: true);
        return taken;
    }

    internal static bool IsTakenByGame(TakenControlType[] ignored)
    {
        if (!TimbnGame.IsInGame || MainGame.PlayerController == null)
            return false;

        return !MainGame.PlayerController.IsControlsEnabledExcept(ignored.Concat(_holds.Select(hold => hold.Reason)).ToArray());
    }

    internal static void ReleaseAll()
    {
        foreach (var hold in _holds.ToList())
            hold.Dispose();
    }

    private static void Set(PlayerController player, TakenControlType reason, bool taken)
    {
        player.SetControlTakenType(reason, isEnabled: !taken);
        player.PhysicalBody.SetNonKinematicFlag((PlayerDynamicType)reason, isDynamic: !taken);
    }

    private sealed class Hold(PlayerController player, TakenControlType reason) : IDisposable
    {
        public TakenControlType Reason { get; } = reason;

        public void Dispose()
        {
            if (_holds.Remove(this) && player != null)
                Set(player, Reason, taken: false);
        }
    }
}
