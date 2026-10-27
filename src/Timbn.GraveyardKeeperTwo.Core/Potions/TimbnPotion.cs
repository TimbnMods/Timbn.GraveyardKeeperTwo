namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a potion for Potions.Add. The potion becomes a real item, buff and alchemy formula. Its buff decides
/// what drinking it does.
/// </summary>
public sealed class TimbnPotion
{
    /// <summary>The potion's item id, such as my_mod_swift_potion. Keep it stable, since saves keep the item under it.</summary>
    public string Id { get; set; } = "";

    /// <summary>The item's name as the player sees it, such as Swift Potion.</summary>
    public string Name { get; set; } = "";

    /// <summary>The item's description in its tooltip, such as what drinking it does.</summary>
    public string Description { get; set; } = "";

    /// <summary>Id of the existing usable item the new one is copied from.</summary>
    public string TemplateItemId { get; set; } = TimbnGameIds.Items.ThirdEyeSalt;

    /// <summary>The item's icon sprite name. Use any sprite the game has or one added with Sprites.AddEmbeddedPng. Empty keeps the template's.</summary>
    public string IconId { get; set; } = "";

    /// <summary>The item's base price in the game's raw units, where 100 is one silver coin. 100 by default.</summary>
    public int Price { get; set; } = 100;

    /// <summary>Red, green, and blue rune counts of the formula. They must not match any other formula.</summary>
    public Vector3Int Runes { get; set; }

    /// <summary>The tab of the alchemy formula window the formula lists under. Epic by default.</summary>
    public AlchemyFormulaTab Tab { get; set; } = AlchemyFormulaTab.Epic;

    /// <summary>Whether the formula starts hidden, so the player has to discover it. Shown from the start by default.</summary>
    public bool HiddenAtStart { get; set; }

    /// <summary>What drinking the potion does.</summary>
    public TimbnPotionBuff Buff { get; set; } = new();

    /// <summary>The id of the buff drinking it gives, the potion's <see cref="Id"/> with _buff on the end.</summary>
    public string BuffId => $"{Id}_buff";
}

/// <summary>The buff a potion applies, and what it does.</summary>
public sealed class TimbnPotionBuff
{
    /// <summary>The buff's name in the HUD and its tooltip, such as Swift.</summary>
    public string Name { get; set; } = "";

    /// <summary>The buff's description in its tooltip.</summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// How long the buff lasts in real seconds, read each time the potion is drunk so it can follow a setting. 60 by
    /// default.
    /// </summary>
    public Func<float> DurationSeconds { get; set; } = () => 60f;

    /// <summary>One of the game's buff colors, used for the start burst and the HUD frame. Read each time the potion is drunk so it can follow a setting.</summary>
    public Func<string> GlowColor { get; set; } = () => "ltyellow";

    /// <summary>The buff's icon sprite name. Use any sprite the game has or one added with Sprites.AddEmbeddedPng.</summary>
    public string IconId { get; set; } = "b_speed";

    /// <summary>
    /// The game's script expressions, run once by the game when the buff starts, such as DropItem("stick", 5). Use
    /// these for changes the save keeps, like items or unlocks. Each is checked when the potion is registered, and a
    /// potion with one that doesn't work is skipped with a warning.
    /// </summary>
    public List<string> StartExpressions { get; set; } = [];

    /// <summary>
    /// The game's script expressions, run once by the game when the buff ends, such as taking back what
    /// <see cref="StartExpressions"/> gave. If the mod is removed while the buff lasts, Core ends the buff the next time the
    /// save loads and runs these then.
    /// </summary>
    public List<string> EndExpressions { get; set; } = [];

    /// <summary>
    /// Runs when the buff starts, and again when a save loads with it active. Use it for effects the save doesn't
    /// keep, like walk speed. Use <see cref="ApplyChange"/> for C# changes the save would keep.
    /// </summary>
    public Action? OnStart { get; set; }

    /// <summary>Runs every frame while the buff lasts.</summary>
    public Action? WhileActive { get; set; }

    /// <summary>Runs when the buff ends, the player leaves the game, or the potion is unregistered.</summary>
    public Action? OnEnd { get; set; }

    /// <summary>
    /// Makes a C# change the save would keep, such as a vendor's money. Core runs it when the buff starts.
    /// <see cref="RevertChange"/> takes it back just before every save, and Core runs this again right after. That way
    /// the save never holds the change. Removing the mod mid buff leaves nothing behind. Both run on every save. Keep
    /// them to setting state with no sounds, toasts or drops.
    /// </summary>
    public Action? ApplyChange { get; set; }

    /// <summary>Takes back what <see cref="ApplyChange"/> changed. Runs when the buff ends and just before every save.</summary>
    public Action? RevertChange { get; set; }
}
