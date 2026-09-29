using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds a plugin's own data for the loaded save. Core fills <see cref="Current"/> when a save loads and writes it
/// to disk each time the game saves, so the plugin only reads and changes it. Get one from Saves.Register.
/// </summary>
/// <typeparam name="T">The plugin's save data type.</typeparam>
public sealed class TimbnSaveData<T> : ITimbnSaveEntry where T : class, new()
{
    private readonly string _id;
    private readonly Action<T>? _loaded;

    internal TimbnSaveData(string id, Action<T>? loaded)
    {
        _id = id;
        _loaded = loaded;
    }

    /// <summary>
    /// The data for the loaded save. It is the copy read from disk once a save loads, a new T on a new game or a
    /// save that has none yet, and a new T again at the main menu. Change it freely. It reaches disk the next time
    /// the game saves, so leaving without saving drops the changes, just like the game's own progress.
    /// </summary>
    public T Current { get; private set; } = new();

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
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSaves)}|The loaded handler of {_id} threw: {ex}");
        }
    }

    JToken ITimbnSaveEntry.Serialize() => JToken.FromObject(Current, TimbnSaves.Serializer);

    void ITimbnSaveEntry.Reset() => Current = new T();
}
