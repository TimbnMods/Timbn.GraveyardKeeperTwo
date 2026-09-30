using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Reads and writes the file of mod data that belongs to no save, shared by every plugin.</summary>
internal static class TimbnGlobalSaves
{
    private const string _fileName = "TimbnGlobalData";

    private static readonly Dictionary<string, ITimbnSaveEntry> _entries = [];
    private static JObject? _sections;
    private static string _lastWritten = "";

    internal static string MainPath => SaveSystem.SaveFolder + _fileName + ".dat";

    private static string BackupPath => SaveSystem.SaveFolder + _fileName + ".backup.dat";

    internal static IDisposable Register(ITimbnSaveEntry entry)
    {
        if (_entries.ContainsKey(entry.Id))
            throw new InvalidOperationException($"{entry.Id} already registered global data. Give a second data object its own name.");

        _entries.Add(entry.Id, entry);
        TimbnSectionFile.LoadEntry(Sections, entry, MainPath, nameof(TimbnGlobalSaves));
        return new TimbnUndo(() =>
        {
            TimbnSectionFile.KeepEntry(Sections, entry, nameof(TimbnGlobalSaves));
            _entries.Remove(entry.Id);
            Flush();
        });
    }

    internal static void Flush()
    {
        try
        {
            if (_sections is null && _entries.Count == 0)
                return;

            foreach (var entry in _entries.Values)
                TimbnSectionFile.KeepEntry(Sections, entry, nameof(TimbnGlobalSaves));

            var sections = Sections;
            if (sections.Count == 0)
                return;

            var json = sections.ToString(Formatting.None);
            if (json == _lastWritten)
                return;

            TimbnSectionFile.Write(MainPath, BackupPath, sections);
            _lastWritten = json;
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnGlobalSaves)}|Could not write mod global data: {ex}");
        }
    }

    private static JObject Sections
    {
        get
        {
            if (_sections is null)
            {
                _sections = TimbnSectionFile.Read(MainPath, BackupPath);
                _lastWritten = _sections.ToString(Formatting.None);
            }

            return _sections;
        }
    }
}
