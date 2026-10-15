namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The per frame handlers of one plugin for one Unity phase, run while a save is loaded. A mod's handler is stopped
/// after a throw, and Core's own keeps running, since Core is never reloaded and a stopped tick would be gone until the
/// game restarts.
/// </summary>
internal sealed class TimbnFrameHandlers
{
    // A kept handler waits this long after a throw, so a lasting fault logs once a second instead of every frame.
    private const float _retryDelay = 1f;

    private readonly List<Entry> _entries = [];
    private readonly ManualLogSource _logger;
    private readonly string _name;
    private readonly bool _keep;
    private bool _running;
    private bool _dirty;

    internal TimbnFrameHandlers(ManualLogSource logger, string name, bool keep = false)
    {
        _logger = logger;
        _name = name;
        _keep = keep;
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
                var outcome = _keep ? "keeps running after it" : "was stopped because it";
                if (TimbnSafe.Run(entry.Handler, _logger, $"{nameof(TimbnFrameHandlers)}|{_name} handler {TimbnSafe.Describe(entry.Handler)} {outcome}"))
                    continue;

                if (_keep)
                {
                    entry.Next = now + Math.Max(entry.Interval, _retryDelay);
                    continue;
                }

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
