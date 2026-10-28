using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The lines of a <see cref="TimbnList"/>, each a card that lights when it is chosen, hovered or focused.</summary>
internal sealed class TimbnListWidget : TimbnWidget
{
    private readonly List<GameObject> _frames = [];
    private readonly List<RectTransform> _cards = [];
    private readonly Func<int> _get;
    private readonly Action<int> _set;
    private readonly ManualLogSource _logger;

    // The line LT or RT stepped to, which takes the focus once it is the chosen one. The owner may ask first, such as about
    // unsaved edits, so the step can land later or not at all.
    private int _focusWhenChosen = -1;

    public TimbnListWidget(Transform parent, IReadOnlyList<TimbnListEntry> entries, Func<int> get, Action<int> set, ManualLogSource logger)
        : base(TimbnUi.Column(parent, "List"), null)
    {
        _get = get;
        _set = set;
        _logger = logger;
        for (var i = 0; i < entries.Count; i++)
        {
            var index = i;
            var card = TimbnUi.Card(Root, entries[i].Title);
            if (entries[i].Content is { } content)
                BuildContent(card, content);
            else
                TimbnUi.Entry(card, entries[i].Title, entries[i].Subtitle, wrap: false, detail: entries[i].Detail);

            var frame = TimbnUi.MakeButton(card, () => Choose(index));
            MarkSelect(card);
            _frames.Add(frame);
            _cards.Add(card);

            // A hover lights the card on its own, so it goes back to the chosen line's when the pointer leaves.
            card.GetComponent<LazyButton>().onExit.AddListener(() => frame.SetActive(index == _get()));
        }

        Refresh();
    }

    public int Count => _frames.Count;

    public bool ShowsHint { get; set; } = true;

    public Action<int>? Picked { get; set; }

    public bool FocusWhenShown { get; set; }

    // The chosen line's stop for the gamepad, or the first line's when none is chosen. A card's own stop is looked up on the
    // card, since a mod's content in the card may hold stops of its own.
    public GamepadNavigationItem? ChosenItem
    {
        get
        {
            var chosen = _get();
            var items = _cards.Select(card => card.GetComponent<GamepadNavigationItem>()).Where(item => item != null && item.isActiveAndEnabled && item.Active).ToList();
            var own = chosen >= 0 && chosen < _cards.Count ? _cards[chosen].GetComponent<GamepadNavigationItem>() : null;
            return own != null && items.Contains(own) ? own : items.FirstOrDefault();
        }
    }

    // The line the gamepad's focus is on, or -1.
    public int FocusedIndex => !LazyInput.IsGamepadActive || Root.GetComponentInParent<GamepadNavigationController>()?.FocusedItem is not { } item
        ? -1
        : _cards.FindIndex(card => item.transform.IsChildOf(card));

    public void FollowFocus(int index)
    {
        if (index < 0 || index == _get())
            return;

        // What the pick shows may build again, which drops the gamepad's focus, so the line keeps it.
        var controller = Root.GetComponentInParent<GamepadNavigationController>();
        var focused = controller?.FocusedItem;
        Choose(index, picked: false);
        if (controller != null && focused != null)
            controller.SetFocusedItem(focused);
    }

    public void FocusChosen()
    {
        _focusWhenChosen = -1;
        TimbnPageWindow.FocusOrResume(ChosenItem);
    }

    public override void Refresh()
    {
        var chosen = _get();
        for (var i = 0; i < _frames.Count; i++)
            _frames[i].SetActive(i == chosen);

        if (_focusWhenChosen >= 0 && _focusWhenChosen == chosen)
            FocusChosen();
    }

    /// <summary>Steps the chosen line by the given amount, wrapping at the ends, and moves the gamepad's focus onto it.</summary>
    internal void Cycle(int delta)
    {
        if (_frames.Count <= 1)
            return;

        _focusWhenChosen = (_get() + delta + _frames.Count) % _frames.Count;
        Choose(_focusWhenChosen, picked: false);
    }

    private void BuildContent(RectTransform card, Action<TimbnPageBuilder> content)
    {
        var column = TimbnUi.Column(card, "Content", TimbnPageBuilder.PartGap);
        var builder = TimbnPageBuilder.Create(column, _logger);
        TimbnSafe.Run(() => content(builder), _logger, $"{nameof(TimbnList)}|The entry content");
    }

    private void Choose(int index, bool picked = true)
    {
        if (!Interactable)
            return;

        TimbnSafe.Run(() => _set(index), _logger, $"{nameof(TimbnList)}|The choose handler");
        Refresh();
        if (picked && Picked != null)
            TimbnSafe.Run(() => Picked(index), _logger, $"{nameof(TimbnList)}|The picked handler");
    }
}
