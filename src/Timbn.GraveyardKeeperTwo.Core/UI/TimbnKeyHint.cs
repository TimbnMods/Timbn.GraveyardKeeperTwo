using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>One key and what it does, for the bar of key hints at the bottom of the screen that <see cref="TimbnPluginUI.ShowKeyHints"/> shows.</summary>
/// <param name="key">The game's key, such as GameKey.Interaction, whose icon follows the keyboard or gamepad in use.</param>
/// <param name="label">What the key does, as text or a callback read every frame, such as <c>Text.Key("my_mod_get_up")</c>. Empty leaves the key out.</param>
public readonly struct TimbnKeyHint(GameKey key, TimbnBinding<string> label)
{
    /// <summary>The game's key.</summary>
    public GameKey Key { get; } = key;

    /// <summary>What the key does.</summary>
    public TimbnBinding<string> Label { get; } = label;
}
