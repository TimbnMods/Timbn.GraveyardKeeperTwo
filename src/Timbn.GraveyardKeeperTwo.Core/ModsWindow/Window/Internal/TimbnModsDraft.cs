using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The unsaved edits to one mod's config while the Mods window shows it. Controls write here instead of into the
/// entries, so the mod sees nothing until Save puts every edit into its entries, where it reacts as to any change,
/// and writes the file once. Discard drops the edits, and the entries were never touched. A setting of another mod,
/// such as a group mate's Enabled, is read and written straight, and a draft with no config holds no edits at all.
/// </summary>
internal sealed class TimbnModsDraft
{
    private readonly ConfigFile? _config;
    private readonly Dictionary<ConfigEntryBase, object> _pending = [];

    public TimbnModsDraft(ConfigFile? config)
    {
        _config = config;
    }

    public bool IsDirty => _pending.Count > 0;

    public bool Changes(ConfigEntryBase entry) => _pending.ContainsKey(entry);

    public object Get(ConfigEntryBase entry) => _pending.TryGetValue(entry, out var value) ? value : entry.BoxedValue;

    // An edit back to the saved value is no edit at all.
    public void Set(ConfigEntryBase entry, object value)
    {
        if (!Owns(entry))
        {
            entry.BoxedValue = value;
            return;
        }

        value = entry.Description?.AcceptableValues?.Clamp(value) ?? value;
        if (Equals(value, entry.BoxedValue))
            _pending.Remove(entry);
        else
            _pending[entry] = value;
    }

    // The mod's own ObjToStr when it has one, else the text BepInEx writes to the file, else whatever the value prints.
    public string GetText(ConfigEntryBase entry, Func<object, string>? toText = null) => TimbnModsValues.TextOf(entry, Get(entry), toText);

    public void SetText(ConfigEntryBase entry, string text, Func<string, object>? fromText = null, ManualLogSource? logger = null)
    {
        if (TimbnModsValues.TryRead(entry, text, fromText, logger, out var value))
            Set(entry, value);
    }

    public bool IsDefault(ConfigEntryBase entry) => TimbnModsValues.IsDefault(entry, Get(entry));

    public void Save()
    {
        if (_config == null)
            return;

        var saveOnSet = _config.SaveOnConfigSet;
        _config.SaveOnConfigSet = false;
        try
        {
            foreach (var pair in _pending.ToList())
                pair.Key.BoxedValue = pair.Value;
        }
        finally
        {
            _config.SaveOnConfigSet = saveOnSet;
        }

        _config.Save();
        _pending.Clear();
    }

    public void Discard() => _pending.Clear();

    private bool Owns(ConfigEntryBase entry) => _config != null && entry.ConfigFile == _config;
}

/// <summary>How the Mods window turns a setting's value into text and back, and tells a default value.</summary>
internal static class TimbnModsValues
{
    // Compared by the text BepInEx writes to the file, since a value read back from it can differ from the default in code
    // by less than the file keeps (a Color's 0.5 comes back as 128/255).
    internal static bool IsDefault(ConfigEntryBase entry, object value)
    {
        if (Equals(value, entry.DefaultValue))
            return true;

        try
        {
            return TomlTypeConverter.ConvertToString(value, entry.SettingType) == TomlTypeConverter.ConvertToString(entry.DefaultValue, entry.SettingType);
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static string TextOf(ConfigEntryBase entry, object value, Func<object, string>? toText)
    {
        try
        {
            return toText != null ? toText(value) : TomlTypeConverter.ConvertToString(value, entry.SettingType);
        }
        catch (Exception)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    // Text that does not read as the setting's type is ignored, the way BepInEx ignores it in a hand edited file. A mod's
    // own StrToObj that throws or gives nothing keeps the old value too, and is logged since it is the mod's bug.
    internal static bool TryRead(ConfigEntryBase entry, string text, Func<string, object>? fromText, ManualLogSource? logger, out object value)
    {
        object? read;
        try
        {
            read = fromText != null ? fromText(text) : TomlTypeConverter.ConvertToValue(text, entry.SettingType);
        }
        catch (Exception exception)
        {
            if (fromText != null)
                logger?.LogWarning($"{entry.Definition}|StrToObj could not read \"{text}\". {exception.Message}");

            value = null!;
            return false;
        }

        if (read == null)
            logger?.LogWarning($"{entry.Definition}|StrToObj gave nothing for \"{text}\".");

        value = read!;
        return read != null;
    }
}
