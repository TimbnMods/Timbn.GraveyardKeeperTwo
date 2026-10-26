namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>One line of a menu shown with <see cref="TimbnPluginUI.ShowMenu"/>.</summary>
/// <param name="label">The text of the line, already in the player's language.</param>
/// <param name="onPressed">Called after the menu closes when the player picks the line.</param>
/// <param name="enabled">False to show the line dimmed, so it cannot be picked.</param>
public sealed class TimbnMenuItem(string label, Action onPressed, bool enabled = true)
{
    /// <summary>The text of the line.</summary>
    public string Label { get; } = label;

    /// <summary>Called after the menu closes when the player picks the line.</summary>
    public Action OnPressed { get; } = onPressed;

    /// <summary>Whether the line can be picked.</summary>
    public bool Enabled { get; } = enabled;
}
