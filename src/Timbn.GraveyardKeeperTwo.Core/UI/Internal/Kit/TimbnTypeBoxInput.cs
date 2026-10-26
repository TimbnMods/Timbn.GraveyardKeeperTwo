using LazyBearTechnology;
using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// The game reads its keys while a box has the caret, so typing a letter would also open a menu, close the page or move
// the keeper. Its input is off while the box has the caret and the keyboard is in use. With a gamepad the game's input
// stays on, so its buttons can still be reached. The player can pick up the keyboard after selecting the box with a
// gamepad, so which one is in use is checked every frame while the box is selected, before the game's own scripts read
// their keys. The game may learn of the switch only after it has read the key, so a keyboard key holds the input on that
// frame too, a typed letter or one that prints nothing, such as Esc. While the input is held the game stops looking at the gamepad, so a gamepad button or a push of a stick lets
// the input go and the game switches back on it.
[DefaultExecutionOrder(-10000)]
internal sealed class TimbnTypeBoxInput : MonoBehaviour
{
    private static readonly System.Reflection.FieldInfo? _gamepad = AccessTools.Field(typeof(LazyInput), "gamepad");
    private static readonly System.Reflection.FieldInfo? _player = AccessTools.Field(typeof(GamepadController), "rewiredPlayer");
    private static readonly System.Reflection.FieldInfo? _leftStick = AccessTools.Field(typeof(GamepadController), "stick");
    private static readonly System.Reflection.FieldInfo? _rightStick = AccessTools.Field(typeof(GamepadController), "rightStick");
    private static readonly System.Reflection.MethodInfo? _readStick = AccessTools.Method(typeof(Stick), nameof(Stick.Update));

    private bool _selected;
    private bool _holding;
    private int? _caretAfterEscape;
    private TMP_InputField? _field;

    // Hooked up as soon as it is set, since a prompt's box can take the caret in its first Start, before this one's.
    public TMP_InputField? Field
    {
        get => _field;
        set
        {
            _field = value;
            if (value == null)
                return;

            // Esc works like a gamepad's B. It lets the game's input go, so the page backs out if it takes Back, and it never
            // touches the text. The box itself would put the text back and drop the caret, so it keeps both instead.
            value.restoreOriginalTextOnEscape = false;
            value.onEndEdit.AddListener(_ =>
            {
                if (value.wasCanceled)
                    _caretAfterEscape = value.caretPosition;
            });

            value.onSelect.AddListener(_ =>
            {
                _selected = true;
                Follow();
            });
            value.onDeselect.AddListener(_ =>
            {
                _selected = false;
                Follow();
            });
        }
    }

    private void Update()
    {
        if (_caretAfterEscape is { } caret)
            RefocusAfterEscape(caret);

        if (_selected)
            Follow();
    }

    // A page that took Back is closing by now, so the caret only goes back on one that is still shown.
    private void RefocusAfterEscape(int caret)
    {
        _caretAfterEscape = null;
        if (_field == null || GetComponentInParent<TimbnPageWindow>() is not { IsShown: true })
            return;

        _field.Select();
        _field.ActivateInputField();
        _field.caretPosition = caret;
    }

    private void OnDisable()
    {
        _selected = false;
        Follow();
    }

    private void Follow()
    {
        var hold = _selected && (!LazyInput.IsGamepadActive || KeyboardPressed()) && !GamepadPressed() && !Input.GetKeyDown(KeyCode.Escape);
        if (hold == _holding)
            return;

        _holding = hold;
        if (hold)
            TimbnInputHold.Acquire();
        else
            TimbnInputHold.Release();
    }

    // A typed letter, or a key that prints nothing such as Esc or an arrow, which the game would also read as one of its own.
    private static bool KeyboardPressed() =>
        Input.inputString.Length > 0
        || (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2) && !GamepadButtonDown());

    private static bool GamepadButtonDown()
    {
        try
        {
            return _gamepad?.GetValue(null) is GamepadController gamepad && gamepad.AnyKeyDownExceptSticks();
        }
        catch (Exception)
        {
            return false;
        }
    }

    // The game's own checks read the gamepad straight from its input library, so they still work while its input is off. The
    // sticks are read the way the game's gamepad update reads them, which it skips while its input is off.
    private bool GamepadPressed()
    {
        if (!_holding || _gamepad?.GetValue(null) is not GamepadController gamepad)
            return false;

        if (GamepadButtonDown())
            return true;

        try
        {
            var player = _player?.GetValue(gamepad);
            return player != null && (Pushed(_leftStick, gamepad, player) || Pushed(_rightStick, gamepad, player));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool Pushed(System.Reflection.FieldInfo? field, GamepadController gamepad, object player)
    {
        if (field?.GetValue(gamepad) is not Stick stick || _readStick == null)
            return false;

        _readStick.Invoke(stick, [player]);
        return stick.HasDirection;
    }
}
