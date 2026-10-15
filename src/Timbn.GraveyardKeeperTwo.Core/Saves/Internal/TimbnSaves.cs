using LazyBearTechnology;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSaves
{
    private const string _fileSuffix = ".TimbnSaveData";

    private static readonly Regex _backupSlot = new("^(.+)_backup_\\d+$");

    private static readonly Dictionary<string, ITimbnSaveEntry> _entries = [];
    private static JObject _sections = [];
    private static string? _slotName;
    private static bool _freshSave;
    private static bool _newGameStarting;
    private static bool _readOnContinue;

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

    internal static void OnNewGameStarting()
    {
        _newGameStarting = true;
        _readOnContinue = false;
        TimbnModContent.OnNewGame();
    }

    internal static void OnLoadingExistingSave() => _newGameStarting = false;

    internal static void OnContinueGame(SaveSlotData slot, GameSave? save)
    {
        var slotName = slot.slotName;
        _newGameStarting = false;
        _slotName = slotName;
        _freshSave = slot.isDemoSave || string.IsNullOrEmpty(slotName);
        _sections = _freshSave ? [] : TimbnSectionFile.Read(MainPath(slotName), BackupPath(slotName));
        _readOnContinue = true;
        TimbnModContent.OnContinueGame(_freshSave ? _sections : ModContentSections(slotName), slot, save);
    }

    private static JObject ModContentSections(string slotName)
    {
        if (_sections[TimbnModContent.SectionId] is not null || _backupSlot.Match(slotName) is not { Success: true } backup)
            return _sections;

        var baseSlot = backup.Groups[1].Value;
        return TimbnSectionFile.Read(MainPath(baseSlot), BackupPath(baseSlot));
    }

    internal static void OnGameStarted()
    {
        if (!_readOnContinue)
        {
            var slot = MainGame.Instance.SaveSlotData;
            _slotName = slot?.slotName;
            _freshSave = _newGameStarting || slot is null || slot.isDemoSave || string.IsNullOrEmpty(_slotName);
            _sections = _freshSave || _slotName is null ? [] : TimbnSectionFile.Read(MainPath(_slotName), BackupPath(_slotName));
        }

        _newGameStarting = false;
        _readOnContinue = false;
        foreach (var entry in _entries.Values.ToList())
            Load(entry);
    }

    internal static void OnLeftGame()
    {
        _newGameStarting = false;
        _readOnContinue = false;
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
            TimbnSafe.Run(() => Write(slotData.slotName, slotData.saveDateTime), $"{nameof(TimbnSaves)}|Writing mod save data for slot {slotData.slotName}");
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

    private static void Write(string slotName, string stamp)
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

        TimbnSafe.Run(() => TimbnModContent.KeepSection(_sections, stamp), $"{nameof(TimbnSaves)}|Keeping the mod items taken out of the save");
        TimbnSafe.Run(() => RotateBackups(slotName), $"{nameof(TimbnSaves)}|Rotating the mod save data backups of slot {slotName}");
        if (_sections.Count == 0)
            return;

        TimbnSectionFile.Write(path, backupPath, _sections);
    }

    // The game just moved its slot file down its three _backup_N slots, so the mod data follows it under the same
    // names and loading a backup slot finds the plugin sections of that very save.
    private static void RotateBackups(string slotName)
    {
        if (!LazyAPI.LazyFile.IsSupportingBackupSaves)
            return;

        var third = MainPath(BackupSlot(slotName, 3));
        if (File.Exists(third))
            File.Delete(third);

        MoveIfPresent(MainPath(BackupSlot(slotName, 2)), third);
        MoveIfPresent(MainPath(BackupSlot(slotName, 1)), MainPath(BackupSlot(slotName, 2)));
        var main = MainPath(slotName);
        if (File.Exists(main))
            File.Copy(main, MainPath(BackupSlot(slotName, 1)), overwrite: true);
    }

    private static void MoveIfPresent(string from, string to)
    {
        if (File.Exists(from))
            File.Move(from, to);
    }

    private static string BackupSlot(string slotName, int index) => $"{slotName}_backup_{index}";

    private static string MainPath(string slotName) => SaveSystem.SaveFolder + slotName + _fileSuffix + ".dat";

    private static string BackupPath(string slotName) => SaveSystem.SaveFolder + slotName + _fileSuffix + ".backup.dat";
}
