namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Takes the player's control for plugins, one reason in the game's own list per hold, and gives it back as holds are released.</summary>
internal static class TimbnControl
{
    private const int _firstReason = 7400;

    private static readonly List<Hold> _holds = [];
    private static int _moving;

    internal static bool HasPluginHolds => _holds.Count > 0;

    /// <summary>True when plugins hold the player's control and the game has not taken it for any reason of its own.</summary>
    internal static bool IsHeldByPlugins =>
        _holds.Count > 0
        && TimbnGame.IsInGame
        && MainGame.PlayerController != null
        && !MainGame.PlayerController.IsControlsEnabled
        && !IsTakenByGame([]);

    internal static IDisposable Take()
    {
        var player = MainGame.Instance != null ? MainGame.PlayerController : null;
        if (player == null)
            return TimbnUndo.None;

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

    /// <summary>Lets the player's body move under the plugins' holds until disposed, so a walk can steer it.</summary>
    internal static IDisposable LetMove()
    {
        _moving++;
        Refreeze();
        return new TimbnUndo(() =>
        {
            _moving--;
            Refreeze();
        });
    }

    internal static void ReleaseAll()
    {
        foreach (var hold in _holds.ToList())
            hold.Dispose();
    }

    private static void Set(PlayerController player, TakenControlType reason, bool taken)
    {
        player.SetControlTakenType(reason, isEnabled: !taken);
        player.PhysicalBody.SetNonKinematicFlag((PlayerDynamicType)reason, isDynamic: !taken || _moving > 0);
    }

    private static void Refreeze()
    {
        var player = MainGame.Instance != null ? MainGame.PlayerController : null;
        if (player == null)
            return;

        foreach (var hold in _holds)
            player.PhysicalBody.SetNonKinematicFlag((PlayerDynamicType)hold.Reason, isDynamic: _moving > 0);
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
