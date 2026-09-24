using LazyBearTechnology;
using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnMainMenu
{
    private static readonly AccessTools.FieldRef<MainGame, UIMainMenuInfoPanel> _infoPanel =
        AccessTools.FieldRefAccess<MainGame, UIMainMenuInfoPanel>("uiMainMenuInfoPanel");

    private static readonly AccessTools.FieldRef<UIMainMenuInfoPanel, TextMeshProUGUI> _developerLabel =
        AccessTools.FieldRefAccess<UIMainMenuInfoPanel, TextMeshProUGUI>("developerNameLabel");

    private static readonly AccessTools.FieldRef<UIMainMenuInfoPanel, TextMeshProUGUI> _publisherLabel =
        AccessTools.FieldRefAccess<UIMainMenuInfoPanel, TextMeshProUGUI>("publisherNameLabel");

    private static readonly AccessTools.FieldRef<UIMainMenuInfoPanel, TextStyle> _valueStyle =
        AccessTools.FieldRefAccess<UIMainMenuInfoPanel, TextStyle>("rightSideStyle");

    private static readonly List<Line> _lines = [];
    private static readonly List<Popup> _popups = [];
    private static UIMainMenuWindow? _menu;

    internal static IDisposable AddLine(string label, string value)
    {
        var line = new Line(label, value);
        _lines.Add(line);
        DrawLines();
        return new TimbnUndo(() =>
        {
            _lines.Remove(line);
            if (line.Text != null)
                UnityEngine.Object.Destroy(line.Text.gameObject);
            DrawLines();
        });
    }

    internal static IDisposable AddPopup(string header, string text)
    {
        var popup = new Popup(header, text);
        _popups.Add(popup);
        return new TimbnUndo(() => _popups.Remove(popup));
    }

    internal static void OnMenuOpened(UIMainMenuWindow menu) => _menu = menu;

    internal static void DrawLines()
    {
        if (MainGame.Instance == null || _infoPanel(MainGame.Instance) is not { } panel || panel == null)
            return;

        var developer = _developerLabel(panel);
        var publisher = _publisherLabel(panel);
        var step = developer.rectTransform.anchoredPosition - publisher.rectTransform.anchoredPosition;
        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            if (line.Text == null)
            {
                line.Text = UnityEngine.Object.Instantiate(publisher, publisher.transform.parent);
                line.Text.name = "TimbnLine";
            }

            line.Text.rectTransform.anchoredPosition = developer.rectTransform.anchoredPosition + step * (_lines.Count - i + 1);
            line.Text.text = line.Label + _valueStyle(panel).ApplyStyleToString(line.Value, staticFont: true);
            line.Text.gameObject.SetActive(developer.gameObject.activeSelf);
        }
    }

    internal static void OnUpdate()
    {
        if (_popups.Count == 0 || _menu == null || !_menu.IsShownAndTop)
            return;

        var popup = _popups[0];
        _popups.RemoveAt(0);
        var dialog = LazyUI.GetWindow<UIDialogWindow>();
        var ok = new UIDialogWindowData.ButtonData(dialog.Close, LLBase.L("btn_ok"), null, replaceForGamepad: true, GameKey.Select);
        dialog.Open(new UIDialogWindowData(popup.Header, popup.Text, ok));
    }

    private sealed class Line
    {
        public Line(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }

        public string Value { get; }

        public TextMeshProUGUI? Text { get; set; }
    }

    private sealed class Popup
    {
        public Popup(string header, string text)
        {
            Header = header;
            Text = text;
        }

        public string Header { get; }

        public string Text { get; }
    }
}
