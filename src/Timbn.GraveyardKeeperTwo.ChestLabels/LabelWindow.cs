using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.ChestLabels;

/// <summary>
/// A one field text window in the look of the game's bug report window. It is a copy of that window's prefab with the
/// report code taken off, so nothing here can open, fill or send a report.
/// </summary>
internal sealed class LabelWindow
{
    private static readonly string[] _hiddenRows = ["Steps", "Email", "VsyncSwitchBtn", "CategoryHeader", "Categories", "Privacy", "Status"];

    private readonly Plugin _plugin;
    private GameObject? _root;
    private IDisposable? _control;
    private IDisposable? _freeze;
    private bool? _inputWasActive;

    public LabelWindow(Plugin plugin)
    {
        _plugin = plugin;
    }

    public bool IsOpen => _root != null;

    public void OpenFor(WgoData chest)
    {
        if (IsOpen)
            return;

        var original = LazyUI.GetWindow<UIBugReportWindow>();
        if (original == null)
            return;

        var root = UnityEngine.Object.Instantiate(original.gameObject, original.transform.parent);
        root.name = "ChestLabelWindow";
        UnityEngine.Object.DestroyImmediate(root.GetComponent<UIBugReportWindow>());
        foreach (var behaviour in root.GetComponents<MonoBehaviour>().Where(IsStaleController))
            UnityEngine.Object.DestroyImmediate(behaviour);

        foreach (var localized in root.GetComponentsInChildren<LocalizedLabel>(true))
            UnityEngine.Object.DestroyImmediate(localized);

        foreach (var name in _hiddenRows)
            Find(root, name)?.gameObject.SetActive(false);

        var input = root.GetComponentsInChildren<TMP_InputField>(true).FirstOrDefault(field => field.name == "Title");
        if (input == null)
        {
            UnityEngine.Object.Destroy(root);
            return;
        }

        var header = Find(root, "Header")?.GetComponent<TMP_Text>();
        if (header != null)
            header.text = LLBase.L("timbn_chest_labels_title");

        if (input.placeholder is TMP_Text placeholder)
            placeholder.text = LLBase.L("timbn_chest_labels_placeholder");

        input.text = _plugin.Board.GetText(chest);
        input.richText = false;

        void Save()
        {
            _plugin.Board.SetText(chest, input.text);
            Close();
        }

        input.onSubmit.AddListener(_ => Save());
        var buttons = root.GetComponentsInChildren<UIDialogWindowButton>(true);
        buttons.FirstOrDefault(button => button.name == "Send")?.Draw(
            new UIDialogWindowData.ButtonData(Save, LLBase.L("timbn_chest_labels_save"), null, false, GameKey.Select));
        buttons.FirstOrDefault(button => button.name == "Cancel")?.Draw(
            new UIDialogWindowData.ButtonData(Close, LLBase.L("timbn_chest_labels_cancel"), null, false, GameKey.Back));

        root.AddComponent<InputGuard>().Set(() =>
        {
            if (_root == root)
                Close();
        });
        _root = root;
        _control = _plugin.Player.TakeControl();
        _freeze = _plugin.Clock.Freeze();
        _inputWasActive = LazyInput.IsInputActive();
        LazyInput.SetInputActivity(false);
        root.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
        _plugin.StartCoroutine(Focus(root, input));
    }

    public void Close()
    {
        if (_inputWasActive is { } wasActive)
        {
            _inputWasActive = null;
            LazyInput.SetInputActivity(wasActive);
        }

        _freeze?.Dispose();
        _control?.Dispose();
        _freeze = null;
        _control = null;
        if (_root != null)
        {
            var root = _root;
            _root = null;
            UnityEngine.Object.Destroy(root);
        }
    }

    // The prefab's gamepad navigation points at fields of the component that was removed.
    private static bool IsStaleController(MonoBehaviour behaviour) =>
        behaviour != null && behaviour.GetType().Name == "GamepadNavigationController";

    private static Transform? Find(GameObject root, string name) =>
        root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);

    private static System.Collections.IEnumerator Focus(GameObject root, TMP_InputField input)
    {
        yield return null;
        if (input != null)
        {
            // The buttons size to their text a frame late, so their row is rebuilt before the whole window.
            if (Find(root, "Buttons") is RectTransform buttons)
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttons);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
            input.Select();
            input.ActivateInputField();
            input.caretPosition = input.text.Length;
        }
    }

    private sealed class InputGuard : MonoBehaviour
    {
        private Action? _close;

        public void Set(Action close) => _close = close;

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                _close?.Invoke();
        }

        private void OnDestroy() => _close?.Invoke();
    }
}
