namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Where a button that a key of the page's is tied to goes while a gamepad is in use.</summary>
public enum TimbnGamepadPlacement
{
    /// <summary>The button stays where it is placed and turns into the key's hint, the key's glyph and the label with no button art, the way the game's own dialog and fight windows do it. The default.</summary>
    Inline,

    /// <summary>The button goes and its key shows as a hint under the window, the way the game's vendor window does it for its icon buttons. For a button with no text of its own.</summary>
    Bottom,
}
