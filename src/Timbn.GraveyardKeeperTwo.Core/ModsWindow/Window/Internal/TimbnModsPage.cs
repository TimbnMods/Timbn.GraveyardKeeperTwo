using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Turns a mod's config sections into the cards of its Overview. Each control reads and writes through the Mods window's
/// draft, so the mod only sees a change once the player saves it.
/// </summary>
internal static class TimbnModsPage
{
    private static readonly HashSet<Type> _wholeNumbers = [typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong)];

    // Every config section on one page, each under a plate with its name. Advanced settings only show when asked for. A
    // search keeps the settings whose name or description holds it, or every setting of a section whose name does, and
    // drops sections left empty.
    // Each setting that can go back to its default is handed to resets with its field, for the gamepad's reset key.
    internal static void Fill(TimbnPageBuilder page, TimbnModsDraft draft, string guid, IReadOnlyList<TimbnModsSection> sections, string search, bool showAdvanced, Action<TimbnField, Action> resets)
    {
        var shown = Visible(sections, search, showAdvanced, row => Translate(guid, row));
        foreach (var (name, rows) in shown)
        {
            page.Add(new TimbnSection(name));
            foreach (var row in rows)
                AddRow(page, draft, guid, row, resets);
        }

        if (shown.Count == 0 && search.Trim().Length > 0)
            page.Card(card => card.Add(new TimbnText(string.Format(TimbnModsText.NoMatches, search.Trim()))));
    }

    // The sections and settings that show, in bind order. A search also finds a description in the text the page shows it in.
    internal static IReadOnlyList<(string Name, IReadOnlyList<TimbnModsRow> Rows)> Visible(IReadOnlyList<TimbnModsSection> sections, string search, bool showAdvanced, Func<TimbnModsRow, string>? shownDescription = null)
    {
        search = search.Trim();
        var visible = new List<(string, IReadOnlyList<TimbnModsRow>)>();
        foreach (var section in sections)
        {
            var shown = section.Rows.Where(row => showAdvanced || !row.IsAdvanced).ToList();
            IReadOnlyList<TimbnModsRow> rows = Has(section.Name, search) ? shown : [.. shown.Where(row => Matches(row, search, shownDescription?.Invoke(row)))];
            if (rows.Count > 0)
                visible.Add((section.Name, rows));
        }

        return visible;
    }

    // What Default All puts back. A read only row is the mod's to change, a row tagged HideDefaultButton has no default
    // to go back to, and an advanced row stays as it is while the player cannot see it.
    internal static IEnumerable<TimbnModsRow> Resettable(TimbnModsMod mod, bool showAdvanced) =>
        mod.Sections.SelectMany(section => section.Rows).Where(row => CanReset(row) && (showAdvanced || !row.IsAdvanced));

    private static bool CanReset(TimbnModsRow row) => !row.IsBroken && !row.IsReadOnly && !row.HidesReset;

    internal static bool Matches(TimbnModsRow row, string search, string? shownDescription = null) =>
        Has(row.Label, search) || Has(row.Description, search) || (shownDescription != null && Has(shownDescription, search)) || Has(row.Entry.Definition.Key, search);

    private static bool Has(string text, string search) =>
        search.Length == 0 || text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

    // A setting is a card with one field, the control with its reset button beside it. A setting that cannot be reset
    // keeps the button's room, so the controls of a list stay in line.
    private static void AddRow(TimbnPageBuilder page, TimbnModsDraft draft, string guid, TimbnModsRow row, Action<TimbnField, Action> resets)
    {
        var entry = row.Entry;
        page.Card(card =>
        {
            var control = Control(draft, row);
            var parts = new List<TimbnComponent> { control };
            Action? reset = null;
            if (CanReset(row))
            {
                reset = () =>
                {
                    draft.Set(entry, entry.DefaultValue);
                    control.Refresh();
                };
                parts.Add(new TimbnButton(TimbnIcon.Reset, reset) { TooltipToken = TimbnModsText.ResetTooltipToken, Disabled = new(() => draft.IsDefault(entry)) });
            }
            else
            {
                parts.Add(new TimbnSpace(TimbnButton.ResetWidth));
            }

            // A broken row is dimmed as a whole already, and a read only control inside it would dim twice.
            control.ReadOnly = row.IsReadOnly && !row.IsBroken;
            var field = new TimbnField(row.Label, Describe(guid, row), [.. parts])
            {
                Tags = row.Tags,
                Disabled = row.IsBroken,
            };
            card.Add(field);
            if (reset != null)
                resets(field, reset);
        });
    }

    private static TimbnControl Control(TimbnModsDraft draft, TimbnModsRow row)
    {
        var entry = row.Entry;
        switch (row.Kind)
        {
            case TimbnModsRowKind.Switch when row.Options == null && entry is ConfigEntry<bool> toggle:
                return new TimbnToggle(() => (bool)draft.Get(toggle), value => draft.Set(toggle, value));

            case TimbnModsRowKind.Switch when row.Options != null:
                var options = row.Options;
                return new TimbnChoice(row.OptionNames ?? options, () => Math.Max(0, IndexOf(options, draft.GetText(entry))), index => draft.SetText(entry, options[index]));

            // The slider holds floats, so a double or decimal range is typed to keep the setting's own precision.
            case TimbnModsRowKind.Slider when entry.SettingType == typeof(double) || entry.SettingType == typeof(decimal):
                return new TimbnTextBox(() => draft.GetText(entry), text => draft.SetText(entry, text), WholeNumbers(entry));

            case TimbnModsRowKind.Slider:
                var min = Convert.ToSingle(row.Min, CultureInfo.InvariantCulture);
                var max = Convert.ToSingle(row.Max, CultureInfo.InvariantCulture);
                var whole = _wholeNumbers.Contains(entry.SettingType);
                var decimals = TimbnSliderSteps.Decimals(whole, min, max, Convert.ToSingle(entry.DefaultValue, CultureInfo.InvariantCulture));

                return new TimbnSlider(
                    min,
                    max,
                    () => Convert.ToSingle(draft.Get(entry), CultureInfo.InvariantCulture),
                    value => draft.Set(entry, Convert.ChangeType(value, entry.SettingType, CultureInfo.InvariantCulture)),
                    whole)
                {
                    Decimals = decimals,
                };

            case TimbnModsRowKind.Text when row.FromText != null:
                return new TimbnTextBox(() => draft.GetText(entry, row.ToText), text => draft.SetText(entry, text, row.FromText, TimbnModsWindowHost.Log));

            case TimbnModsRowKind.Text:
                return new TimbnTextBox(() => draft.GetText(entry, row.ToText), text => draft.SetText(entry, text), row.ToText == null ? WholeNumbers(entry) : null);

            case TimbnModsRowKind.Key when entry is ConfigEntry<KeyboardShortcut> key:
                return new TimbnKey(() => (KeyboardShortcut)draft.Get(key), value => draft.Set(key, value));

            default:
                return new TimbnValue(() => draft.GetText(entry, row.ToText));
        }
    }

    // The setting's own description in the player's language when the mod's lang file has it, shown under it, and why it is
    // off when a game update broke it.
    private static string Describe(string guid, TimbnModsRow row)
    {
        var description = Translate(guid, row);
        return row.IsBroken ? string.Join(" ", new[] { description, TimbnModsText.BrokenLine }.Where(text => text.Length > 0)) : description;
    }

    private static string Translate(string guid, TimbnModsRow row) =>
        row.Description.Length > 0 ? TimbnModsText.Translated(TimbnModsText.SettingKey(guid, row.Entry.Definition), row.Description) : row.Description;

    // A number with no range types in its box, which then takes only whole numbers or only decimals, and any other text takes anything.
    private static bool? WholeNumbers(ConfigEntryBase entry) =>
        _wholeNumbers.Contains(entry.SettingType) ? true
        : entry.SettingType == typeof(float) || entry.SettingType == typeof(double) || entry.SettingType == typeof(decimal) ? false
        : null;

    private static int IndexOf(IReadOnlyList<string> options, string value)
    {
        for (var i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i], value, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}
