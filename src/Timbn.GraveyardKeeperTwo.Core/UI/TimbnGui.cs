namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Draws the IMGUI a plugin registered with UI.Gui and UI.Window, and offers the shared bits of a debug window.</summary>
public static class TimbnGui
{
    private static readonly List<Entry> _entries = [];
    private static GUIStyle? _header;

    /// <summary>Draws a bold label, for the heading of a section in a debug window.</summary>
    /// <param name="text">The heading.</param>
    public static void Header(string text)
    {
        _header ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        GUILayout.Label(text, _header);
    }

    internal static IDisposable Add(Action draw, Func<bool> isRunning, ManualLogSource logger)
    {
        var entry = new Entry(draw, isRunning, logger);
        _entries.Add(entry);
        return new TimbnUndo(() => _entries.Remove(entry));
    }

    internal static void Draw()
    {
        if (_entries.Count == 0)
            return;

        foreach (var entry in _entries.ToList())
        {
            if (!entry.IsRunning())
                continue;

            if (!TimbnSafe.Run(entry.Draw, entry.Logger, $"{nameof(TimbnGui)}|A GUI handler was stopped because it"))
                _entries.Remove(entry);
        }
    }

    private sealed class Entry(Action draw, Func<bool> isRunning, ManualLogSource logger)
    {
        public Action Draw { get; } = draw;

        public Func<bool> IsRunning { get; } = isRunning;

        public ManualLogSource Logger { get; } = logger;
    }
}
