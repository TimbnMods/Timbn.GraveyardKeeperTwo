namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gets the pages ready before the player sees one. The first time a page window or a control is built the game compiles
/// its code, which a mod's first page would otherwise pay for in a frame the player sees. It builds each mod's settings
/// one a frame, then opens the Mods window and a small page with the controls mods use most, closing each in the same
/// frame so nothing is drawn. This happens once, as soon as the game can show a page and before a save is loaded.
/// </summary>
internal static class TimbnControlWarmUp
{
    private static bool _done;

    private enum Choice
    {
        First,
        Second,
    }

    // Called every frame from Core's update.
    internal static void Tick()
    {
        if (_done)
            return;

        TimbnModsCatalog.Warm();
        var ui = TimbnCorePlugin.Instance!.UI;
        if (!TimbnModsCatalog.IsWarm || TimbnGame.IsInGame || !ui.CanShowPage)
            return;

        _done = true;
        TimbnModsScreen.WarmUp();
        ControlsPage(ui);
    }

    private static void ControlsPage(TimbnPluginUI ui)
    {
        var text = string.Empty;
        string? color = null;
        var choice = Choice.First;
        var on = false;
        var amount = 0f;
        using var handle = ui.ShowPage(
            string.Empty,
            (page, close) =>
            {
                page.Add(new TimbnTextBox(() => text, value => text = value));
                page.Add(new TimbnColorPicker(["#FFFFFF"], () => color, value => color = value));
                page.Add(new TimbnChoice<Choice>(() => choice, value => choice = value));
                page.Add(new TimbnToggle(() => on, value => on = value));
                page.Add(new TimbnSlider(0f, 10f, () => amount, value => amount = value));
                page.ButtonFooter(footer => footer.AddPrimarySpecial(string.Empty, close));
            },
            width: TimbnLength.Pixels(180));
    }
}
