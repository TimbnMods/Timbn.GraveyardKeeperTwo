using LazyBearTechnology;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds one play of a quest whose details change each time, such as which villager asks and for what. A quest's Setup
/// fills it when the mod calls NewRun. Core keeps it with the save until the play ends. Every part of the quest
/// that takes a run reads it. Each value has a name. Text anywhere in the quest shows a value with its name in braces,
/// such as {dish}. An NPC shows as their name, an item as its name and a number as itself. A text value shows its
/// line. Names and lines appear in the player's language.
/// </summary>
/// <example>
/// <code><![CDATA[
/// .Setup(run =>
/// {
///     run.Npc("villager", TimbnGameIds.Npcs.Larry).Item("dish", "cook_muffin").Number("count", 2);
///     return true;
/// })
/// .Description("Bring {villager} {count} {dish}.")
/// .Cost((cost, run) => cost.Item(run.Item("dish"), run.Number("count")))
/// ]]></code>
/// </example>
public sealed class TimbnQuestRun
{
    private static readonly Regex _name = new("^[a-z][a-z0-9_]*$");

    internal TimbnQuestRun(TimbnQuestRunData data)
    {
        Data = data;
    }

    internal TimbnQuestRunData Data { get; }

    /// <summary>The game day the run was rolled on, as <see cref="TimbnClock.Day"/> counts them.</summary>
    public int Day => Data.Day;

    /// <summary>Sets an NPC value, shown in text as the NPC's name.</summary>
    /// <param name="name">A short name of lowercase letters, digits and underscores, starting with a letter.</param>
    /// <param name="npcId">The NPC, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <returns>This run, for chaining.</returns>
    public TimbnQuestRun Npc(string name, string npcId) => Set(Data.Npcs, name, npcId);

    /// <summary>Sets an item value, shown in text as the item's name.</summary>
    /// <param name="name">A short name of lowercase letters, digits and underscores, starting with a letter.</param>
    /// <param name="itemId">The item id, with its quality suffix where it has one, such as grape_wine:2.</param>
    /// <returns>This run, for chaining.</returns>
    public TimbnQuestRun Item(string name, string itemId) => Set(Data.Items, name, itemId);

    /// <summary>Sets a number value, shown in text as the number.</summary>
    /// <param name="name">A short name of lowercase letters, digits and underscores, starting with a letter.</param>
    /// <param name="value">The number.</param>
    /// <returns>This run, for chaining.</returns>
    public TimbnQuestRun Number(string name, int value) => Set(Data.Numbers, name, value);

    /// <summary>
    /// Sets a text value, the localization key of a line that shows in place of {name}, so one quest can say a
    /// different thing on each run, such as an answer that names what the player brings. Add the line with Text.Add or
    /// a lang file, and a translation of it shows in its place.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Text.Add("my_errand_potion", "I have some medication for you.");
    /// ...
    /// .Setup(run => { run.Text("delivering", "my_errand_potion"); return true; })
    /// .Talk(talk => talk.Accept("{delivering}"))
    /// ]]></code>
    /// </example>
    /// <param name="name">A short name of lowercase letters, digits and underscores, starting with a letter.</param>
    /// <param name="key">The line's localization key.</param>
    /// <returns>This run, for chaining.</returns>
    public TimbnQuestRun Text(string name, string key) => Set(Data.Texts, name, key);

    /// <summary>Reads a text value set in Setup.</summary>
    /// <param name="name">The value's name.</param>
    /// <returns>The line's localization key.</returns>
    public string Text(string name) => Get(Data.Texts, name, nameof(Text));

    /// <summary>Reads an NPC value set in Setup.</summary>
    /// <param name="name">The value's name.</param>
    /// <returns>The NPC id.</returns>
    public string NpcId(string name) => Get(Data.Npcs, name, nameof(NpcId));

    /// <summary>Reads an NPC value set in Setup, the same as <see cref="NpcId"/>.</summary>
    /// <param name="name">The value's name.</param>
    /// <returns>The NPC id.</returns>
    public string Npc(string name) => NpcId(name);

    /// <summary>Reads an item value set in Setup.</summary>
    /// <param name="name">The value's name.</param>
    /// <returns>The item id.</returns>
    public string Item(string name) => Get(Data.Items, name, nameof(Item));

    /// <summary>Reads a number value set in Setup.</summary>
    /// <param name="name">The value's name.</param>
    /// <returns>The number.</returns>
    public int Number(string name) => Get(Data.Numbers, name, nameof(Number));

    internal string? Shown(string name)
    {
        if (Data.Npcs.TryGetValue(name, out var npc))
            return $"#({npc})";

        if (Data.Items.TryGetValue(name, out var item))
            return $"#({(LLBase.HasL(item) || !item.Contains(':') ? item : item.Split(':')[0])})";

        if (Data.Numbers.TryGetValue(name, out var number))
            return number.ToString(CultureInfo.CurrentCulture);

        if (Data.Texts.TryGetValue(name, out var text))
            return $"#({text})";

        return null;
    }

    private TimbnQuestRun Set<T>(Dictionary<string, T> values, string name, T value)
    {
        if (!_name.IsMatch(name))
            throw new ArgumentException($"The run value name '{name}' can only hold lowercase letters, digits and underscores, and starts with a letter.");

        var kinds = (Data.Npcs.ContainsKey(name) ? 1 : 0) + (Data.Items.ContainsKey(name) ? 1 : 0) + (Data.Numbers.ContainsKey(name) ? 1 : 0) + (Data.Texts.ContainsKey(name) ? 1 : 0);
        if (kinds > (values.ContainsKey(name) ? 1 : 0))
            throw new ArgumentException($"The run value '{name}' is already set as another kind of value.");

        values[name] = value;
        return this;
    }

    private static T Get<T>(Dictionary<string, T> values, string name, string kind) =>
        values.TryGetValue(name, out var value)
            ? value
            : throw new KeyNotFoundException($"The run has no {kind.ToLowerInvariant()} value named '{name}'. Set it in the quest's Setup.");
}
