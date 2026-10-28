namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// What a sprite added with Sprites.Add is for. Each kind has one size to give and one way Core places it, because the game
/// draws item icons, blueprints and perks on canvases of their own and stretches any sprite that is not that size. An image
/// that breaks its kind's rule is skipped with a warning and the game shows its own default instead.
/// </summary>
public enum TimbnSpriteKind
{
    /// <summary>A picture, kept at the image's own size and never rejected. Use it for anything the kinds below don't cover, such as a page picture or a quest portrait.</summary>
    Image,

    /// <summary>
    /// An item icon, any size up to 32 by 32, centered on the game's 48 by 48 icon canvas. Up to 28 by 28 fits the inventory,
    /// the tech tree and the build list. A larger icon reaches past the inventory slot and the tech tree node, as the game's
    /// Universal Bag does. An image over 32 by 32 is skipped.
    /// </summary>
    Item,

    /// <summary>A station's build icon, exactly 28 by 28 like the game's blueprint tiles, centered on the 48 by 48 canvas with a 10 pixel margin. Any other size is skipped.</summary>
    Blueprint,

    /// <summary>A perk icon, exactly 26 by 26, centered on the game's 30 by 30 perk canvas. The perk slot's border covers the outer 2 pixels of that canvas. Any other size is skipped.</summary>
    Perk,

    /// <summary>A buff icon, exactly 26 by 26, kept at that size. The HUD draws a buff at its own size and spaces buffs 30 apart. Any other size is skipped.</summary>
    Buff,
}
