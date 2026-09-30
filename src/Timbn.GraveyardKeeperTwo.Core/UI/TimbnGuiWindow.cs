namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// An IMGUI box for a plugin's debug or cheat menu, opened and closed with a key from the plugin's config. Core
/// draws the box, its title line with the key to close it, and a "load a save" note while no save is loaded, so
/// the plugin only draws what goes inside. Get one from the plugin's UI.Window.
/// </summary>
/// <example>
/// <code>
/// var menu = UI.Window("Timbn Cheats", DrawCheats, PluginConfig.ToggleMenu);
/// menu.Area = new Rect(20, 20, 420, 640);
/// </code>
/// </example>
public sealed class TimbnGuiWindow
{
    private readonly string _title;
    private readonly Action _draw;
    private readonly ConfigEntry<KeyboardShortcut>? _toggle;

    internal TimbnGuiWindow(string title, Action draw, ConfigEntry<KeyboardShortcut>? toggle)
    {
        _title = title;
        _draw = draw;
        _toggle = toggle;
    }

    /// <summary>Whether the window is on screen. The toggle key flips it, and the plugin can set it too.</summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Where the box is drawn and how big it is, in screen pixels. With <see cref="AnchorRight"/> the x is measured
    /// from the right edge instead of the left.
    /// </summary>
    public Rect Area { get; set; } = new(20, 20, 420, 640);

    /// <summary>Measures the box's x from the right edge of the screen, for a window that sits on the right.</summary>
    public bool AnchorRight { get; set; }

    /// <summary>
    /// Whether the contents need a loaded save. While on, the box shows only its title and a note asking the player
    /// to load a save until one is loaded. Turn it off for a window that also works on the main menu.
    /// </summary>
    public bool InGameOnly { get; set; } = true;

    internal void Tick()
    {
        if (_toggle != null && _toggle.Value.IsDown())
            Visible = !Visible;
    }

    internal void Draw()
    {
        if (!Visible)
            return;

        var area = AnchorRight ? new Rect(Screen.width - Area.x - Area.width, Area.y, Area.width, Area.height) : Area;
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label(_toggle is null ? _title : $"{_title}  (press {_toggle.Value} to close)");
        if (InGameOnly && !TimbnGame.IsInGame)
            GUILayout.Label("Load a save to use these.");
        else
            _draw();

        GUILayout.EndArea();
    }
}
