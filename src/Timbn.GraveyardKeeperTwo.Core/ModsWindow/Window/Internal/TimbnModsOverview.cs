namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The first tab of a mod or a group in the Mods window. A mod's is its About card, then the Settings header, the search
/// box and the sections made from its config, and a group's lists its mods. The search text stays while the window is
/// open, and the settings that can go back to their default are kept for the gamepad's reset key.
/// </summary>
internal sealed class TimbnModsOverview
{
    private const float _aboutPadding = 3f;
    private const float _searchGap = 4f;
    private const float _searchSide = 1f;

    // Typing settles for this long before the settings are built again for the new search, so each keystroke does not rebuild them all.
    private const float _searchSettleSeconds = 0.25f;

    private readonly List<(TimbnField Field, Action Reset)> _resets = [];
    private string _search = string.Empty;
    private string _shownSearch = string.Empty;
    private float _searchChangedAt;
    private TimbnRegion? _sections;
    private TimbnField? _showAdvanced;

    // Builds the settings again once the search text has stopped changing for a moment.
    public void Tick()
    {
        if (_search == _shownSearch || Time.unscaledTime - _searchChangedAt < _searchSettleSeconds)
            return;

        _shownSearch = _search;
        _sections?.Rebuild();
    }

    public void Mod(TimbnPageBuilder page, TimbnModsMod mod, TimbnModsDraft draft)
    {
        AboutCard(page, mod, TimbnModInfos.For(mod.Guid));
        if (TimbnSettingsPages.ReplacesDefault(mod.Guid))
            return;

        page.Add(new TimbnSection(TimbnModsText.Settings));
        if (!mod.Sections.Any(section => section.Rows.Count > 0))
        {
            page.Card(card => card.Add(new TimbnText(TimbnModsText.NoSettings)));
            return;
        }

        page.Add(new TimbnSpace(height: _searchGap));
        page.Row(
            row =>
            {
                row.Add(new TimbnSpace(width: _searchSide));
                row.Column(
                    column => column.Add(new TimbnTextBox(() => _search, text =>
                    {
                        _search = text;
                        _searchChangedAt = Time.unscaledTime;
                    })
                    { Placeholder = TimbnModsText.Search, SetsWhileTyping = true, ShowsFocusFrame = false }),
                    gap: 0f,
                    fill: 1f);
                row.Add(new TimbnSpace(width: _searchSide));
            },
            gap: 0f);
        page.Add(new TimbnSpace(height: _searchGap));
        _sections = page.Region(sections => Sections(sections, mod, draft), gap: 0f);
    }

    // The mods of a group are rows to pick from, each opening the mod's own settings. The one opened last stays lit, so the
    // focus can go back to it.
    public static TimbnList Group(TimbnPageBuilder page, TimbnModsListEntry group, IReadOnlyList<TimbnModsMod> mods, TimbnModsDraft draft, Func<int?> opened, Action<int> open, Action picked)
    {
        var entries = new List<TimbnListEntry>();
        foreach (var index in group.Members)
        {
            var mod = mods[index];
            var info = TimbnModInfos.For(mod.Guid);
            var off = mod.Enabled is { } enabled && !(bool)draft.Get(enabled) ? $" ({TimbnModsText.Off})" : string.Empty;
            var description = string.IsNullOrWhiteSpace(info?.Description) ? string.Empty : TimbnModsText.Translated(TimbnModsText.AboutKey(mod.Guid), info!.Description!.Trim());
            entries.Add(new TimbnListEntry(mod.Name + off, mod.Version, description));
        }

        var chosen = () => opened() is { } mod ? group.Members.ToList().IndexOf(mod) : -1;
        return page.Add(new TimbnList(entries, chosen, row => open(group.Members[row])) { ShowsHint = false, Picked = _ => picked() });
    }

    // Y on a gamepad puts the setting in focus back to its default.
    public void ResetFocused()
    {
        foreach (var (field, reset) in _resets)
        {
            if (!field.HasFocus)
                continue;

            reset();
            return;
        }
    }

    private void Sections(TimbnPageBuilder page, TimbnModsMod mod, TimbnModsDraft draft)
    {
        _resets.Clear();

        // The advanced switch is the window's own, so flipping it is not an edit to save, and it stays where it is for every mod.
        var show = TimbnModsWindowHost.ShowAdvanced;
        TimbnModsPage.Fill(page, draft, mod.Guid, mod.Sections, _search, show.Value, (field, reset) => _resets.Add((field, reset)));
        if (!mod.HasAdvanced)
            return;

        page.Add(new TimbnSection(TimbnModsText.Advanced));
        // Only the settings build again, so the scroll stays where it is and the focus goes back to the switch.
        page.Card(card => _showAdvanced = card.Add(new TimbnField(
            TimbnModsText.ShowAdvanced,
            TimbnModsText.ShowAdvancedLine,
            new TimbnToggle(
                () => show.Value,
                value =>
                {
                    show.Value = value;
                    _sections?.Rebuild();
                    _showAdvanced?.Focus();
                }))));
    }

    private static void AboutCard(TimbnPageBuilder page, TimbnModsMod mod, TimbnModInfo? info)
    {
        var links = info == null ? [] : TimbnModLinks.For(info);
        var description = TimbnAboutCard.DescriptionFor(mod, info, links.Count > 0);
        page.Card(card =>
        {
            if (description != null)
            {
                card.Add(new TimbnSpace(height: _aboutPadding));
                card.Add(new TimbnText(description) { Wrap = true });
                card.Add(new TimbnSpace(height: _aboutPadding));
            }

            if (links.Count == 0)
                return;

            card.Row(row =>
            {
                foreach (var (label, url) in links)
                    row.Add(new TimbnButton(label, () => TimbnAboutCard.OpenUrl(label, url)));
            });
        });
    }
}
