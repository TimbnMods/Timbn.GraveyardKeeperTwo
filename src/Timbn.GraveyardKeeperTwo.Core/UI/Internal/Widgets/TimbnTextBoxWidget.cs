using TMPro;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A setting typed into its value box. A click types in place, unless the text is wider than the box, when the game's
/// text window opens instead so all of it shows. A gamepad always gets the text window, since it cannot type in place.
/// </summary>
internal sealed class TimbnTextBoxWidget : TimbnWidget
{
    private const int _characterLimit = 200;

    private readonly Func<string> _get;
    private readonly Action<string> _set;
    private readonly TMP_InputField _field;
    private readonly ManualLogSource _logger;
    private readonly bool _typesInPlace;
    private bool _windowOpen;

    public TimbnTextBoxWidget(TimbnWidgets.SettingParts parts, Func<string> get, Action<string> set, bool? wholeNumbers, string placeholder, bool typesInPlace, bool focusWhenShown, bool inField, bool showsFrame, ManualLogSource logger)
        : base(parts.Row, parts.Name)
    {
        _get = get;
        _set = set;
        _logger = logger;
        _typesInPlace = typesInPlace;
        _field = TimbnWidgets.TypeBox(parts.Control, placeholder, value => { if (typesInPlace) _set(value); }, inField ? TimbnWidgets.BoxHeight : TimbnWidgets.TypeBoxHeight, showsFrame);
        if (wholeNumbers is { } whole)
            _field.contentType = whole ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;

        if (typesInPlace)
        {
            _field.characterLimit = _characterLimit;
        }

        if (focusWhenShown)
            _field.gameObject.AddComponent<TimbnFocusOnStart>().Field = _field;

        _field.onSelect.AddListener(_ =>
        {
            if (typesInPlace || !Overflows)
                return;

            _field.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            OpenWindow();
        });
        // An edit Esc ended is not kept, so Esc never saves what was typed.
        _field.onEndEdit.AddListener(text =>
        {
            if (!_field.wasCanceled)
                Commit(text);
        });
        TimbnWidgets.FitValueArt(parts.Row);
        GamepadStop(null, typesInPlace ? FocusField : OpenWindow);

        // The box already has the caret, so A has nothing to pick and the page shows no Select hint for it.
        if (focusWhenShown)
            UnmarkSelect(Root);

        Refresh();
    }

    public override void Refresh() => _field.SetTextWithoutNotify(_get());

    private bool Overflows => _field.textComponent.GetPreferredValues(_get()).x > _field.textViewport.rect.width;

    private void Commit(string text)
    {
        // The page may have closed or rebuilt while the text window was open, so the box is gone.
        if (Root == null)
            return;

        if (text != _get())
            _set(text);

        Refresh();
    }

    private void FocusField()
    {
        _field.Select();
        _field.ActivateInputField();
    }

    // A gamepad's A both selects the box and presses it, and a long text opens the window from each, so only one opens.
    private void OpenWindow()
    {
        if (!Interactable || _windowOpen)
            return;

        _windowOpen = true;

        // A window that could not open hands back the handle that does nothing, and never calls closed.
        if (TimbnPrompts.Prompt(TitleText, _get(), string.Empty, Commit, null, _logger, () => _windowOpen = false) == TimbnUndo.None)
            _windowOpen = false;
    }
}
