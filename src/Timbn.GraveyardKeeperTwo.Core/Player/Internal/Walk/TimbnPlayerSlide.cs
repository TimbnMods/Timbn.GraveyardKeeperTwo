namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The player sliding to a spot with their physics off, moved by the game's own walker, either in a straight line the
/// way the game walks the player to a reservoir or along the NPC navmesh the way a story scene walks them. It passes
/// through anything in the way that NPCs fit past. Only the newest slide can call back, and disposing one stops it only
/// while it is still the player's slide, not one the game started over it.
/// </summary>
internal sealed class TimbnPlayerSlide : IDisposable
{
    /// <summary>The pace the game slides the player at for a short step, such as to a reservoir.</summary>
    internal const float StepPace = 1.5f;

    /// <summary>The pace the game walks the player at on its own, to a workstation.</summary>
    internal const float WalkPace = 3.3f;

    private static TimbnPlayerSlide? _current;

    private readonly Action? _arrived;
    private readonly ManualLogSource _logger;
    private readonly Action _finish;

    private TimbnPlayerSlide(Action? arrived, ManualLogSource logger)
    {
        _arrived = arrived;
        _logger = logger;
        _finish = Finish;
    }

    internal bool IsDone { get; private set; }

    internal Action? Ended { get; set; }

    internal static TimbnPlayerSlide Start(Vector3 position, Action? arrived, ManualLogSource logger, bool overNavmesh = false, float pace = StepPace)
    {
        var slide = new TimbnPlayerSlide(arrived, logger);
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player == null)
        {
            slide.IsDone = true;
            return slide;
        }

        var movement = player.MovementComponent;
        if (_current is { IsDone: false } previous)
        {
            // Stopped here, since a path to the spot the player already stands on returns at once and would leave
            // the old one running with nothing to end it.
            previous.End();
            if (movement.IsMoving && TimbnWalkState.IsWalkFinishing(movement, previous._finish) != false)
                movement.ForceStop();
        }

        _current = slide;
        var scene = MainGame.PlayerData.currentGameSceneId;
        var type = overNavmesh ? MovementType.Recast : MovementType.Direct;
        movement.StartPath(position, scene, scene, type, pace, onFinish: slide._finish, customSeeker: player.PlayerLocalAreaMovement.Seeker);
        if (!slide.IsDone && TimbnWalkState.IsUnderWay(movement) == false)
            slide.NoWay(position);

        return slide;
    }

    public void Dispose()
    {
        if (IsDone)
            return;

        IsDone = true;
        if (_current != this)
            return;

        _current = null;
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player != null && player.MovementComponent.IsMoving && TimbnWalkState.IsWalkFinishing(player.MovementComponent, _finish) != false)
            player.MovementComponent.ForceStop();
    }

    private void End()
    {
        if (IsDone)
            return;

        IsDone = true;
        Ended?.Invoke();
    }

    private void NoWay(Vector3 position)
    {
        _logger.LogWarning($"{nameof(TimbnPlayerSlide)}|No way to ({position.x:0.00}, {position.z:0.00}), the player stays put.");
        if (_current == this)
            _current = null;

        End();
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player != null)
            player.OnPathComplete(player.MovementComponent);
    }

    private void Finish()
    {
        if (IsDone || _current != this)
            return;

        _current = null;
        End();
        var player = TimbnGame.IsInGame ? MainGame.PlayerController : null;
        if (player != null && player.MovementComponent.Completion == MovementComponent.CompletionState.Fail)
        {
            _logger.LogWarning($"{nameof(TimbnPlayerSlide)}|The slide could not reach its spot, so it does not call back.");
            return;
        }

        if (_arrived != null)
            TimbnSafe.Run(_arrived, _logger, $"{nameof(TimbnPlayerSlide)}|{TimbnSafe.Describe(_arrived)}");
    }
}
