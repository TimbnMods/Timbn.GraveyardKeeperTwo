namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// How a setting shows in the Mods window, passed as a tag of its <c>ConfigDescription</c>. Leave a field null to keep the
/// usual behavior. The Mods window reads the same names from any other tag object too.
/// </summary>
/// <example>
/// <code>
/// config.Bind("General", "Seed", 42, new ConfigDescription("The world's seed.", null, new TimbnSettingTags { ReadOnly = true }));
/// </code>
/// </example>
public sealed class TimbnSettingTags
{
    /// <summary>The name the setting shows under in place of its key.</summary>
    public string? DispName;

    /// <summary>Where the setting sits in its section, higher first. Settings with the same order keep the order they were bound in.</summary>
    public int? Order;

    /// <summary>True to show the value without letting it be changed.</summary>
    public bool? ReadOnly;

    /// <summary>True to show the setting only once the player turns on advanced settings.</summary>
    public bool? IsAdvanced;

    /// <summary>False to leave the setting out of the Mods window, as <see cref="TimbnConfig.BindHidden"/> does.</summary>
    public bool? Browsable;

    /// <summary>True for a setting with no reset button, which Default All also leaves alone.</summary>
    public bool? HideDefaultButton;

    /// <summary>Turns the value into the text shown and typed, for a type the Mods window has no control for. Give <see cref="StrToObj"/> with it.</summary>
    public Func<object, string>? ObjToStr;

    /// <summary>Reads typed text back into a value, the other half of <see cref="ObjToStr"/>. Return null to keep the old value.</summary>
    public Func<string, object>? StrToObj;
}
