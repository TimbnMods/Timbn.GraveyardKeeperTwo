using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

internal sealed class Meditation
{
    private const string _meditateKey = "timbn_campfire_meditate";
    private const string _getUpKey = "timbn_campfire_get_up";
    private const string _insanity = "insanity";
    private const float _hintHeight = 1.2f;

    private readonly TimbnFrameworkPlugin _plugin;
    private readonly Campfires _campfires = new();
    private readonly TimbnInteractionHint _hint;
    private IDisposable? _hold;
    private IDisposable? _fastTime;
    private Vector3 _fire;
    private bool _meditating;
    private bool _applied;
    private int _version;
    private float _lastTimeOfDay;
    private float _insanityRemoved;

    public Meditation(TimbnFrameworkPlugin plugin)
    {
        _plugin = plugin;
        _hint = plugin.UI.CreateHint();
    }

    private static PlayerController? Player => MainGame.Instance != null ? MainGame.PlayerController : null;

    public void Tick()
    {
        var player = Player;
        var engine = player != null ? EnvironmentEngine.Instance : null;
        if (player == null || engine == null)
        {
            Stop();
            _hint.Hide();
            return;
        }

        if (!TimbnInput.PlayerHasControlExcept(TakenControlType.ByUI))
            Stop();

        if (_meditating)
            KeepMeditating(engine);

        if (TimbnUI.IsScreenFading || !TimbnInput.PlayerHasControl || (!_meditating && (TimbnInput.GameHasInteraction || !FindFire())))
        {
            _hint.Hide();
            return;
        }

        _hint.Show(_fire + Vector3.up * _hintHeight, LLBase.L(_meditating ? _getUpKey : _meditateKey));
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

    private bool FindFire() => _campfires.TryFindNear(TimbnPlayer.Position, out _fire);

    private void SitDown(PlayerController player, EnvironmentEngine engine)
    {
        _meditating = true;
        _hold?.Dispose();
        _hold = _plugin.Player.HoldStill();
        _lastTimeOfDay = engine.timeOfDay;
        _insanityRemoved = 0f;
        var version = ++_version;
        TimbnUI.FadeThrough(() =>
        {
            if (version == _version)
                Begin();
        });
    }

    private void GetUp()
    {
        _meditating = false;
        var version = ++_version;
        TimbnUI.FadeThrough(() =>
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
        _fastTime?.Dispose();
        _fastTime = _plugin.Clock.SetSpeed(PluginConfig.TimeSpeed.Value);
        Plugin.Logger.LogInfo($"Meditating by the fire at {_fire} in {Campfires.CurrentZone ?? "no zone"}, time at x{PluginConfig.TimeSpeed.Value:0.#}.");
    }

    private void Restore()
    {
        var player = Player;
        if (player != null)
            player.PhysicalBody.SetDirectionLock(true);

        _hold?.Dispose();
        _hold = null;
        _fastTime?.Dispose();
        _fastTime = null;
        if (!_applied)
            return;

        _applied = false;
        MeditationLighting.End();
        Plugin.Logger.LogInfo("Stopped meditating.");
    }

    private void KeepMeditating(EnvironmentEngine engine)
    {
        var passed = TimbnClock.Between(_lastTimeOfDay, engine.timeOfDay);
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
}
