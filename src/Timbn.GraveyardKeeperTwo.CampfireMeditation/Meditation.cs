using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

internal sealed class Meditation
{
    private const string _meditateKey = "timbn_campfire_meditate";
    private const string _getUpKey = "timbn_campfire_get_up";
    private const string _insanity = "insanity";
    private static readonly TakenControlType[] _windows = [TakenControlType.ByUI];

    private readonly Campfires _campfires = new();
    private readonly CampfireHint _hint = new();
    private Vector3 _fire;
    private bool _meditating;
    private bool _applied;
    private int _version;
    private float _lastTimeOfDay;
    private float _insanityRemoved;

    private static PlayerController? Player => MainGame.Instance != null ? MainGame.PlayerController : null;

    public void Tick()
    {
        var player = TimbnGame.IsInGame ? Player : null;
        var engine = player != null ? EnvironmentEngine.Instance : null;
        if (player == null || engine == null)
        {
            Stop();
            _hint.Hide();
            return;
        }

        if (!player.IsControlsEnabledExcept(_windows))
            Stop();

        if (_meditating)
            KeepMeditating(engine);

        if (IsFading() || !player.IsControlsEnabled || (!_meditating && (GameHasInteraction(player) || !FindFire())) || LazyWindowsStackController.ActiveWindow != null)
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
            SitDown(player, engine);
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

    private void SitDown(PlayerController player, EnvironmentEngine engine)
    {
        _meditating = true;
        MovementLock.Hold(player.PhysicalBody);
        _lastTimeOfDay = engine.timeOfDay;
        _insanityRemoved = 0f;
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
        var player = Player;
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
        var player = Player;
        if (player != null)
            player.PhysicalBody.SetDirectionLock(true);

        MovementLock.Release();
        if (!_applied)
            return;

        _applied = false;
        if (MainGame.Instance != null)
            MainGame.UpdateManager?.SetTimeSpeedMultiplier(1f);
        MeditationLighting.End();
        Plugin.Logger.LogInfo("Stopped meditating.");
    }

    private void KeepMeditating(EnvironmentEngine engine)
    {
        var passed = engine.timeOfDay - _lastTimeOfDay;
        if (passed < 0f)
            passed += 1f;

        _lastTimeOfDay = engine.timeOfDay;
        if (!_applied || passed <= 0f)
            return;

        var energy = PlayerEnergyGameResSystem.GetSystem();
        if (energy != null && !energy.HasMax())
            energy.Add(passed * PluginConfig.EnergyPerDay.Value);

        CalmDown(passed);
    }

    private void CalmDown(float passed)
    {
        var insanity = PlayerInsanityGameResSystem.GetSystem();
        var left = PluginConfig.MaxInsanity.Value - _insanityRemoved;
        if (insanity == null || left <= 0f)
            return;

        var amount = Mathf.Min(passed * PluginConfig.InsanityPerDay.Value, left, MainGame.PlayerData.GetRes(_insanity));
        if (amount <= 0f)
            return;

        insanity.Add(-amount);
        _insanityRemoved += amount;
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
