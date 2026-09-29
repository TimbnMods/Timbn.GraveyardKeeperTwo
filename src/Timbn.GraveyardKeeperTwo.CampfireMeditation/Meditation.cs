using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

internal sealed class Meditation
{
    private const string _meditateKey = "timbn_campfire_meditate";
    private const string _getUpKey = "timbn_campfire_get_up";

    private readonly Campfires _campfires = new();
    private readonly CampfireHint _hint = new();
    private Vector3 _fire;
    private bool _meditating;
    private bool _applied;
    private int _version;
    private float _lastTimeOfDay;

    public void Tick()
    {
        var player = MainGame.PlayerController;
        var engine = EnvironmentEngine.Instance;
        if (!TimbnGame.IsInGame || player == null || engine == null)
        {
            Stop();
            _hint.Hide();
            return;
        }

        if (_meditating)
            KeepMeditating(player, engine);

        if (IsFading() || (!_meditating && (GameHasInteraction(player) || !FindFire())) || LazyWindowsStackController.ActiveWindow != null)
        {
            _hint.Hide();
            return;
        }

        _hint.Show(_fire, LLBase.L(_meditating ? _getUpKey : _meditateKey));
        if (!LazyInput.GetKeyDown(GameKey.Interaction))
            return;

        if (_meditating)
            GetUp();
        else
            SitDown(engine);
    }

    public void Stop()
    {
        _version++;
        if (_meditating || _applied)
            Restore();

        _meditating = false;
    }

    public void Dispose()
    {
        Stop();
        _hint.Hide();
    }

    private static bool IsFading()
    {
        var fade = LazyUI.Get<UIFade>();
        return fade != null && fade.IsFadeShowing;
    }

    private static bool GameHasInteraction(PlayerController player)
    {
        var interaction = player.PlayerInteractionComponent;
        return interaction != null && (interaction.HasWgoUnderInteraction || interaction.BigDropUnderInteraction != null);
    }

    private bool FindFire() => _campfires.TryFindNear(MainGame.PlayerData.position.Value, out _fire);

    private void SitDown(EnvironmentEngine engine)
    {
        _meditating = true;
        _lastTimeOfDay = engine.timeOfDay;
        var version = ++_version;
        FadeThrough(() =>
        {
            if (version == _version)
                Begin();
        });
    }

    private void GetUp()
    {
        _meditating = false;
        var version = ++_version;
        FadeThrough(() =>
        {
            if (version == _version)
                Restore();
        });
    }

    private void Begin()
    {
        var player = MainGame.PlayerController;
        if (player == null)
            return;

        _applied = true;
        MeditationLighting.Begin();
        var position = player.transform.position;
        var toFire = new Vector2(_fire.x - position.x, _fire.z - position.z);
        if (toFire.sqrMagnitude > 0.0001f)
            player.PhysicalBody.SetFacingDirection(toFire.normalized);

        player.PhysicalBody.SetDirectionLock(false);
        MainGame.UpdateManager.SetTimeSpeedMultiplier(PluginConfig.TimeSpeed.Value);
        Plugin.Logger.LogInfo($"Meditating by the fire, time at x{PluginConfig.TimeSpeed.Value:0.#}.");
    }

    private void Restore()
    {
        var player = MainGame.PlayerController;
        if (player != null)
        {
            player.PhysicalBody.SetDirectionLock(true);
            player.PhysicalBody.LockMovement(false);
        }

        if (!_applied)
            return;

        _applied = false;
        MainGame.UpdateManager?.SetTimeSpeedMultiplier(1f);
        MeditationLighting.End();
        Plugin.Logger.LogInfo("Stopped meditating.");
    }

    private void KeepMeditating(PlayerController player, EnvironmentEngine engine)
    {
        player.PhysicalBody.LockMovement(true);
        var passed = engine.timeOfDay - _lastTimeOfDay;
        if (passed < 0f)
            passed += 1f;

        _lastTimeOfDay = engine.timeOfDay;
        var energy = PlayerEnergyGameResSystem.GetSystem();
        if (_applied && passed > 0f && energy != null && !energy.HasMax())
            energy.Add(passed * PluginConfig.EnergyPerDay.Value);
    }

    private static void FadeThrough(Action whileBlack)
    {
        var fade = LazyUI.Get<UIFade>();
        if (fade == null)
            whileBlack();
        else
            fade.Fade(onInCompleted: whileBlack);
    }
}
