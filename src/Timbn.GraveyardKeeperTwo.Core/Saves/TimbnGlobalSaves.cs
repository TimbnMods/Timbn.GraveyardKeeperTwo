using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnGlobalSaves
{
    private const string _fileName = "TimbnGlobalData";

    private static readonly Dictionary<string, ITimbnSaveEntry> _entries = [];
    private static JObject? _sections;
    private static string _lastWritten = "";

    internal static IDisposable Register(ITimbnSaveEntry entry)
    {
        if (_entries.ContainsKey(entry.Id))
            throw new InvalidOperationException($"{entry.Id} already registered global data. A plugin gets one global data object, so put everything it keeps in one type.");

        _entries.Add(entry.Id, entry);
        Load(entry);
        return new TimbnUndo(() =>
        {
            KeepSection(entry);
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
                KeepSection(entry);

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

    private static string MainPath => SaveSystem.SaveFolder + _fileName + ".dat";

    private static string BackupPath => SaveSystem.SaveFolder + _fileName + ".backup.dat";

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

    private static void Load(ITimbnSaveEntry entry)
    {
        var section = Sections[entry.Id];
        try
        {
            entry.Load(section);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnGlobalSaves)}|Could not read the global data of {entry.Id}, so it starts empty. {ex.Message}");
            if (section is not null)
                TimbnSectionFile.WriteAside(MainPath + "." + entry.Id + ".bad", section);

            entry.Load(null);
        }
    }

    private static void KeepSection(ITimbnSaveEntry entry)
    {
        try
        {
            Sections[entry.Id] = entry.Serialize();
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnGlobalSaves)}|Could not keep the global data of {entry.Id}, so its last saved copy stays. {ex.Message}");
        }
    }
}
