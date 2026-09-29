using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds a plugin's own data that belongs to no save. Core fills <see cref="Current"/> when the plugin starts and
/// writes it to disk when the game saves, when the player returns to the main menu, when the game quits, when the
/// plugin unloads, and whenever <see cref="Save"/> is called. Get one from Saves.RegisterGlobal.
/// </summary>
/// <typeparam name="T">The plugin's global data type.</typeparam>
public sealed class TimbnGlobalData<T> : ITimbnSaveEntry where T : class, new()
{
    private readonly string _id;
    private readonly Action<T>? _loaded;

    internal TimbnGlobalData(string id, Action<T>? loaded)
    {
        _id = id;
        _loaded = loaded;
    }

    /// <summary>
    /// The data, read from disk when the plugin started or a new T when there was none. Change it freely. It
    /// stays the same across saves and the main menu.
    /// </summary>
    public T Current { get; private set; } = new();

    /// <summary>
    /// Writes the data to disk now, unless nothing in the file has changed. Core already writes on the events
    /// listed on this type, so call this only right after a change that must not be lost to a crash.
    /// </summary>
    public void Save() => TimbnGlobalSaves.Flush();

    string ITimbnSaveEntry.Id => _id;

    void ITimbnSaveEntry.Load(JToken? section)
    {
        Current = section is null ? new T() : section.ToObject<T>(TimbnSaves.Serializer) ?? new T();
        if (_loaded is null)
            return;

        try
        {
            _loaded(Current);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnGlobalSaves)}|The loaded handler of {_id} threw: {ex}");
        }
    }

    JToken ITimbnSaveEntry.Serialize() => JToken.FromObject(Current, TimbnSaves.Serializer);

    void ITimbnSaveEntry.Reset() => Current = new T();
}
