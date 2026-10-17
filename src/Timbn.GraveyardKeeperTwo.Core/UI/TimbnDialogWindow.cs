using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Opens the game's own dialog window for a plugin, as a prompt (a text field is added under the header), a yes or no
/// question, or a message. The text field is a copy of the bug report window's title field (that window is never
/// opened). Whatever was added is taken out again whenever the dialog closes, so the game's other dialogs never see
/// it. Only one plugin dialog is open at a time.
/// </summary>
internal sealed class TimbnDialogWindow : IDisposable
{
    private const int _characterLimit = 200;

    private static TimbnDialogWindow? _open;

    private readonly UIDialogWindow _window;
    private readonly Action? _dismissed;
    private readonly ManualLogSource _logger;
    private TMP_InputField? _field;
    private GameObject? _marker;
    private bool _closed;

    private TimbnDialogWindow(UIDialogWindow window, Action? dismissed, ManualLogSource logger)
    {
        _window = window;
        _dismissed = dismissed;
        _logger = logger;
    }

    internal static IDisposable Prompt(string title, string current, string placeholder, Action<string> onSave, Action? onCancel, ManualLogSource logger)
    {
        var source = LazyUI.GetWindow<UIBugReportWindow>()?.GetComponentsInChildren<TMP_InputField>(true).FirstOrDefault(field => field.name == "Title");
        if (source == null || Begin(onCancel, logger, out var content) is not { } dialog)
            return TimbnUndo.None;

        var copy = UnityEngine.Object.Instantiate(source.gameObject, content);
        var field = copy.GetComponent<TMP_InputField>();
        dialog._field = field;
        var buttons = new List<UIDialogWindowData.ButtonData>
        {
            new(() => dialog.Save(onSave), LLBase.L("btn_ok"), null, true, GameKey.Select),
            new(dialog.Dismiss, LLBase.L("btn_cancel"), null, true, GameKey.Back),
        };
        dialog.Show(new UIDialogWindowData(title, "", buttons) { ShowCloseButton = true, CloseButtonAction = dialog.Dismiss });

        field.characterLimit = _characterLimit;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.richText = false;
        field.onSubmit.AddListener(_ => dialog.Save(onSave));
        field.text = current;
        if (field.placeholder is TMP_Text hint)
            hint.text = placeholder;

        copy.transform.SetSiblingIndex(1);
        copy.SetActive(true);
        copy.AddComponent<Guard>().Set(dialog.Dismiss, dialog._window);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)dialog._window.transform);
        return dialog;
    }

    internal static IDisposable Confirm(string title, string message, Action onYes, Action? onNo, ManualLogSource logger)
    {
        if (Begin(onNo, logger, out var content) is not { } dialog)
            return TimbnUndo.None;

        dialog.Show(
            new UIDialogWindowData(title, message, () => dialog.Answer(onYes), dialog.Dismiss, true) { ShowCloseButton = true, CloseButtonAction = dialog.Dismiss },
            content);
        return dialog;
    }

    internal static IDisposable Message(string title, string message, Action? onOk, ManualLogSource logger)
    {
        if (Begin(onOk, logger, out var content) is not { } dialog)
            return TimbnUndo.None;

        var ok = new UIDialogWindowData.ButtonData(dialog.Dismiss, LLBase.L("btn_ok"), null, true, GameKey.Select);
        dialog.Show(new UIDialogWindowData(title, message, ok) { CloseButtonAction = dialog.Dismiss }, content);
        return dialog;
    }

    /// <summary>Closes the dialog without calling any handler.</summary>
    public void Dispose()
    {
        if (_closed)
            return;

        _closed = true;
        if (_open == this)
            _open = null;

        if (_window != null && _window.IsShown && (_field != null || _marker != null))
            _window.Close();

        if (_field != null)
            UnityEngine.Object.Destroy(_field.gameObject);

        if (_marker != null)
            UnityEngine.Object.Destroy(_marker);
    }

    private static TimbnDialogWindow? Begin(Action? dismissed, ManualLogSource logger, out Transform content)
    {
        content = null!;
        if (!TimbnGame.IsInGame)
            return null;

        _open?.Dispose();
        var window = LazyUI.GetWindow<UIDialogWindow>();
        var found = window == null ? null : window.transform.Find("GenericWIndowLayout/Content");
        if (window == null || found == null || window.IsShown)
            return null;

        content = found;
        _open = new TimbnDialogWindow(window, dismissed, logger);
        return _open;
    }

    // A dialog with no text field still gets a marker, so the dialog hiding itself is noticed either way.
    private void Show(UIDialogWindowData data, Transform? content = null)
    {
        _window.Open(data);
        if (content == null)
            return;

        _marker = new GameObject("TimbnDialogMarker", typeof(RectTransform), typeof(LayoutElement));
        _marker.transform.SetParent(content, false);
        _marker.GetComponent<LayoutElement>().ignoreLayout = true;
        _marker.AddComponent<Guard>().Set(Dismiss, _window);
    }

    private void Save(Action<string> onSave)
    {
        var text = _field!.text;
        Answer(() => onSave(text));
    }

    private void Answer(Action handler)
    {
        if (_closed)
            return;

        Dispose();
        TimbnSafe.Run(handler, _logger, $"{nameof(TimbnPluginUI)}.{nameof(TimbnDialogWindow)}|The answer handler");
    }

    // The player backing out, by Cancel, No, the close button, Esc or the dialog going away.
    private void Dismiss()
    {
        if (_closed)
            return;

        Dispose();
        if (_dismissed != null)
            TimbnSafe.Run(_dismissed, _logger, $"{nameof(TimbnPluginUI)}.{nameof(TimbnDialogWindow)}|The dismiss handler");
    }

    // Typing must not set off the game's hotkeys, so keyboard input is switched off while the field has focus.
    private sealed class Guard : MonoBehaviour
    {
        private Action? _close;
        private UIDialogWindow? _window;
        private bool _started;
        private bool _suspended;
        private bool _wasActive;

        public void Set(Action close, UIDialogWindow window)
        {
            _close = close;
            _window = window;
        }

        private void LateUpdate()
        {
            var field = GetComponent<TMP_InputField>();
            if (field == null)
                return;

            if (!_started)
            {
                _started = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_window!.transform);

                // A gamepad cannot type, and focusing the field would switch off the input it needs to reach the buttons.
                if (!LazyInput.IsGamepadActive)
                {
                    field.Select();
                    field.ActivateInputField();
                    field.caretPosition = field.text.Length;
                }
            }

            var focused = field.isFocused;
            if (focused && !_suspended)
            {
                _wasActive = LazyInput.IsInputActive();
                LazyInput.SetInputActivity(false);
                _suspended = true;
            }
            else if (!focused && _suspended)
            {
                Restore();
            }

            // The field drops its own focus on Esc before this runs, so Esc closes whether or not it had focus a moment ago.
            if (Input.GetKeyDown(KeyCode.Escape))
                _close?.Invoke();
        }

        // The dialog hiding itself, whoever closed it, ends the prompt.
        private void OnDisable() => _close?.Invoke();

        private void OnDestroy() => Restore();

        private void Restore()
        {
            if (!_suspended)
                return;

            LazyInput.SetInputActivity(_wasActive);
            _suspended = false;
        }
    }
}
