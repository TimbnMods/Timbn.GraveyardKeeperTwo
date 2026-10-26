using BepInEx.Bootstrap;
using System.Globalization;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal enum TimbnModsRowKind
{
    Switch,
    Slider,
    Text,
    Key,
    Readonly,
}

internal sealed class TimbnModsPlugin
{
    public TimbnModsPlugin(string name, string version, string guid, ConfigFile config, bool isTimbn, Func<string, bool>? isFeatureBroken = null)
    {
        Name = name;
        Version = version;
        Guid = guid;
        Config = config;
        IsTimbn = isTimbn;
        IsFeatureBroken = isFeatureBroken;
    }

    public string Name { get; }

    public string Version { get; }

    public string Guid { get; }

    public ConfigFile Config { get; }

    public bool IsTimbn { get; }

    public Func<string, bool>? IsFeatureBroken { get; }
}

internal sealed class TimbnModsMod
{
    public TimbnModsMod(string name, string version, string guid, bool isTimbn, ConfigFile config, ConfigEntry<bool>? enabled, IReadOnlyList<TimbnModsSection> sections)
    {
        Name = name;
        Version = version;
        Guid = guid;
        IsTimbn = isTimbn;
        Config = config;
        Enabled = enabled;
        Sections = sections;
    }

    public string Name { get; }

    public string Version { get; }

    public string Guid { get; }

    public bool IsTimbn { get; }

    public ConfigFile Config { get; }

    public ConfigEntry<bool>? Enabled { get; }

    public IReadOnlyList<TimbnModsSection> Sections { get; }

    public bool HasAdvanced => Sections.Any(section => section.Rows.Any(row => row.IsAdvanced));
}

internal sealed class TimbnModsSection
{
    public TimbnModsSection(string name, IReadOnlyList<TimbnModsRow> rows)
    {
        Name = name;
        Rows = rows;
    }

    public string Name { get; }

    public IReadOnlyList<TimbnModsRow> Rows { get; }
}

internal sealed class TimbnModsRow
{
    public TimbnModsRow(ConfigEntryBase entry, TimbnModsRowKind kind, string label, string description, bool isBroken)
    {
        Entry = entry;
        Kind = kind;
        Label = label;
        Description = description;
        IsBroken = isBroken;
    }

    public ConfigEntryBase Entry { get; }

    public TimbnModsRowKind Kind { get; }

    public string Label { get; }

    public string Description { get; }

    public bool IsBroken { get; }

    public IReadOnlyList<string>? Options { get; set; }

    // What each option shows, when that differs from the value saved, such as an enum value's translated name.
    public IReadOnlyList<string>? OptionNames { get; set; }

    public object? Min { get; set; }

    public object? Max { get; set; }

    // Tagged HideDefaultButton, so the row gets no button to put it back to its default.
    public bool HidesReset { get; set; }

    // Tagged IsAdvanced, so it only shows once the player turns on advanced settings.
    public bool IsAdvanced { get; set; }

    // Shows its value in its usual control, grayed out, since it is tagged ReadOnly or has no control of its own here.
    public bool IsReadOnly { get; set; }

    // The marks shown under the setting's name, such as Read Only and Advanced.
    public List<string> Tags { get; } = [];

    // A mod's own conversion between the value and the text shown and typed, from its ObjToStr and StrToObj tags.
    public Func<object, string>? ToText { get; set; }

    public Func<string, object>? FromText { get; set; }
}

/// <summary>
/// Turns each loaded plugin's config file into the mods, sections and rows the Mods window draws. Sections come in the
/// order the plugin bound them, and each entry picks a row kind from its type and accepted values. It
/// honors the setting tags (Browsable, DispName, Order, ReadOnly, IsAdvanced, HideDefaultButton, ObjToStr, StrToObj and
/// CustomDrawer), read by name from any tag object. Settings tagged
/// IsAdvanced stay in their section, marked so the page can hide them. It holds no game state so it can be tested.
/// </summary>
internal static class TimbnModsCatalog
{
    private const string _enabledSection = "General";
    private const string _enabledKey = "Enabled";

    private static readonly HashSet<Type> _numberTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal),
    ];

    internal static IReadOnlyList<TimbnModsMod> FromChainloader() =>
        Build(Chainloader.PluginInfos.Values
            .Where(info => info?.Instance != null)
            .Select(info => new TimbnModsPlugin(
                info.Metadata.Name,
                info.Metadata.Version.ToString(),
                info.Metadata.GUID,
                info.Instance.Config,
                info.Instance is TimbnFrameworkPlugin,
                info.Instance is TimbnFrameworkPlugin timbn ? timbn.IsFeatureBroken : null)));

    internal static IReadOnlyList<TimbnModsMod> Build(IEnumerable<TimbnModsPlugin> plugins) =>
        [.. plugins
            .Select(BuildMod)
            .OrderByDescending(mod => mod.IsTimbn)
            .ThenBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase)];

    internal static TimbnModsMod BuildMod(TimbnModsPlugin plugin)
    {
        ConfigEntry<bool>? enabled = null;
        var sections = new TimbnModsSectionBuilder();
        foreach (var entry in plugin.Config.Select(pair => pair.Value))
        {
            // A hidden entry gets nothing, so Core's Enabled, which is hidden since Core is always on, gets no header switch.
            if (IsHidden(entry))
                continue;

            var definition = entry.Definition;
            if (plugin.IsTimbn && definition.Section == _enabledSection && definition.Key == _enabledKey && entry is ConfigEntry<bool> toggle)
            {
                enabled = toggle;
                continue;
            }

            var broken = plugin.IsFeatureBroken?.Invoke(definition.Key) == true;
            sections.Add(definition.Section, RowFor(entry, broken), OrderOf(entry));
        }

        return new TimbnModsMod(plugin.Name, plugin.Version, plugin.Guid, plugin.IsTimbn, plugin.Config, enabled, sections.Build());
    }

    // One setting's row the way the default page shows it, labeled by its DispName tag or else by its key as written.
    internal static TimbnModsRow RowFor(ConfigEntryBase entry, bool broken = false)
    {
        var tags = Tags(entry);
        var label = ReadTag<string>(tags, "DispName") is { Length: > 0 } name ? name : entry.Definition.Key;
        var row = BuildRow(entry, label, broken);
        row.ToText = ReadTag<Func<object, string>>(tags, "ObjToStr");
        row.FromText = ReadTag<Func<string, object>>(tags, "StrToObj");
        row.IsAdvanced = ReadTag<bool?>(tags, "IsAdvanced") == true;
        row.HidesReset = ReadTag<bool?>(tags, "HideDefaultButton") == true;
        row.IsReadOnly = row.Kind == TimbnModsRowKind.Readonly || ReadTag<bool?>(tags, "ReadOnly") == true;
        if (row.IsReadOnly)
            row.Tags.Add(TimbnModsText.ReadOnly);

        if (row.IsAdvanced)
            row.Tags.Add(TimbnModsText.Advanced);

        return row;
    }

    private static bool IsHidden(ConfigEntryBase entry) => ReadTag<bool?>(Tags(entry), "Browsable") == false;

    private static int OrderOf(ConfigEntryBase entry) => ReadTag<int?>(Tags(entry), "Order") ?? 0;

    private static object[] Tags(ConfigEntryBase entry) => entry.Description?.Tags ?? [];

    internal static TimbnModsRow BuildRow(ConfigEntryBase entry, string label, bool broken)
    {
        var type = entry.SettingType;
        var accepted = entry.Description?.AcceptableValues;
        var description = entry.Description?.Description ?? string.Empty;
        var tags = Tags(entry);

        // A CustomDrawer paints with IMGUI, which this window cannot host, so the value is only shown.
        if (ReadTag<object>(tags, "CustomDrawer") != null)
            return new TimbnModsRow(entry, TimbnModsRowKind.Readonly, label, description, broken);

        if (type == typeof(bool))
            return new TimbnModsRow(entry, TimbnModsRowKind.Switch, label, description, broken);

        // A combination such as "A, B" is not one of the names, so stepping would overwrite it.
        if (type.IsEnum && type.IsDefined(typeof(FlagsAttribute), false))
            return new TimbnModsRow(entry, TimbnModsRowKind.Readonly, label, description, broken);

        if (type.IsEnum)
        {
            // Declaration order, since Enum.GetNames puts a negative value after the positive ones.
            var names = type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => field.Name).ToArray();
            return new TimbnModsRow(entry, TimbnModsRowKind.Switch, label, description, broken) { Options = names, OptionNames = [.. names.Select(name => TimbnModsText.Translated($"{type.FullName}.{name}", name))] };
        }

        if (accepted != null && accepted.GetType().GetProperty("AcceptableValues")?.GetValue(accepted) is Array values)
        {
            var options = values.Cast<object>().Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).ToList();
            return new TimbnModsRow(entry, TimbnModsRowKind.Switch, label, description, broken) { Options = options };
        }

        if (_numberTypes.Contains(type) && accepted != null && accepted.GetType().GetProperty("MinValue") is { } min)
        {
            return new TimbnModsRow(entry, TimbnModsRowKind.Slider, label, description, broken)
            {
                Min = min.GetValue(accepted),
                Max = accepted.GetType().GetProperty("MaxValue")?.GetValue(accepted),
            };
        }

        if (_numberTypes.Contains(type) || type == typeof(string))
            return new TimbnModsRow(entry, TimbnModsRowKind.Text, label, description, broken);

        if (type == typeof(KeyboardShortcut))
            return new TimbnModsRow(entry, TimbnModsRowKind.Key, label, description, broken);

        // A type with no editor of its own can still be typed when the mod says how to turn it into text and back.
        var typed = ReadTag<Func<object, string>>(tags, "ObjToStr") != null && ReadTag<Func<string, object>>(tags, "StrToObj") != null;
        return new TimbnModsRow(entry, typed ? TimbnModsRowKind.Text : TimbnModsRowKind.Readonly, label, description, broken);
    }

    private static T? ReadTag<T>(object[] tags, string name)
    {
        foreach (var tag in tags)
        {
            if (tag == null)
                continue;

            var type = tag.GetType();
            var value = type.GetField(name)?.GetValue(tag) ?? type.GetProperty(name)?.GetValue(tag);
            if (value is T typed)
                return typed;
        }

        return default;
    }
}

// Rows gathered by section in the order the sections first appear, each section's rows sorted by their Order tag.
internal sealed class TimbnModsSectionBuilder
{
    private readonly List<string> _sections = [];
    private readonly Dictionary<string, List<(TimbnModsRow Row, int Order)>> _rows = [];

    public void Add(string section, TimbnModsRow row, int order)
    {
        if (!_rows.TryGetValue(section, out var rows))
        {
            rows = [];
            _rows[section] = rows;
            _sections.Add(section);
        }

        rows.Add((row, order));
    }

    public IReadOnlyList<TimbnModsSection> Build() =>
        [.. _sections.Select(section => new TimbnModsSection(section, [.. _rows[section].OrderByDescending(row => row.Order).Select(row => row.Row)]))];
}
