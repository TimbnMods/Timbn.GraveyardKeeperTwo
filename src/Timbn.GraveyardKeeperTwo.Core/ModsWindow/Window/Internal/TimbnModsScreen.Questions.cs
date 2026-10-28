namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// The questions the Mods window asks before it saves, puts settings back or leaves a mod with unsaved edits.
internal sealed partial class TimbnModsScreen
{
    private const float _questionWidth = 260f;

    // Goes on at once when the mod has no unsaved edits, otherwise once the player saves or discards them. Backing out with
    // Esc, B or the close button keeps the edits unsaved and stays on the mod, with the list's focus back on its line,
    // since moving the focus is what asked.
    private void Leave(Action next)
    {
        if (!IsDirty)
        {
            next();
            return;
        }

        var ask = new TimbnPage(TimbnModsText.UnsavedTitle, (page, close) =>
        {
            page.Add(new TimbnText(string.Format(TimbnModsText.UnsavedMessage, Mod.Name)) { Wrap = true, Align = TimbnAlign.Center });
            page.ButtonFooter(footer =>
            {
                footer.AddPrimarySpecial(TimbnModsText.Save, () =>
                {
                    close();
                    var notice = SaveDraft();
                    next();
                    notice?.Invoke();
                });
                footer.AddSecondarySpecial(TimbnModsText.Discard, () =>
                {
                    close();
                    Discard();
                    next();
                });
            });
        },
        () => _list?.FocusChosen())
        { Width = TimbnLength.Pixels(_questionWidth) };
        TimbnCorePlugin.Instance!.UI.ShowPage(ask, overOthers: true);
    }

    // Both ask first, so the player sees that something happened.
    private void AskSave()
    {
        if (!HasMod || !IsDirty)
            return;

        TimbnCorePlugin.Instance!.UI.ShowConfirm(TimbnModsText.SaveTitle, string.Format(TimbnModsText.SaveMessage, Mod.Name), () => SaveDraft()?.Invoke());
    }

    private void AskDefaults()
    {
        if (HasMod)
            TimbnCorePlugin.Instance!.UI.ShowConfirm(TimbnModsText.DefaultsTitle, string.Format(TimbnModsText.DefaultsMessage, Mod.Name), ResetToDefaults);
    }

    private void ResetToDefaults()
    {
        if (!HasMod)
            return;

        foreach (var row in TimbnModsPage.Resettable(Mod, TimbnModsWindowHost.ShowAdvanced.Value))
            _draft.Set(row.Entry, row.Entry.DefaultValue);

        _page!.Refresh();
    }

    // Saves the edits and hands back the restart notice when Enabled was one of them, so a caller that closes the window
    // or switches mods after saving can show it last.
    private Action? SaveDraft()
    {
        var notice = RestartNotice();
        _draft.Save();
        _page!.Refresh();
        return notice;
    }

    // Core only reads Enabled when the game starts, so a mod turned off keeps running and one turned on stays off until then.
    private Action? RestartNotice()
    {
        if (!HasMod || Mod.Enabled is not { } enabled || !_draft.Changes(enabled))
            return null;

        var state = (bool)_draft.Get(enabled) ? TimbnModsText.On : TimbnModsText.Off;
        var message = string.Format(TimbnModsText.RestartMessage, Mod.Name, state);
        return () => TimbnCorePlugin.Instance!.UI.ShowMessage(TimbnModsText.RestartTitle, message);
    }

    private void Discard()
    {
        _draft.Discard();
        _page!.Refresh();
    }
}
