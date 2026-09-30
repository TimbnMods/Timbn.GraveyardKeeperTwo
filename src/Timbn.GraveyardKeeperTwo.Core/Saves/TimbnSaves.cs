using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Reads and writes the mod data file kept next to each save, shared by every plugin.</summary>
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

    internal static string? CurrentPath => TimbnGame.IsInGame && _slotName is { } slot ? MainPath(slot) : null;

    internal static IDisposable Register(ITimbnSaveEntry entry)
    {
        if (_entries.ContainsKey(entry.Id))
            throw new InvalidOperationException($"{entry.Id} already registered save data. Give a second data object its own name.");

        _entries.Add(entry.Id, entry);
        if (TimbnGame.IsInGame)
            Load(entry);

        return new TimbnUndo(() =>
        {
            _entries.Remove(entry.Id);
            if (TimbnGame.IsInGame)
                TimbnSectionFile.KeepEntry(_sections, entry, nameof(TimbnSaves));
        });
    }

    internal static void OnNewGameStarting() => _newGameStarting = true;

    internal static void OnLoadingExistingSave() => _newGameStarting = false;

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
        _newGameStarting = false;
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
            TimbnSafe.Run(() => Write(slotData.slotName), $"{nameof(TimbnSaves)}|Writing mod save data for slot {slotData.slotName}");
            callback?.Invoke();
        };
    }

    internal static void DeleteSlot(string slotName)
    {
        foreach (var path in Directory.GetFiles(SaveSystem.SaveFolder, slotName + _fileSuffix + "*"))
            File.Delete(path);
    }

    private static void Load(ITimbnSaveEntry entry) =>
        TimbnSectionFile.LoadEntry(_sections, entry, _slotName is null ? "" : MainPath(_slotName), nameof(TimbnSaves));

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
            TimbnSectionFile.KeepEntry(_sections, entry, nameof(TimbnSaves));

        if (_sections.Count == 0)
            return;

        TimbnSectionFile.Write(path, backupPath, _sections);
    }

    private static string MainPath(string slotName) => SaveSystem.SaveFolder + slotName + _fileSuffix + ".dat";

    private static string BackupPath(string slotName) => SaveSystem.SaveFolder + slotName + _fileSuffix + ".backup.dat";
}
