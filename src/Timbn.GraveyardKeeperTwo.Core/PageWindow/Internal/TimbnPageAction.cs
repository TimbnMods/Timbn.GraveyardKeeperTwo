using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>An action a page tied to one of the four keys of a gamepad's face, with a button of its own or with the key alone.</summary>
internal sealed class TimbnPageAction
{
    private readonly TimbnBinding<bool> _disabled;
    private readonly string _label;

    // A button the page placed itself or that the footer made, so the key presses that button.
    public TimbnPageAction(TimbnFaceButton face, TimbnButton button, TimbnGamepadPlacement placement)
    {
        Face = face;
        Button = button;
        Placement = placement;
        _label = string.Empty;
        Run = button.Press;
    }

    // The key alone, with its hint under the window and no button.
    public TimbnPageAction(TimbnFaceButton face, string label, Action run, TimbnBinding<bool> disabled, Action? holdRun, string holdLabel)
    {
        Face = face;
        _label = label;
        Run = run;
        _disabled = disabled;
        HoldRun = holdRun;
        HoldLabel = holdLabel;
        Placement = TimbnGamepadPlacement.Bottom;
    }

    public TimbnFaceButton Face { get; }

    public TimbnButton? Button { get; }

    public TimbnGamepadPlacement Placement { get; }

    public Action Run { get; }

    public Action? HoldRun { get; }

    public string HoldLabel { get; } = string.Empty;

    public string Label => Button != null ? Button.Label : _label;

    public bool Enabled => Button != null ? Button.Pressable : !_disabled.Current;

    // A button's hint shows while the button can be seen, dimmed when it cannot be pressed, and a key alone shows its hint while it is enabled.
    public bool Shown => Button != null ? Button.Reachable : Enabled;

    // A is Select, B is Back, X is Action (F on the keyboard) and Y is the save slot's import key.
    public GameKey Key => Face switch
    {
        TimbnFaceButton.Primary => GameKey.Select,
        TimbnFaceButton.Secondary => GameKey.Back,
        TimbnFaceButton.PrimarySpecial => GameKey.Action,
        _ => GameKey.SaveImport,
    };
}

/// <summary>The four buttons of a gamepad's face that a page can tie an action to. On a keyboard they are Space, Esc, F and a fourth key.</summary>
public enum TimbnFaceButton
{
    /// <summary>A, the confirm button.</summary>
    Primary,

    /// <summary>B, the back button.</summary>
    Secondary,

    /// <summary>X.</summary>
    PrimarySpecial,

    /// <summary>Y.</summary>
    SecondarySpecial,
}
