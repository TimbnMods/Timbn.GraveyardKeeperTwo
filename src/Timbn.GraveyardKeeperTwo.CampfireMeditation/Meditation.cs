using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

internal sealed class Meditation
{
    private const string _meditateKey = "timbn_campfire_meditate";

    // The campfire in the garden at the keeper's home, the only one there is to meditate at.
    private static readonly Vector3 _fire = new(-5.22f, 6.00f, 29.40f);

    private readonly TimbnFrameworkPlugin _plugin;
    private readonly TimbnInteractionSpot _spot;
    private IDisposable? _control;
    private IDisposable? _keyHints;
    private IDisposable? _fastTime;
    private bool _meditating;
    private bool _applied;
    private IDisposable? _fade;
    private float _lastTimeOfDay;
    private float _insanityRemoved;

    public Meditation(TimbnFrameworkPlugin plugin)
    {
        _plugin = plugin;
        _spot = plugin.Interactions.Register(_fire).AddPrimary(() => CanSitDown() ? plugin.Text.Get(_meditateKey) : string.Empty, SitDown);
    }

    public void Tick()
    {
        _spot.Radius = PluginConfig.Radius.Value;
        if (!TimbnGame.IsInGame || MainGame.PlayerController == null)
        {
            Stop();
            return;
        }

        if (TimbnPlayer.GameHasControl(TakenControlType.ByUI))
            Stop();

        if (_meditating && TimbnPlayer.IsTired && CanGetUp())
            GetUp();

        if (!_meditating)
            return;

        KeepMeditating();
        if (CanGetUp() && GameKey.Back.IsDownInGame())
            GetUp();
    }

    public void Stop()
    {
        HideKeyHints();
        _fade?.Dispose();
        _fade = null;
        if (_meditating || _applied)
            Restore();

        _meditating = false;
    }

    private static bool CanGetUp() =>
        !TimbnUI.IsScreenFading && TimbnInput.HotkeysEnabled;

    private bool CanSitDown() => !_meditating && !TimbnPlayer.IsTired;

    private void SitDown()
    {
        _meditating = true;
        _keyHints = _plugin.UI.ShowKeyHints(new TimbnKeyHint(GameKey.Back, _plugin.Text.Key(TimbnGameIds.Texts.HintExit)));
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
        HideKeyHints();
        _fade?.Dispose();
        _fade = _plugin.UI.FadeThrough(Restore);
    }

    private void Begin()
    {
        if (!TimbnPlayer.Face(_fire))
            return;

        _applied = true;
        MeditationLighting.Begin();
        _fastTime?.Dispose();
        _fastTime = _plugin.Clock.MultiplyGameSpeed(PluginConfig.TimeSpeed.Value);
        Plugin.Logger.LogInfo($"Meditating by the fire at {_fire} in {TimbnPlayer.ZoneId ?? "no zone"}, time at x{PluginConfig.TimeSpeed.Value:0.#}.");
    }

    private void Restore()
    {
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

        TimbnPlayer.AddEnergy(passed * PluginConfig.EnergyPerDay.Value);
        CalmDown(passed);
    }

    private void CalmDown(float passed)
    {
        var left = PluginConfig.MaxInsanity.Value - _insanityRemoved;
        var amount = Mathf.Min(passed * PluginConfig.InsanityPerDay.Value, left, TimbnPlayer.Insanity);
        if (amount > 0f && TimbnPlayer.AddInsanity(-amount))
            _insanityRemoved += amount;
    }

    private void HideKeyHints()
    {
        _keyHints?.Dispose();
        _keyHints = null;
    }
}
