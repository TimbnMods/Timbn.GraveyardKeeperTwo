using BepInEx.Configuration;
using BepInEx.Logging;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSettingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "TimbnTests", Guid.NewGuid().ToString("N"));
    private readonly ManualLogSource _logger = new("Tests");
    private readonly List<LogEventArgs> _logged = [];
    private readonly ConfigFile _config;
    private readonly ConfigEntry<bool> _enabled;

    public TimbnSettingsTests()
    {
        Directory.CreateDirectory(_folder);
        _config = new ConfigFile(Path.Combine(_folder, "test.cfg"), false) { SaveOnConfigSet = false };
        _enabled = _config.Bind("General", "Enabled", false, "A toggle.");
        _logger.LogEvent += (_, args) => _logged.Add(args);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void WhileSubscribesOnlyOnceTheSettingIsOn()
    {
        var subscribed = 0;
        var unsubscribed = 0;
        using var handle = TimbnSettings.While(_enabled, on => on, () => { subscribed++; return new TimbnUndo(() => unsubscribed++); }, _logger);

        Assert.Equal(0, subscribed);

        _enabled.Value = true;
        Assert.Equal(1, subscribed);
        Assert.Equal(0, unsubscribed);

        _enabled.Value = false;
        Assert.Equal(1, subscribed);
        Assert.Equal(1, unsubscribed);
    }

    [Fact]
    public void WhileSubscribesAtOnceWhenAlreadyOn()
    {
        _enabled.Value = true;
        var subscribed = 0;

        using var handle = TimbnSettings.While(_enabled, on => on, () => { subscribed++; return TimbnUndo.None; }, _logger);

        Assert.Equal(1, subscribed);
    }

    [Fact]
    public void AChangeWhileOnResubscribes()
    {
        var level = _config.Bind("General", "Level", 1, "A number.");
        List<int> seen = [];
        var dropped = 0;
        using var handle = TimbnSettings.While(level, value => value > 0, () => { seen.Add(level.Value); return new TimbnUndo(() => dropped++); }, _logger);

        level.Value = 2;
        level.Value = 3;

        Assert.Equal([1, 2, 3], seen);
        Assert.Equal(2, dropped);
    }

    [Fact]
    public void DisposingUnsubscribesAndStopsFollowingTheSetting()
    {
        _enabled.Value = true;
        var subscribed = 0;
        var unsubscribed = 0;
        var handle = TimbnSettings.While(_enabled, on => on, () => { subscribed++; return new TimbnUndo(() => unsubscribed++); }, _logger);

        handle.Dispose();
        handle.Dispose();
        _enabled.Value = false;
        _enabled.Value = true;

        Assert.Equal(1, subscribed);
        Assert.Equal(1, unsubscribed);
    }

    [Fact]
    public void ASubscribeThatThrowsIsLoggedAndTheSettingKeepsWorking()
    {
        var attempts = 0;
        using var handle = TimbnSettings.While(_enabled, on => on, () => { attempts++; throw new InvalidOperationException("no hook"); }, _logger);

        _enabled.Value = true;
        _enabled.Value = false;
        _enabled.Value = true;

        Assert.Equal(2, attempts);
        Assert.Equal(2, _logged.Count);
        Assert.All(_logged, entry => Assert.Equal(LogLevel.Error, entry.Level));
        Assert.Contains("no hook", _logged[0].Data.ToString());
    }

    [Fact]
    public void AnUnsubscribeThatThrowsIsLogged()
    {
        _enabled.Value = true;
        using var handle = TimbnSettings.While(_enabled, on => on, () => new TimbnUndo(() => throw new InvalidOperationException("stuck")), _logger);

        _enabled.Value = false;

        var entry = Assert.Single(_logged);
        Assert.Contains("stuck", entry.Data.ToString());
    }

    [Fact]
    public void ChangedHandsTheNewValueToTheHandler()
    {
        var level = _config.Bind("General", "Level", 1, "A number.");
        List<int> seen = [];
        using var handle = TimbnSettings.Changed(level, value => seen.Add(value), _logger);

        level.Value = 5;
        level.Value = 8;

        Assert.Equal([5, 8], seen);
    }

    [Fact]
    public void ChangedStopsAfterDispose()
    {
        var calls = 0;
        var handle = TimbnSettings.Changed(_enabled, _ => calls++, _logger);

        handle.Dispose();
        _enabled.Value = true;

        Assert.Equal(0, calls);
    }

    [Fact]
    public void AChangeHandlerThatThrowsIsLogged()
    {
        using var handle = TimbnSettings.Changed(_enabled, _ => throw new InvalidOperationException("bad change"), _logger);

        _enabled.Value = true;

        var entry = Assert.Single(_logged);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("bad change", entry.Data.ToString());
    }
}
