using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Runs one <see cref="TimbnObjectAction"/>, showing its hint beside the object, walking the player to the spot when
/// they press Interaction, and holding them there for its minutes. It ends early when the game takes the controls back.
/// </summary>
internal sealed class TimbnObjectActionRunner : IDisposable
{
    private const string _obstacleLayer = "Obstacle";
    private const float _searchRadius = 1.5f;
    private const float _objectReach = 1f;
    private const float _hintAbove = 0.5f;
    private const double _giveUpWalkMinutes = 30;

    private readonly TimbnFrameworkPlugin _owner;
    private readonly TimbnObjectAction _action;
    private readonly TimbnInteractionHint _hint;
    private readonly Collider[] _hits = new Collider[32];
    private int _mask;
    private IDisposable? _control;
    private IDisposable? _walk;
    private double _until;
    private bool _walking;
    private bool _acting;

    internal TimbnObjectActionRunner(TimbnFrameworkPlugin owner, TimbnObjectAction action)
    {
        _owner = owner;
        _action = action;
        _hint = new TimbnInteractionHint();
    }

    private static PlayerController? Player => MainGame.Instance != null ? MainGame.PlayerController : null;

    internal void Tick()
    {
        if (!TimbnGame.IsInGame || Player == null)
        {
            Stop();
            _hint.Hide();
            return;
        }

        if (_walking || _acting)
        {
            if (TimbnPlayer.GameHasControl(TakenControlType.ByUI))
                Stop();
            else if (TimbnClock.Minutes >= _until)
            {
                var finished = _acting;
                Stop();
                if (finished && _action.OnFinished is { } onFinished)
                    TimbnSafe.Run(onFinished, _owner.PluginLogger, "object action finished");
            }

            _hint.Hide();
            return;
        }

        if (!CanStart() || !TryFindObject(out var hintAt) || !AllowedByMod())
        {
            _hint.Hide();
            return;
        }

        _hint.Show(hintAt, LLBase.L(_action.HintKey));
        if (GameKey.Interaction.IsDownInGame())
            WalkOver();
    }

    internal void Stop()
    {
        _walk?.Dispose();
        _walk = null;
        if (!_walking && !_acting)
            return;

        var player = Player;
        if (player != null)
        {
            player.View.PlayerAnimation.SetState(AnimationState.Idle);
            player.PhysicalBody.SetDirectionLock(true);
        }

        _walking = false;
        _acting = false;
        _control?.Dispose();
        _control = null;
    }

    public void Dispose()
    {
        Stop();
        _hint.Hide();
    }

    private static bool CanStart() =>
        !TimbnUI.IsScreenFading && TimbnInput.PlayerHasControl && !TimbnInput.GameHasInteraction;

    private void WalkOver()
    {
        _walking = true;
        _until = TimbnClock.Minutes + _giveUpWalkMinutes;
        _control?.Dispose();
        _control = _owner.Player.TakeControl();
        _walk = TimbnPlayerSlide.Start(StandAt(TimbnPlayer.Position.y), Begin, _owner.PluginLogger);
    }

    private void Begin()
    {
        var player = Player;
        if (player == null)
            return;

        _walk = null;
        _walking = false;
        _acting = true;
        _until = TimbnClock.Minutes + _action.GameMinutes;
        player.PhysicalBody.SetFacingDirection(_action.Facing.ConvertToVector2XZ());
        player.PhysicalBody.SetDirectionLock(false);
        player.View.PlayerAnimation.SetState(_action.Animation);
        if (_action.OnStarted is { } onStarted)
            TimbnSafe.Run(onStarted, _owner.PluginLogger, "object action started");
    }

    private bool AllowedByMod()
    {
        if (_action.CanPerform is not { } canPerform)
            return true;

        var allowed = false;
        TimbnSafe.Run(() => allowed = canPerform(), _owner.PluginLogger, "object action can perform");
        return allowed;
    }

    private bool TryFindObject(out Vector3 hintAt)
    {
        hintAt = default;
        if (_mask == 0)
        {
            var layer = LayerMask.NameToLayer(_obstacleLayer);
            _mask = layer >= 0 ? 1 << layer : Physics.AllLayers;
        }

        var player = TimbnPlayer.Position;
        var stand = StandAt(player.y);
        var count = Physics.OverlapSphereNonAlloc(player, _searchRadius, _hits, _mask, QueryTriggerInteraction.Collide);
        for (var i = 0; i < count; i++)
        {
            var hit = _hits[i];
            if (!IsPartOfObject(hit.transform) || TimbnWorld.FlatDistance(hit.ClosestPoint(stand), stand) > _objectReach)
                continue;

            var bounds = hit.bounds;
            hintAt = new Vector3(bounds.center.x, bounds.max.y + _hintAbove, bounds.center.z);
            return true;
        }

        return false;
    }

    private Vector3 StandAt(float y) => new(_action.Stand.x, y, _action.Stand.z);

    private bool IsPartOfObject(Transform hit)
    {
        for (var current = hit; current != null; current = current.parent)
        {
            if (current.name.StartsWith(_action.ObjectName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
