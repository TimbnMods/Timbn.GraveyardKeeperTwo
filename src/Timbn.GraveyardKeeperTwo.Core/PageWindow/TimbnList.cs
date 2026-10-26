namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>One line of a <see cref="TimbnList"/>, a title with up to two smaller lines under it.</summary>
/// <param name="title">The line's name, already in the player's language.</param>
/// <param name="subtitle">A smaller line under the name, or empty for none.</param>
/// <param name="detail">A smaller line under the subtitle, or empty for none.</param>
public sealed class TimbnListEntry(string title, string subtitle = "", string detail = "")
{
    /// <summary>The line's name.</summary>
    public string Title { get; } = title;

    /// <summary>The smaller line under the name.</summary>
    public string Subtitle { get; } = subtitle;

    /// <summary>The smaller line under the subtitle.</summary>
    public string Detail { get; } = detail;
}

/// <summary>
/// A column of lines the player picks one from, each on the game's save slot card, with the chosen one lit. A click or A
/// picks a line. On a gamepad, LT and RT step to the previous and next line and move the focus onto it, from anywhere on
/// the page, when the page does not use the triggers to step its controls (<see cref="TimbnPage.StepWithTriggers"/> false). It is as tall as all its lines, so put it in
/// a <see cref="TimbnScroll"/> when there may be more than fit.
/// </summary>
/// <example>
/// <code>
/// page.Scroll(list => list.Add(new TimbnList(_recipes.Select(r => new TimbnListEntry(r.Name, r.Kind)).ToList(), () => _chosen, index => _chosen = index)));
/// </code>
/// </example>
/// <param name="entries">The lines, top to bottom.</param>
/// <param name="get">Reads the index of the chosen line.</param>
/// <param name="set">Called with the index of the line the player picked.</param>
public sealed class TimbnList(IReadOnlyList<TimbnListEntry> entries, Func<int> get, Action<int> set) : TimbnControl
{
    /// <summary>
    /// Whether the page's hint bar names LT and RT for the list while a gamepad is in use, true by default. Set it to false
    /// for a list whose header shows the triggers' glyphs itself, such as a <see cref="TimbnSection"/>
    /// with the trigger keys. Set it before the control is put on a page.
    /// </summary>
    public bool ShowsHint { get; set; } = true;

    /// <summary>
    /// Called with the index after the player picks a line with a click or A, and not when LT and RT step the list. It is
    /// the place to move the gamepad's focus into what the pick shows, with <see cref="TimbnRegion.FocusFirst"/>.
    /// </summary>
    public Action<int>? Picked { get; set; }

    /// <summary>
    /// Whether moving the gamepad's focus over a line picks it, so what the list shows follows the focus, such as the page
    /// of the mod under the cursor. A click or A still calls <see cref="Picked"/>, and the focus moving does not. False by
    /// default. Set it before the control is put on a page.
    /// </summary>
    public bool SelectsOnFocus { get; set; }

    /// <summary>
    /// Whether a gamepad's focus starts on the chosen line when the page opens, and comes back to it when the gamepad is
    /// picked up after the mouse with nothing focused before, instead of on the page's first control. False by default.
    /// Set it on one list of a page, before it is put on the page.
    /// </summary>
    public bool FocusWhenShown { get; set; }

    /// <summary>
    /// With a gamepad in use, puts the focus on the chosen line, such as when the player backs out of what the line shows.
    /// With a mouse in use it is where the gamepad's focus starts once the player picks one up.
    /// </summary>
    public void FocusChosen()
    {
        if (Widget is TimbnListWidget list)
            list.FocusChosen();
    }

    internal override TimbnWidget BuildWidget(Transform parent)
    {
        var list = new TimbnListWidget(parent, entries, get, set, Watcher!.Logger) { ShowsHint = ShowsHint, Picked = Picked, FocusWhenShown = FocusWhenShown };
        if (SelectsOnFocus)
            Watcher.WatchValue(() => list.FocusedIndex, list.FollowFocus);

        return list;
    }
}
