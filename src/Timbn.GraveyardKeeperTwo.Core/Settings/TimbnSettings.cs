namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSettings
{
    internal static IDisposable Toggle<T>(ConfigEntry<T> entry, Func<T, bool> isOn, Action apply, Action? revert, ManualLogSource logger)
    {
        var binding = new Binding<T>(entry, isOn, apply, revert, logger);
        binding.Start();
        return binding;
    }

    internal static IDisposable While<T>(ConfigEntry<T> entry, Func<T, bool> isOn, Func<IDisposable> subscribe, ManualLogSource logger)
    {
        var binding = new Subscription<T>(entry, isOn, subscribe, logger);
        binding.Start();
        return binding;
    }

    internal static IDisposable Changed<T>(ConfigEntry<T> entry, Action<T> handler, ManualLogSource logger)
    {
        void changed(object sender, EventArgs args)
        {
            try
            {
                handler(entry.Value);
            }
            catch (Exception ex)
            {
                logger.LogError($"{nameof(TimbnSettings)}|The change handler of {entry.Definition} threw: {ex}");
            }
        }

        entry.SettingChanged += changed;
        return new TimbnUndo(() => entry.SettingChanged -= changed);
    }

    private sealed class Binding<T> : IDisposable
    {
        private readonly ConfigEntry<T> _entry;
        private readonly Func<T, bool> _isOn;
        private readonly Action _apply;
        private readonly Action? _revert;
        private readonly ManualLogSource _logger;
        private IDisposable? _gameStarted;
        private bool _applied;

        public Binding(ConfigEntry<T> entry, Func<T, bool> isOn, Action apply, Action? revert, ManualLogSource logger)
        {
            _entry = entry;
            _isOn = isOn;
            _apply = apply;
            _revert = revert;
            _logger = logger;
        }

        public void Start()
        {
            _entry.SettingChanged += OnSettingChanged;
            _gameStarted = TimbnGameEvents.GameStarted(Sync);
            if (TimbnGame.IsInGame)
                Sync();
        }

        public void Dispose()
        {
            if (_gameStarted == null)
                return;

            _entry.SettingChanged -= OnSettingChanged;
            _gameStarted.Dispose();
            _gameStarted = null;
            Revert();
        }

        private void Sync()
        {
            if (_isOn(_entry.Value))
                Apply();
            else
                Run(_revert);
        }

        private void OnSettingChanged(object sender, EventArgs args)
        {
            Revert();
            if (_isOn(_entry.Value) && TimbnGame.IsInGame)
                Apply();
        }

        private void Apply()
        {
            _applied = true;
            Run(_apply);
        }

        private void Revert()
        {
            if (!_applied && !TimbnGame.IsInGame)
                return;

            _applied = false;
            Run(_revert);
        }

        private void Run(Action? action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(TimbnSettings)}|{_entry.Definition} threw while applying its change: {ex}");
            }
        }
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
            try
            {
                current?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(TimbnSettings)}|{_entry.Definition} threw while unsubscribing: {ex}");
            }
        }
    }
}
