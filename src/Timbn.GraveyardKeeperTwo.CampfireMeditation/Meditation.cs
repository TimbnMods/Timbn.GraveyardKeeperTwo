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
    private IDisposable? _control;
    private IDisposable? _fastTime;
    private Vector3 _fire;
    private bool _meditating;
    private bool _applied;
    private IDisposable? _fade;
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
        if (!TimbnGame.IsInGame || MainGame.PlayerController == null)
        {
            Stop();
            _hint.Hide();
            return;
        }

        if (TimbnPlayer.GameHasControl(TakenControlType.ByUI))
            Stop();

        if (_meditating && TimbnPlayer.IsTired && CanGetUp())
            GetUp();

        if (_meditating)
            KeepMeditating();

        if (_meditating ? !CanGetUp() : !CanSitDown())
        {
            _hint.Hide();
            return;
        }

        _hint.Show(_fire + Vector3.up * _hintHeight, LLBase.L(_meditating ? _getUpKey : _meditateKey));
        if (!GameKey.Interaction.IsDownInGame())
            return;

        if (_meditating)
            GetUp();
        else
            SitDown();
    }

    public void Stop()
    {
        _fade?.Dispose();
        _fade = null;
        if (_meditating || _applied)
            Restore();

        _meditating = false;
    }

    private static bool CanGetUp() =>
        !TimbnUI.IsScreenFading && TimbnInput.HotkeysEnabled;

    private bool CanSitDown() =>
        !TimbnUI.IsScreenFading
        && TimbnInput.PlayerHasControl
        && !TimbnInput.GameHasInteraction
        && !TimbnPlayer.IsTired
        && FindFire();

    private bool FindFire() => _campfires.TryFindNear(TimbnPlayer.Position, out _fire);

    private void SitDown()
    {
        _meditating = true;
        _control?.Dispose();
        _control = _plugin.Player.TakeControl();
        _lastTimeOfDay = TimbnClock.TimeOfDay;
        _insanityRemoved = 0f;
        _fade?.Dispose();
        _fade = _plugin.UI.FadeThrough(Begin);
    }

    private void GetUp()
    {
        _meditating = false;
        _fade?.Dispose();
        _fade = _plugin.UI.FadeThrough(Restore);
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
        _fastTime = _plugin.Clock.MultiplyGameSpeed(PluginConfig.TimeSpeed.Value);
        Plugin.Logger.LogInfo($"Meditating by the fire at {_fire} in {TimbnPlayer.ZoneId ?? "no zone"}, time at x{PluginConfig.TimeSpeed.Value:0.#}.");
    }

    private void Restore()
    {
        var player = Player;
        if (player != null)
            player.PhysicalBody.SetDirectionLock(true);

        _control?.Dispose();
        _control = null;
        _fastTime?.Dispose();
        _fastTime = null;
        if (!_applied)
            return;

        _applied = false;
        MeditationLighting.End();
        Plugin.Logger.LogInfo("Stopped meditating.");
    }

    private void KeepMeditating()
    {
        var now = TimbnClock.TimeOfDay;
        var passed = TimbnClock.DayFractionBetween(_lastTimeOfDay, now);
        _lastTimeOfDay = now;
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

        var amount = Mathf.Min(passed * PluginConfig.InsanityPerDay.Value, left, TimbnPlayer.GetResource(_insanity));
        if (amount <= 0f)
            return;

        insanity.Add(-amount);
        _insanityRemoved += amount;
    }
}
