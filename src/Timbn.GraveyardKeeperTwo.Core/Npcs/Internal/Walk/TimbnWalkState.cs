namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads and writes walk state the game keeps private on a walker, to pause and resume a walk, to save a person
/// standing still, and to tell a walk of Core's from one the game started over it. Each part does nothing when a game
/// update renamed what it reads.
/// </summary>
internal static class TimbnWalkState
{
    internal static readonly AccessTools.FieldRef<MovementComponent, MovementComponent.Status>? Status =
        TimbnReflection.TryField<MovementComponent, MovementComponent.Status>("status");

    private static readonly AccessTools.FieldRef<MovementComponent, List<MovementData>>? _movementData =
        TimbnReflection.TryField<MovementComponent, List<MovementData>>("movementData");

    private static readonly AccessTools.FieldRef<MovementComponent, int>? _movementDataIndex =
        TimbnReflection.TryField<MovementComponent, int>("currentMovementDataIndex");

    private static readonly AccessTools.FieldRef<MovementComponent, int>? _pathTargetIndex =
        TimbnReflection.TryField<MovementComponent, int>("currentPathTargetIndex");

    private static readonly AccessTools.FieldRef<MovementComponent, Action?>? _onFinishPath =
        TimbnReflection.TryField<MovementComponent, Action?>("onFinishPath");

    private static readonly AccessTools.FieldRef<MovementComponent, Action?>? _onFinishAction =
        TimbnReflection.TryField<MovementComponent, Action?>("onFinishAction");

    private static readonly AccessTools.FieldRef<MovementComponent, Action<float>?>? _onStartAction =
        TimbnReflection.TryField<MovementComponent, Action<float>?>("onStartAction");

    private static readonly AccessTools.FieldRef<MovementComponent, float>? _speed =
        TimbnReflection.TryField<MovementComponent, float>("speed");

    internal static void Pause(MovementComponent movement)
    {
        movement.IsPaused = true;
        if (_onFinishAction != null)
            _onFinishAction(movement)?.Invoke();
    }

    internal static void Resume(MovementComponent movement)
    {
        movement.IsPaused = false;
        if (movement.IsMoving && _onStartAction != null && _speed != null)
            _onStartAction(movement)?.Invoke(_speed(movement));
    }

    internal static Action? StandStill(MovementComponent movement)
    {
        if (Status == null || _movementData == null || _movementDataIndex == null || _pathTargetIndex == null || !movement.IsMoving)
            return null;

        var status = Status(movement);
        var movementData = _movementData(movement);
        var movementDataIndex = _movementDataIndex(movement);
        var pathTargetIndex = _pathTargetIndex(movement);
        var paused = movement.IsPaused;
        Status(movement) = MovementComponent.Status.None;
        _movementData(movement) = [];
        _movementDataIndex(movement) = 0;
        _pathTargetIndex(movement) = 0;
        movement.IsPaused = false;
        return () =>
        {
            Status(movement) = status;
            _movementData(movement) = movementData;
            _movementDataIndex(movement) = movementDataIndex;
            _pathTargetIndex(movement) = pathTargetIndex;
            movement.IsPaused = paused;
        };
    }

    internal static bool? IsUnderWay(MovementComponent movement) =>
        Status == null ? null : Status(movement) is MovementComponent.Status.PathBuilding or MovementComponent.Status.Moving;

    internal static bool? IsWalkFinishing(MovementComponent movement, Action onFinish) =>
        _onFinishPath == null ? null : _onFinishPath(movement) == onFinish;
}
