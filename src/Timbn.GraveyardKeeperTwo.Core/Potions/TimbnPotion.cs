namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a potion for a plugin's Potions.Register. The potion becomes a real item, buff, and alchemy
/// formula, and its buff decides what drinking it does.
/// </summary>
public sealed class TimbnPotion
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Id of the existing usable item the new one is copied from.</summary>
    public string TemplateItemId { get; set; } = "third_eye_salt";

    /// <summary>Item icon sprite name. Any sprite the game has, or one added with the plugin's Sprites.AddPng. Empty keeps the template's.</summary>
    public string IconId { get; set; } = "";

    public int Price { get; set; } = 100;

    /// <summary>Red, green, and blue rune counts of the formula. They must not match any other formula.</summary>
    public Vector3Int Runes { get; set; }

    public AlchemyFormulaTab Tab { get; set; } = AlchemyFormulaTab.Epic;

    public bool HiddenAtStart { get; set; }

    public TimbnPotionBuff Buff { get; set; } = new();

    public string BuffId => $"{Id}_buff";
}

/// <summary>The buff a potion applies, and what it does.</summary>
public sealed class TimbnPotionBuff
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Func<float> Seconds { get; set; } = () => 60f;

    /// <summary>One of the game's buff colours, used for the start burst and the HUD frame.</summary>
    public Func<string> GlowColor { get; set; } = () => "ltyellow";

    /// <summary>Buff icon sprite name. Any sprite the game has, or one added with the plugin's Sprites.AddPng.</summary>
    public string IconId { get; set; } = "b_speed";

    /// <summary>Game script expressions run once when the buff is added. Use these for changes that are saved, like items or unlocks.</summary>
    public List<string> OnAddExpressions { get; set; } = [];

    /// <summary>Game script expressions run once when the buff is removed.</summary>
    public List<string> OnRemoveExpressions { get; set; } = [];

    /// <summary>Runs when the buff starts, and again when a save loads with it active. Use it for effects that are not saved.</summary>
    public Action? OnStart { get; set; }

    /// <summary>Runs every frame while the buff lasts.</summary>
    public Action? WhileActive { get; set; }

    /// <summary>Runs when the buff ends, the player leaves the game, or the potion is unregistered.</summary>
    public Action? OnEnd { get; set; }
}
