using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal interface ITimbnSaveEntry
{
    string Id { get; }

    void Load(JToken? section);

    JToken Serialize();

    void Reset();
}

internal static class TimbnSaves
{
    private const string _fileSuffix = ".TimbnSaveData";

    private static readonly Dictionary<string, ITimbnSaveEntry> _entries = [];
    private static JObject _sections = [];
    private static string? _slotName;
    private static bool _freshSave;
    private static bool _newGameStarting;

    internal static JsonSerializer Serializer { get; } = JsonSerializer.Create(new JsonSerializerSettings
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    });

    internal static IDisposable Register(ITimbnSaveEntry entry)
    {
        if (_entries.ContainsKey(entry.Id))
            throw new InvalidOperationException($"{entry.Id} already registered save data. A plugin gets one save data object, so put everything it saves in one type.");

        _entries.Add(entry.Id, entry);
        if (TimbnGame.IsInGame)
            Load(entry);

        return new TimbnUndo(() =>
        {
            _entries.Remove(entry.Id);
            if (TimbnGame.IsInGame)
                KeepSection(entry);
        });
    }

    internal static void OnNewGameStarting() => _newGameStarting = true;

    internal static void OnGameStarted()
    {
        var slot = MainGame.Instance.SaveSlotData;
        _slotName = slot?.slotName;
        _freshSave = _newGameStarting || slot is null || slot.isDemoSave || string.IsNullOrEmpty(_slotName);
        _newGameStarting = false;
        _sections = _freshSave || _slotName is null ? [] : TimbnSectionFile.Read(MainPath(_slotName), BackupPath(_slotName));
        foreach (var entry in _entries.Values.ToList())
            Load(entry);
    }

    internal static void OnLeftGame()
    {
        _sections = [];
        foreach (var entry in _entries.Values)
            entry.Reset();
    }

    internal static Action? WrapSaveCallback(SaveSlotData slotData, GameSave gameSave, Action? callback)
    {
        if (MainGame.Instance == null || gameSave != MainGame.Instance.GameSave)
            return callback;

        return () =>
        {
            try
            {
                Write(slotData.slotName);
            }
            catch (Exception ex)
            {
                TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSaves)}|Could not write mod save data for slot {slotData.slotName}: {ex}");
            }

            callback?.Invoke();
        };
    }

    internal static void DeleteSlot(string slotName)
    {
        foreach (var path in Directory.GetFiles(SaveSystem.SaveFolder, slotName + _fileSuffix + "*"))
            File.Delete(path);
    }

    private static void Load(ITimbnSaveEntry entry)
    {
        var section = _sections[entry.Id];
        try
        {
            entry.Load(section);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSaves)}|Could not read the save data of {entry.Id}, so it starts empty. {ex.Message}");
            if (section is not null && _slotName is not null)
                TimbnSectionFile.WriteAside(MainPath(_slotName) + "." + entry.Id + ".bad", section);

            entry.Load(null);
        }
    }

    private static void KeepSection(ITimbnSaveEntry entry)
    {
        try
        {
            _sections[entry.Id] = entry.Serialize();
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSaves)}|Could not keep the save data of {entry.Id}, so its last saved copy stays. {ex.Message}");
        }
    }

    private static void Write(string slotName)
    {
        var path = MainPath(slotName);
        var backupPath = BackupPath(slotName);
        if (_freshSave)
        {
            File.Delete(path);
            File.Delete(backupPath);
            _freshSave = false;
        }

        _slotName = slotName;
        foreach (var entry in _entries.Values)
            KeepSection(entry);

        if (_sections.Count == 0)
            return;

        TimbnSectionFile.Write(path, backupPath, _sections);
    }

    private static string MainPath(string slotName) => SaveSystem.SaveFolder + slotName + _fileSuffix + ".dat";

    private static string BackupPath(string slotName) => SaveSystem.SaveFolder + slotName + _fileSuffix + ".backup.dat";
}
