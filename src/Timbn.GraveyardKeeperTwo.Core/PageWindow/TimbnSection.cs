using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A brown plate with a title centered on it and an ornament at each end, like the Character window's Buffs header, that
/// heads the part of a page under it. With a key, its glyph sits at that end of the plate while a gamepad is in use, such as
/// the triggers that step a list.
/// </summary>
/// <param name="title">The plate's text, already in the player's language.</param>
/// <param name="leftKey">The key whose glyph shows at the left end with a gamepad, or null for none.</param>
/// <param name="rightKey">The key whose glyph shows at the right end with a gamepad, or null for none.</param>
public sealed class TimbnSection(string title, GameKey? leftKey = null, GameKey? rightKey = null) : TimbnComponent
{
    protected internal override RectTransform Build(Transform parent)
    {
        var section = TimbnWidgets.HangingSection(parent, title);
        if ((leftKey is not null || rightKey is not null) && Watcher != null)
            TimbnWidgets.SectionKeys(section, leftKey, rightKey, Watcher);

        return (RectTransform)section.Root.parent;
    }
}
