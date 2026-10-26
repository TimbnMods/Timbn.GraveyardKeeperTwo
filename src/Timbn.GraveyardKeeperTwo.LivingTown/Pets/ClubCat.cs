namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class ClubCat
{
    private const string _petKey = "timbn_living_town_pet";
    private const double _petMinutes = 10;

    // The cat on the crate by the club, and the spot in front of the crate the keeper pets it from.
    private static readonly Vector3 _cat = new(-104.29f, 0f, -337.79f);
    private static readonly Vector3 _stand = new(-104.36f, 0f, -338.20f);

    private readonly TimbnFrameworkPlugin _plugin;
    private IDisposable? _control;
    private IDisposable? _walk;
    private IDisposable? _animation;
    private double _until;

    public ClubCat(TimbnFrameworkPlugin plugin)
    {
        _plugin = plugin;
        plugin.Interactions.Register(_cat).AddPrimary(plugin.Text.Key(_petKey), WalkOver);
    }

    public void Tick()
    {
        if (_animation == null)
            return;

        if (!TimbnGame.IsInGame || TimbnPlayer.GameHasControl(TakenControlType.ByUI) || TimbnClock.Minutes >= _until)
            Stop();
    }

    public void Stop()
    {
        _walk?.Dispose();
        _walk = null;
        _animation?.Dispose();
        _animation = null;
        _control?.Dispose();
        _control = null;
    }

    private void WalkOver()
    {
        Stop();
        _control = _plugin.Player.TakeControl();
        _walk = _plugin.Player.WalkTo(new Vector3(_stand.x, TimbnPlayer.Position.y, _stand.z), Pet);
    }

    private void Pet(bool arrived)
    {
        _walk = null;
        if (!arrived || !TimbnPlayer.Face(_cat))
        {
            Stop();
            return;
        }

        _until = TimbnClock.Minutes + _petMinutes;
        _animation = _plugin.Player.HoldAnimation(AnimationState.WorkHands);
    }
}
