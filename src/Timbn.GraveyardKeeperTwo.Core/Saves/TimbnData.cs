using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>One plugin's section in a mod data file, read, written and reset by Core.</summary>
internal interface ITimbnSaveEntry
{
    string Id { get; }

    void Load(JToken? section);

    JToken Serialize();

    void Reset();
}

/// <summary>
/// A plugin's own data object that Core reads from and writes to disk. <see cref="TimbnSaveData{T}"/> belongs to a
/// save and <see cref="TimbnGlobalData{T}"/> to the player, and both hand out the data through <see cref="Current"/>.
/// </summary>
/// <typeparam name="T">The plugin's data type. It needs a public constructor with no parameters.</typeparam>
public abstract class TimbnData<T> : ITimbnSaveEntry where T : class, new()
{
    private readonly Action<T>? _loaded;

    internal TimbnData(string id, Action<T>? loaded)
    {
        Id = id;
        _loaded = loaded;
    }

    /// <summary>The data. Change it freely, Core writes it to disk on the moments the derived type lists.</summary>
    public T Current { get; private set; } = new();

    internal string Id { get; }

    string ITimbnSaveEntry.Id => Id;

    void ITimbnSaveEntry.Load(JToken? section)
    {
        Current = section is null ? new T() : section.ToObject<T>(TimbnSaves.Serializer) ?? new T();
        TimbnSafe.Run(() => _loaded?.Invoke(Current), $"{GetType().Name}|The loaded handler of {Id}");
    }

    JToken ITimbnSaveEntry.Serialize() => JToken.FromObject(Current, TimbnSaves.Serializer);

    void ITimbnSaveEntry.Reset() => Current = new T();
}

/// <summary>
/// Holds a plugin's own data for the loaded save. Core fills <see cref="TimbnData{T}.Current"/> when a save loads
/// and writes it to disk each time the game saves, so the plugin only reads and changes it. Get one from
/// Saves.Register.
/// </summary>
/// <typeparam name="T">The plugin's save data type.</typeparam>
public sealed class TimbnSaveData<T> : TimbnData<T> where T : class, new()
{
    internal TimbnSaveData(string id, Action<T>? loaded)
        : base(id, loaded)
    {
    }

    /// <summary>
    /// The file the data is kept in next to the loaded save, or null at the main menu. Every Timbn plugin shares
    /// it, so it holds other plugins' sections too.
    /// </summary>
    public string? FilePath => TimbnSaves.CurrentPath;
}

/// <summary>
/// Holds a plugin's own data that belongs to no save. Core fills <see cref="TimbnData{T}.Current"/> when the plugin
/// starts and writes it to disk when the game saves, when the player returns to the main menu, when the game quits,
/// when the plugin unloads, and whenever <see cref="Save"/> is called. Get one from Saves.RegisterGlobal.
/// </summary>
/// <typeparam name="T">The plugin's global data type.</typeparam>
public sealed class TimbnGlobalData<T> : TimbnData<T> where T : class, new()
{
    internal TimbnGlobalData(string id, Action<T>? loaded)
        : base(id, loaded)
    {
    }

    /// <summary>The file the data is kept in, in the game's save folder. Every Timbn plugin shares it.</summary>
    public string FilePath => TimbnGlobalSaves.MainPath;

    /// <summary>
    /// Writes the data to disk now, unless nothing in the file has changed. Core already writes on the events
    /// listed on this type, so call this only right after a change that must not be lost to a crash.
    /// </summary>
    public void Save() => TimbnGlobalSaves.Flush();
}
