namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Ties a config entry to the change it makes, applying and reverting as the entry changes.</summary>
internal static class TimbnSettings
{
    internal static IDisposable Toggle<T>(ConfigEntry<T> entry, Func<T, bool> isOn, Action apply, Action? revert, ManualLogSource logger)
    {
        var what = $"{nameof(TimbnSettings)}|{entry.Definition}";
        return While(entry, isOn, () => Applied(apply, revert, logger, what), logger);
    }

    internal static IDisposable While<T>(ConfigEntry<T> entry, Func<T, bool> isOn, Func<IDisposable> subscribe, ManualLogSource logger)
    {
        var binding = new Subscription<T>(entry, isOn, subscribe, logger);
        binding.Start();
        return binding;
    }

    internal static IDisposable Changed<T>(ConfigEntry<T> entry, Action<T> handler, ManualLogSource logger)
    {
        void changed(object sender, EventArgs args) =>
            TimbnSafe.Run(() => handler(entry.Value), logger, $"{nameof(TimbnSettings)}|The change handler of {entry.Definition}");

        entry.SettingChanged += changed;
        return new TimbnUndo(() => entry.SettingChanged -= changed);
    }

    private static IDisposable Applied(Action apply, Action? revert, ManualLogSource logger, string what)
    {
        var applied = false;
        void run()
        {
            applied = true;
            TimbnSafe.Run(apply, logger, $"{what} threw while applying its change, it");
        }

        var gameStarted = TimbnGameEvents.GameStarted(run);
        if (TimbnGame.IsInGame)
            run();

        return new TimbnUndo(() =>
        {
            gameStarted.Dispose();
            if (applied || TimbnGame.IsInGame)
                TimbnSafe.Run(revert, logger, $"{what} threw while reverting its change, it");
        });
    }

    private sealed class Subscription<T> : IDisposable
    {
        private readonly ConfigEntry<T> _entry;
        private readonly Func<T, bool> _isOn;
        private readonly Func<IDisposable> _subscribe;
        private readonly ManualLogSource _logger;
        private IDisposable? _current;
        private bool _started;

        public Subscription(ConfigEntry<T> entry, Func<T, bool> isOn, Func<IDisposable> subscribe, ManualLogSource logger)
        {
            _entry = entry;
            _isOn = isOn;
            _subscribe = subscribe;
            _logger = logger;
        }

        public void Start()
        {
            _started = true;
            _entry.SettingChanged += OnSettingChanged;
            Sync();
        }

        public void Dispose()
        {
            if (!_started)
                return;

            _started = false;
            _entry.SettingChanged -= OnSettingChanged;
            Drop();
        }

        private void OnSettingChanged(object sender, EventArgs args)
        {
            Drop();
            Sync();
        }

        private void Sync()
        {
            if (!_isOn(_entry.Value))
                return;

            try
            {
                _current = _subscribe();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(TimbnSettings)}|{_entry.Definition} threw while subscribing: {ex}");
            }
        }

        private void Drop()
        {
            var current = _current;
            _current = null;
            TimbnSafe.Run(() => current?.Dispose(), _logger, $"{nameof(TimbnSettings)}|{_entry.Definition} threw while unsubscribing, it");
        }
    }
}
