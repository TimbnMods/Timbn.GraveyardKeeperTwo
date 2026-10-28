using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Core's Mods window, as a page like any other. A mod list sits at the left and, at the right, the chosen entry's card,
/// its tabs and the tab's page. It is built from the same public pieces a mod gets, so it needs nothing of Core's that a mod
/// cannot reach. A mod's first tab is its Overview, then the pages it added, and a group's first tab is a page that lists
/// its mods, then the pages added to the group. The screen keeps the view state and the draft of edits, and asks its
/// questions in <c>TimbnModsScreen.Questions.cs</c>.
/// </summary>
internal sealed partial class TimbnModsScreen : TimbnPage
{
    internal const string TitleToken = "timbn_mods_window";
    internal const string TitleText = "Mod Settings";

    private const float _cardHeight = 24f;

    // The most of the window's width the list of mods takes, whatever the names in it are.
    private const float _listMaxShare = 0.4f;

    // The mod's name and buttons keep this far in from the column's edges.
    private const float _headerInset = 8f;

    // The space under the list's header and the tab bar, one screen pixel.
    private const float _afterHeader = 0.5f;

    // The room a title line keeps under itself, the same as a line of text, and how far down in it the line sits.
    private const float _titleGap = 2f;
    private const float _titleDrop = 1.5f;

    // The space between a title line's name and each detail after it.
    private const float _titleLineGap = 8f;

    // The one line of the header, 12 tall with the 2 unit gap every line of text leaves under itself.
    private const float _linesHeight = 14f;

    private static TimbnModsScreen? _open;

    private readonly Action? _onClosed;
    private readonly List<PageTab> _tabs = [];
    private readonly TimbnModsOverview _overview = new();
    private TimbnModsList _modsList = null!;
    private TimbnModsView _view;
    private TimbnModsDraft _draft = new(null);
    private TimbnPageBuilder? _page;
    private TimbnList? _list;
    private TimbnRegion? _header;
    private TimbnRegion? _tabBar;
    private TimbnRegion? _body;
    private TimbnRegion? _footer;
    private TimbnList? _groupList;
    private int? _lastMember;
    private int _tabIndex;
    private Vector2Int _sized;
    private bool _built;
    private bool _bodyPending;

    private TimbnModsScreen(Action? onClosed)
    {
        _onClosed = onClosed;
        StepWithTriggers = false;
        EdgeToEdge = true;
        PauseGame = TimbnGame.IsInGame;
        SizeFromSettings();
    }

    public override string Title => TimbnCorePlugin.Instance!.Text.Get(TitleToken);

    // False when the window could not be made, so a caller that closed something first can put it back.
    internal static bool Show(Action? onClosed = null)
    {
        if (_open != null)
            return true;

        var screen = new TimbnModsScreen(onClosed);
        TimbnCorePlugin.Instance!.UI.ShowPage(screen);
        return screen._built;
    }

    // An F6 reload swaps the mods out from under the window's tabs and draft, so the window closes first.
    internal static void CloseForReload() => _open?.Close();

    private IReadOnlyList<TimbnModsMod> Mods => _modsList.Mods;

    private TimbnModsListEntry Entry => _modsList.Entries[_view.Entry];

    private TimbnModsMod Mod => Mods[_modsList.ModIndex(_view)];

    private bool HasMod => !_modsList.IsGroupPage(_view);

    private bool InOpenedGroupMod => _view.Member != null;

    private bool IsDirty => _draft.IsDirty;

    // Every setting Default All would put back is already at its default, counting the edits not saved yet.
    private bool AllDefault => HasMod && !TimbnModsPage.Resettable(Mod, TimbnModsWindowHost.ShowAdvanced.Value).Any(row => !_draft.IsDefault(row.Entry));

    // A mod's first tab is its Overview, where its settings, Enabled and their buttons are. A page the mod added, or an
    // Overview the mod replaced with its own page, handles its own edits, so the window shows none of those there.
    private bool OnSettingsTab => HasMod && _tabIndex == 0 && !TimbnSettingsPages.ReplacesDefault(Mod.Guid);

    protected override void Build(TimbnPageBuilder page)
    {
        _built = true;
        _open = this;
        _page = page;
        _modsList = TimbnModsList.Build(TimbnModsCatalog.FromChainloader(), TimbnModInfos.GroupOf, TimbnCorePlugin.Guid);
        _view = new TimbnModsView(0, null);
        _tabIndex = 0;
        BeginDraft();
        ComputeTabs();
        _bodyPending = true;

        List<TimbnListEntry> entries = [.. _modsList.Entries.Select(entry => new TimbnListEntry(entry.Title, entry.Subtitle))];
        page.Split(
            ListWidth(page, entries),
            string.Format(TimbnModsText.ModsPlate, Mods.Count),
            left =>
            {
                left.Add(new TimbnSpace(height: _afterHeader));
                _list = new TimbnList(entries, () => _view.Entry, SelectEntry)
                {
                    ShowsHint = false,
                    SelectsOnFocus = true,
                    FocusWhenShown = true,
                    Picked = _ => _body?.FocusFirst(),
                };
                left.Scroll(scroll => scroll.Add(_list));
            },
            right =>
            {
                right.Add(new TimbnSpace(height: _afterHeader));
                _body = right.Region(body =>
                {
                    if (!_bodyPending)
                        DrawBody(body);
                }, fill: 1f);
                _footer = right.Region(DrawFooter, gap: 0f);
            },
            top: top => _header = top.Region(DrawHeader),
            topLeft: DrawSummary,
            rightHeader: bar => _tabBar = bar.Region(DrawTabs),
            leftKey: GameKey.PrevSubTab,
            rightKey: GameKey.NextSubTab,
            rightDimmed: new(() => _list is { HasFocus: true }));

        page.AddKeyAction(TimbnFaceButton.SecondarySpecial, TimbnModsText.ResetSetting, _overview.ResetFocused, new(() => !OnSettingsTab || AllDefault), AskDefaults, TimbnModsText.HoldToResetAll);
    }

    // The list column is as wide as its widest line and no wider, so a long name is cut short with dots past a share of the
    // window. It is never narrower than the summary card over it. The window's width is the screen's, in the canvas's units,
    // times the window's width setting.
    private float ListWidth(TimbnPageBuilder page, IReadOnlyList<TimbnListEntry> entries)
    {
        var versionLine = TimbnText.WidthOf(page, TimbnModsText.GameVersion, TimbnTextKind.Name) + _titleLineGap + TimbnText.WidthOf(page, Application.version, TimbnTextKind.Detail);
        var screen = ((RectTransform)page.Root.GetComponentsInParent<Canvas>(true).Last().transform).rect.width;
        var cap = Mathf.Floor(screen * TimbnModsWindowHost.WidthPercent.Value / 100f * _listMaxShare);
        return Mathf.Max(Mathf.Min(TimbnList.WidthFor(page, entries), cap), _headerInset + versionLine);
    }

    // Opens the window and closes it again in the same frame so nothing is drawn, which gets everything it needs ready.
    internal static void WarmUp()
    {
        if (_open != null)
            return;

        var screen = new TimbnModsScreen(null);
        using var handle = TimbnCorePlugin.Instance!.UI.ShowPage(screen);
        screen.BuildBodyNow();
    }

    private void BuildBodyNow()
    {
        if (!_bodyPending)
            return;

        _bodyPending = false;
        _body?.Rebuild();
        _page?.Refresh();
    }

    protected override void OnTick()
    {
        // The settings are built one frame after the window shows, so opening it is not one long frame.
        BuildBodyNow();
        _overview.Tick();
        if (_sized != new Vector2Int(TimbnModsWindowHost.WidthPercent.Value, TimbnModsWindowHost.HeightPercent.Value))
            SizeFromSettings();
    }

    // With a gamepad B steps back out of a mod to its line in the list, and from the list it leaves. Every way out asks
    // first when the mod has unsaved edits.
    protected override void OnDismissRequested(Action dismiss)
    {
        if (TimbnInput.IsGamepadActive && _list is { HasFocus: false } list)
        {
            Leave(list.FocusChosen);
            return;
        }

        Leave(dismiss);
    }

    protected override void OnClosed()
    {
        if (_open == this)
            _open = null;

        _draft = new TimbnModsDraft(null);
        _onClosed?.Invoke();
    }

    // The width and height settings are the share of the screen the whole window takes, so 100 fills it.
    private void SizeFromSettings()
    {
        _sized = new Vector2Int(TimbnModsWindowHost.WidthPercent.Value, TimbnModsWindowHost.HeightPercent.Value);
        Width = TimbnLength.Percent(_sized.x);
        Height = TimbnLength.Percent(_sized.y);
    }

    // The gold bar holds the Overview and then the mod's own tabs, or the group's page of mods and then the pages added to
    // the group. Core's own pages scroll, and a page a mod added fills the area itself.
    private void ComputeTabs()
    {
        _tabs.Clear();
        if (!HasMod)
        {
            var group = Entry;
            _tabs.Add(new PageTab(group.Title, body => body.Scroll(scroll => _groupList = TimbnModsOverview.Group(scroll, group, Mods, _draft, () => _lastMember, OpenMod, () => _body?.FocusFirst()))));
            foreach (var added in TimbnGroupPages.For(group.Group!))
                _tabs.Add(new PageTab(added.Title, body => added.Build(body.Root)));

            return;
        }

        var mod = Mod;
        _tabs.Add(new PageTab(TimbnModsText.Overview, body => body.Scroll(scroll => _overview.Mod(scroll, mod, _draft))));
        foreach (var added in TimbnSettingsPages.For(mod.Guid))
            _tabs.Add(new PageTab(added.Title, body => added.Build(body.Root)));
    }

    // Over the list, the game's version on one title line. How many mods are loaded is on the list's plate.
    private void DrawSummary(TimbnPageBuilder summary) => HeaderCard(summary, row => HeaderLines(row, top => top.Column(
        lines => TitleLine(lines, TimbnModsText.GameVersion, Application.version),
        gap: 0f)));

    // Under the body, Back to Group at the left on every tab of a mod opened from its group, and Default All and Save at the
    // right on a mod's Overview. They stay put while the settings scroll. It is built again with the tabs, since which
    // buttons it has depends on the tab.
    private void DrawFooter(TimbnPageBuilder footer)
    {
        var back = HasMod && InOpenedGroupMod;
        var edits = OnSettingsTab;
        if (!back && !edits)
            return;

        footer.Card(card => card.Row(row =>
        {
            if (back)
            {
                var button = new TimbnButton(TimbnModsText.BackToGroup, () => Leave(BackToGroup));
                row.Add(button);
                footer.BindSecondary(button, TimbnGamepadPlacement.Bottom);
            }

            row.Add(new TimbnSpring());
            if (!edits)
                return;

            // A gamepad has Y for defaults, so that button is only for the mouse.
            var defaults = new TimbnButton(TimbnModsText.Defaults, AskDefaults) { Visible = new(() => !TimbnInput.IsGamepadActive), Disabled = new(() => AllDefault) };
            var save = new TimbnButton(TimbnModsText.Save, AskSave) { Disabled = new(() => !IsDirty) };
            row.Add(defaults);
            row.Add(save);
            footer.BindPrimarySpecial(save, TimbnGamepadPlacement.Bottom);
        }));
    }

    // The top is one line, the mod's name with its version, author and how many settings it has after it, or a group's name
    // with its authors and size.
    private void DrawHeader(TimbnPageBuilder header)
    {
        if (!HasMod)
        {
            // Every author of the group's mods once, in the order the list shows the mods.
            var group = Entry;
            var authors = group.Members.Select(index => TimbnModInfos.For(Mods[index].Guid)?.Author).Where(author => !string.IsNullOrWhiteSpace(author)).Distinct();
            HeaderCard(header, row => HeaderLines(row, top => HeaderText(top, group.Title, string.Empty, string.Join(", ", authors), group.Subtitle)));
            return;
        }

        var mod = Mod;
        var info = TimbnModInfos.For(mod.Guid);
        HeaderCard(header, row => HeaderLines(row, top => HeaderText(top, mod.Name, mod.Version, info?.Author, TimbnModsText.SettingCount(mod.Sections.Sum(section => section.Rows.Count)))));
    }

    // A name in yellow with its version, who made it and a count after it in gray, like the summary's line.
    private static void HeaderText(TimbnPageBuilder top, string name, string version, string? author, string count) => top.Column(
        lines => TitleLine(lines, name, version, string.IsNullOrWhiteSpace(author) ? string.Empty : string.Format(TimbnModsText.ByAuthor, author), count),
        gap: 0f);

    // A name in yellow with smaller gray details after it on its baseline, such as a mod's name and its version, with the
    // same 2 units under it that a line of text leaves. The line sits a little low in that room, where the header's lines
    // have always been.
    private static void TitleLine(TimbnPageBuilder page, string title, params string[] details)
    {
        page.Add(new TimbnSpace(height: _titleDrop));
        page.Row(
            row =>
            {
                row.Add(new TimbnText(title, TimbnTextKind.Name));
                foreach (var detail in details.Where(detail => detail.Length > 0))
                    row.Add(new TimbnText(detail, TimbnTextKind.Detail));
            },
            gap: _titleLineGap,
            verticalAlign: TimbnAlign.End);
        page.Add(new TimbnSpace(height: _titleGap - _titleDrop));
    }

    // The line of a header card sits at the top of a box one line tall.
    private static void HeaderLines(TimbnPageBuilder row, Action<TimbnPageBuilder> build) =>
        row.Row(build, gap: 0f, fill: 1f, height: _linesHeight, verticalAlign: TimbnAlign.Start);

    // Every card over the columns is one row this tall, in from the column's left edge.
    private static void HeaderCard(TimbnPageBuilder page, Action<TimbnPageBuilder> fill) => page.Column(
        column => column.Row(row =>
        {
            row.Add(new TimbnSpace(width: _headerInset));
            fill(row);
        }),
        gap: 0f,
        height: _cardHeight);

    private void DrawTabs(TimbnPageBuilder bar) =>
        bar.Add(new TimbnTabs([.. _tabs.Select(tab => tab.Title)], () => _tabIndex, SelectTab) { ShowsBumpers = true });

    private void DrawBody(TimbnPageBuilder body)
    {
        var tab = _tabs[Mathf.Clamp(_tabIndex, 0, _tabs.Count - 1)];
        TimbnSafe.Run(() => tab.Fill(body), TimbnModsWindowHost.Log, $"{nameof(TimbnModsScreen)}|{tab.Title}");
    }

    private void SelectEntry(int index)
    {
        if (index != _view.Entry)
            Leave(() => GoTo(new TimbnModsView(index, null)));
        else if (InOpenedGroupMod)
            Leave(BackToGroup);
    }

    // A mod picked on its group's page opens on its own settings, with the group still chosen in the list.
    private void OpenMod(int index)
    {
        _lastMember = index;
        GoTo(_view.WithMember(index));
    }

    // Back on the group's page the focus goes to the mod the player came from.
    private void BackToGroup()
    {
        GoTo(_view.WithMember(null));
        _groupList?.FocusChosen();
    }

    private void GoTo(TimbnModsView view)
    {
        if (view.Entry != _view.Entry)
            _lastMember = null;

        _bodyPending = false;
        _view = view;
        _tabIndex = 0;
        BeginDraft();
        ComputeTabs();
        _header!.Rebuild();
        _tabBar!.Rebuild();
        _body!.Rebuild();
        _footer!.Rebuild();
        _page!.Refresh();
    }

    private void SelectTab(int index)
    {
        _bodyPending = false;
        _tabIndex = index;
        _body!.Rebuild();
        _footer!.Rebuild();
        _page!.Refresh();
        _body.FocusFirst();
    }

    private void BeginDraft() => _draft = new TimbnModsDraft(HasMod ? Mod.Config : null);

    private sealed class PageTab(string title, Action<TimbnPageBuilder> fill)
    {
        public string Title { get; } = title;

        public Action<TimbnPageBuilder> Fill { get; } = fill;
    }
}
