using LazyBearTechnology;
using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The bar of key hints at the bottom of the screen, copied from the one the fishing window shows while the player fishes.
/// With a gamepad it is the game's line of button icons, and with a keyboard it is the fishing window's own row of key
/// caps, such as Esc on a key with Exit beside it. The newest bar a plugin shows is the one drawn.
/// </summary>
internal static class TimbnKeyHints
{
    // The fishing window sits at this canvas order, over the world and the HUD.
    private const int _sortingOrder = 450;

    // The fishing window hides the item bar under its hints, and in play it stays, so the hints sit just above it.
    private const float _aboveItemBar = 80f;

    // The fishing window's keyboard row, a key cap with the key's name on it and the label beside it.
    private const string _keyRow = "Horizontal3";
    private const string _keyName = "M1/EscTip/BindKeyboard";
    private const string _keyLabel = "ExitHint";

    private static readonly AccessTools.FieldRef<UIFishingWindow, LazyButtonTipsStr>? _fishingTips =
        TimbnReflection.TryField<UIFishingWindow, LazyButtonTipsStr>("lazyButtonTips");

    private static readonly AccessTools.FieldRef<UIFishingWindow, GameObject>? _fishingKeys =
        TimbnReflection.TryField<UIFishingWindow, GameObject>("mouseAndKeyboardHints");

    private static readonly List<Entry> _shown = [];
    private static readonly List<GameObject> _rows = [];
    private static RectTransform? _holder;
    private static LazyButtonTipsStr? _bar;
    private static RectTransform? _keys;
    private static GameObject? _rowTemplate;
    private static string _printed = string.Empty;

    internal static IDisposable Show(IReadOnlyList<TimbnKeyHint> hints, ManualLogSource logger)
    {
        var entry = new Entry(hints, logger);
        _shown.Add(entry);
        return new TimbnUndo(() => _shown.Remove(entry));
    }

    internal static void Tick()
    {
        var hints = _shown.Count > 0 && TimbnGame.IsInGame ? Labels(_shown[^1]) : null;
        if (hints is not { Count: > 0 } || !Ready())
        {
            Hide();
            return;
        }

        _holder!.gameObject.SetActive(true);
        var gamepad = LazyInput.IsGamepadActive;
        var printed = (gamepad ? "pad|" : "keys|") + string.Join("|", hints.Select(hint => ControllerIconLibrary.GetIconId(hint.Key) + hint.Label));
        if (printed == _printed)
            return;

        _printed = printed;
        _bar!.gameObject.SetActive(gamepad);
        _keys!.gameObject.SetActive(!gamepad);
        if (gamepad)
            _bar.Print([.. hints.Select(hint => new LazyGameKeyTip(hint.Key, hint.Label, gamepadOnly: false, translate: false))]);
        else
            DrawKeys(hints);
    }

    private static void Hide()
    {
        if (_holder != null)
            _holder.gameObject.SetActive(false);

        _printed = string.Empty;
    }

    // A key cap per hint, the key's name taken from the game's own keyboard icon text, such as "[Esc]".
    private static void DrawKeys(List<(GameKey Key, string Label)> hints)
    {
        while (_rows.Count < hints.Count)
        {
            var row = UnityEngine.Object.Instantiate(_rowTemplate!, _keys!, false);
            row.SetActive(true);
            _rows.Add(row);
        }

        for (var i = 0; i < _rows.Count; i++)
        {
            _rows[i].SetActive(i < hints.Count);
            if (i >= hints.Count)
                continue;

            var name = ControllerIconLibrary.GetIconId(hints[i].Key, trailingSpace: false).Trim('[', ']');
            if (_rows[i].transform.Find(_keyName)?.GetComponent<TextMeshProUGUI>() is { } key)
                key.text = name;

            if (_rows[i].transform.Find(_keyLabel)?.GetComponent<TextMeshProUGUI>() is { } label)
                label.text = hints[i].Label;
        }

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_keys!);
    }

    private static List<(GameKey Key, string Label)> Labels(Entry entry)
    {
        var labels = new List<(GameKey, string)>();
        for (var i = 0; i < entry.Hints.Count; i++)
        {
            if (entry.Broken.Contains(i))
                continue;

            var hint = entry.Hints[i];
            string? label = null;
            // A label that throws is left out from then on, so the bar logs it once instead of every frame.
            if (!TimbnSafe.Run(() => label = hint.Label.Current, entry.Logger, $"{nameof(TimbnPluginUI)}.{nameof(TimbnPluginUI.ShowKeyHints)}|A label"))
            {
                entry.Broken.Add(i);
                continue;
            }

            if (!string.IsNullOrEmpty(label))
                labels.Add((hint.Key, label!));
        }

        return labels;
    }

    // Both rows are copied from the fishing window the first time a bar shows, next to the game's windows.
    private static bool Ready()
    {
        if (_bar != null && _keys != null)
            return true;

        var fishing = LazyUI.IsInitialized ? LazyUI.GetWindow<UIFishingWindow>() : null;
        if (fishing == null || fishing.transform.parent == null || _fishingTips?.Invoke(fishing) is not { } tips || _fishingKeys?.Invoke(fishing) is not { } keys)
            return false;

        _holder = TimbnUi.Stretch(TimbnUi.Box(fishing.transform.parent, "TimbnKeyHints"));
        var canvas = _holder.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = _sortingOrder;

        _bar = UnityEngine.Object.Instantiate(tips.gameObject, _holder, false).GetComponent<LazyButtonTipsStr>();
        Raise((RectTransform)_bar.transform);

        _keys = (RectTransform)UnityEngine.Object.Instantiate(keys, _holder, false).transform;
        Raise(_keys);
        foreach (Transform row in _keys)
            row.gameObject.SetActive(false);

        _rowTemplate = _keys.Find(_keyRow)?.gameObject;
        if (_rowTemplate == null)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnKeyHints)}|The fishing window's keyboard row '{_keyRow}' is gone, so the keyboard bar shows nothing.");
            _rowTemplate = new GameObject(_keyRow, typeof(RectTransform));
            _rowTemplate.transform.SetParent(_keys, false);
        }

        return true;
    }

    private static void Raise(RectTransform box) => box.anchoredPosition = new Vector2(box.anchoredPosition.x, _aboveItemBar);

    private sealed class Entry(IReadOnlyList<TimbnKeyHint> hints, ManualLogSource logger)
    {
        public IReadOnlyList<TimbnKeyHint> Hints { get; } = hints;

        public ManualLogSource Logger { get; } = logger;

        public HashSet<int> Broken { get; } = [];
    }
}
