namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The per frame handlers of one plugin for one Unity phase, run while a save is loaded and stopped after a throw.</summary>
internal sealed class TimbnFrameHandlers
{
    private readonly List<Entry> _entries = [];
    private readonly ManualLogSource _logger;
    private readonly string _name;
    private bool _running;
    private bool _dirty;

    internal TimbnFrameHandlers(ManualLogSource logger, string name)
    {
        _logger = logger;
        _name = name;
    }

    internal IDisposable Add(Action handler, float interval, bool whilePaused)
    {
        var entry = new Entry(handler, Math.Max(0f, interval), whilePaused, Remove);
        _entries.Add(entry);
        return entry;
    }

    internal void Run()
    {
        if (_entries.Count == 0 || !TimbnGame.IsInGame)
            return;

        var now = Time.time;
        var paused = MainGame.IsGamePaused;
        var count = _entries.Count;
        _running = true;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var entry = _entries[i];
                if (entry.Removed || now < entry.Next || (paused && !entry.WhilePaused))
                    continue;

                entry.Next = now + entry.Interval;
                if (TimbnSafe.Run(entry.Handler, _logger, $"{nameof(TimbnFrameHandlers)}|{_name} handler {TimbnSafe.Describe(entry.Handler)} was stopped because it"))
                    continue;

                entry.Removed = true;
                _dirty = true;
            }
        }
        finally
        {
            _running = false;
            if (_dirty)
            {
                _entries.RemoveAll(entry => entry.Removed);
                _dirty = false;
            }
        }
    }

    private void Remove(Entry entry)
    {
        entry.Removed = true;
        if (_running)
            _dirty = true;
        else
            _entries.Remove(entry);
    }

    private sealed class Entry : IDisposable
    {
        private readonly Action<Entry> _remove;

        public Entry(Action handler, float interval, bool whilePaused, Action<Entry> remove)
        {
            Handler = handler;
            Interval = interval;
            WhilePaused = whilePaused;
            _remove = remove;
        }

        public Action Handler { get; }

        public float Interval { get; }

        public bool WhilePaused { get; }

        public float Next { get; set; }

        public bool Removed { get; set; }

        public void Dispose()
        {
            if (!Removed)
                _remove(this);
        }
    }
}
