using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Waits for the next key press for a key setting, the way the game's controls window records a binding. The
/// game's input is off while it waits, so the key does not also open a menu or close the window. Modifier keys
/// held at the time are kept with it, Backspace clears the setting and Esc leaves it as it was. Started from a gamepad,
/// the game's input stays on so the gamepad's back button can cancel, and the box asks for a keyboard key, since a
/// setting's key is always a keyboard one.
/// </summary>
internal sealed class TimbnKeyCapture : MonoBehaviour
{
    private static readonly KeyCode[] _modifiers =
    [
        KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftAlt, KeyCode.RightAlt,
    ];

    private static readonly KeyCode[] _keys = [.. Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().Where(IsBindable)];

    private static TimbnKeyCapture? _instance;

    private Action<KeyboardShortcut>? _set;
    private TimbnKeyWidget? _widget;
    private bool _holdsInput;
    private int _startFrame;

    /// <summary>The one capture every key control shares, made the first time one is used.</summary>
    internal static TimbnKeyCapture Instance
    {
        get
        {
            if (_instance == null)
            {
                var host = new GameObject(nameof(TimbnKeyCapture));
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<TimbnKeyCapture>();
            }

            return _instance;
        }
    }

    internal bool IsCapturing => _set != null;

    internal static string KeyText(KeyboardShortcut key) => key.MainKey == KeyCode.None ? TimbnUiText.NoKey : key.ToString();

    internal void Begin(Action<KeyboardShortcut> set, TimbnKeyWidget widget)
    {
        Cancel();
        _set = set;
        _widget = widget;
        _startFrame = Time.frameCount;
        _holdsInput = !LazyInput.IsGamepadActive;
        if (_holdsInput)
            TimbnInputHold.Acquire();

        widget.Show(_holdsInput ? TimbnUiText.PressKey : TimbnUiText.PressKeyboardKey, recording: true);
    }

    internal void Cancel() => End();

    /// <summary>Stops a capture a closing window left waiting, when one has been made at all.</summary>
    internal static bool CancelAny()
    {
        if (_instance is not { IsCapturing: true } capture)
            return false;

        capture.Cancel();
        return true;
    }

    private void Update()
    {
        // The click that started the capture is still down this frame and must not be read as the new key.
        if (_set == null || Time.frameCount == _startFrame)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            End();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            Finish(KeyboardShortcut.Empty);
            return;
        }

        foreach (var key in _keys)
        {
            if (!Input.GetKeyDown(key))
                continue;

            var held = _modifiers.Where(modifier => modifier != key && Input.GetKey(modifier)).ToArray();
            Finish(new KeyboardShortcut(key, held));
            return;
        }
    }

    // The capture ends before the set runs, so the input hold is let go even when the set throws.
    private void Finish(KeyboardShortcut key)
    {
        var set = _set!;
        var widget = _widget;
        End();
        set(key);
        widget?.Refresh();
    }

    private void OnDisable() => End();

    private void End()
    {
        if (_set == null)
            return;

        _set = null;
        if (_holdsInput)
        {
            _holdsInput = false;
            TimbnInputHold.Release();
        }

        _widget?.Refresh();
        _widget = null;
    }

    private static bool IsBindable(KeyCode key) =>
        key != KeyCode.None && key != KeyCode.Escape && key != KeyCode.Backspace && key < KeyCode.JoystickButton0
        && key is not (KeyCode.Mouse0 or KeyCode.Mouse1) && !_modifiers.Contains(key);
}
