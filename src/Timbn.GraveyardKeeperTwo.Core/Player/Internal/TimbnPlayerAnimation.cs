namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Holds an animation on the player for plugins and keeps it playing, since the game's movement sets the player idle on its own.</summary>
internal static class TimbnPlayerAnimation
{
    private static readonly TimbnHolds<AnimationState> _holds = new(Apply, Release);
    private static AnimationState? _held;

    internal static IDisposable Hold(AnimationState state) =>
        TimbnGame.IsInGame ? _holds.Take(state) : TimbnUndo.None;

    internal static void Tick()
    {
        if (_held is { } state && Animation is { } animation && animation.AnimationState != state && !TimbnPlayerControl.IsTakenByGame([]))
            animation.SetState(state);
    }

    internal static void ReleaseAll() => _holds.ReleaseAll();

    private static PlayerAnimation? Animation =>
        TimbnGame.IsInGame && MainGame.PlayerController != null ? MainGame.PlayerController.View.PlayerAnimation : null;

    private static void Apply(AnimationState state)
    {
        _held = state;
        Animation?.SetState(state);
    }

    private static void Release()
    {
        if (_held is { } held && Animation is { } animation && animation.AnimationState == held)
            animation.SetState(AnimationState.Idle);

        _held = null;
    }
}
